using System.Net;
using System.Net.Mail;

namespace Fixtrack.Services;   // match your project's namespace

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
        var port = int.Parse(_config["Email:Port"] ?? "587");
        var user = _config["Email:Username"];
        var pass = _config["Email:Password"];
        var fromName = _config["Email:FromName"] ?? "FixTrack";

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(user, pass)
        };

        using var message = new MailMessage
        {
            From = new MailAddress(user!, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        try
        {
            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", toEmail);
            // Deliberately swallowed: the caller shows the same message either way,
            // so we never reveal whether an account exists.
        }
    }
}