using System.Net;
using System.Net.Mail;

namespace Fixtrack.Services;

public interface IAppEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody);
}

public class EmailSender : IAppEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IConfiguration config, ILogger<EmailSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var host = _config["Email:Host"];
        var portText = _config["Email:Port"] ?? "587";
        var user = _config["Email:Username"];          // the SMTP login
        var pass = _config["Email:Password"];          // app password / SMTP key
        var fromName = _config["Email:FromName"] ?? "FixTrack";

        // The "from" address. With Gmail it is the same as the login. With other
        // providers (e.g. Brevo) the login is different, so set Email:FromAddress.
        var fromAddress = _config["Email:FromAddress"];
        if (string.IsNullOrWhiteSpace(fromAddress)) fromAddress = user;

        // 1. Settings must be present. This is the most common failure: the
        //    secrets file is not being read, or a key is misspelled.
        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(user) ||
            string.IsNullOrWhiteSpace(pass) ||
            string.IsNullOrWhiteSpace(fromAddress))
        {
            _logger.LogError(
                "EMAIL NOT SENT - settings missing. Host set: {Host}, Username set: {User}, Password set: {Pass}, From set: {From}. " +
                "Check secrets.json has an \"Email\" section and restart the app.",
                !string.IsNullOrWhiteSpace(host),
                !string.IsNullOrWhiteSpace(user),
                !string.IsNullOrWhiteSpace(pass),
                !string.IsNullOrWhiteSpace(fromAddress));
            return;
        }

        if (!int.TryParse(portText, out var port))
        {
            _logger.LogError("EMAIL NOT SENT - Email:Port '{Port}' is not a number.", portText);
            return;
        }

        // Gmail shows app passwords in groups of four; spaces are not part of it.
        pass = pass.Replace(" ", "");

        try
        {
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(user, pass),
                Timeout = 20000   // 20 seconds, so a blocked connection fails visibly
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromAddress, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);

            _logger.LogInformation("EMAIL SENT to {To} from {From} via {Host}:{Port}.",
                toEmail, fromAddress, host, port);
        }
        catch (SmtpException ex)
        {
            // StatusCode tells us exactly what the mail server objected to.
            _logger.LogError(ex,
                "EMAIL FAILED (SMTP {Status}) sending to {To} from {From}: {Message}",
                ex.StatusCode, toEmail, fromAddress, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "EMAIL FAILED sending to {To} from {From}: {Message}",
                toEmail, fromAddress, ex.Message);
        }
    }
}
