using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Fixtrack.Data;
using Fixtrack.Models;
using Fixtrack.ViewModels;

namespace Fixtrack.Controllers
{
    [Authorize(Roles = "Receptionist")]
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Customers
        public async Task<IActionResult> Index()
        {
            // Include loads each customer's devices so the list can show a
            // device count. Without it, item.Devices would always be empty.
            return View(await _context.Customers
                .Include(c => c.Devices)
                // Sort on real columns - FullName is [NotMapped], so EF
                // cannot turn it into SQL and would throw at runtime.
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .ToListAsync());
        }

        // GET: Customers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                // The details page lists what they have brought in, so
                // the devices and jobs have to be loaded with them.
                .Include(c => c.Devices)
                .Include(c => c.RepairJobs).ThenInclude(j => j.Device)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // GET: Customers/Create
        public IActionResult Create()
        {
            return View(new CustomerRegistrationViewModel());
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerRegistrationViewModel model)
        {
            // Drop any empty rows (e.g. an added row the receptionist didn't
            // fill in) before validating, so a blank extra row doesn't
            // block submission.
            model.Devices = model.Devices
                .Where(d => !string.IsNullOrWhiteSpace(d.Brand) || !string.IsNullOrWhiteSpace(d.Model))
                .ToList();

            if (!model.Devices.Any())
            {
                ModelState.AddModelError(string.Empty, "At least one device is required.");
            }

            if (!ModelState.IsValid)
            {
                if (!model.Devices.Any())
                {
                    model.Devices.Add(new DeviceEntryViewModel());
                }
                return View(model);
            }

            var customer = new Customer
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address
            };

            foreach (var deviceEntry in model.Devices)
            {
                customer.Devices.Add(new Device
                {
                    DeviceType = deviceEntry.DeviceType,
                    Brand = deviceEntry.Brand,
                    Model = deviceEntry.Model
                });
            }

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        // POST: Customers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,PhoneNumber,Email,Address,PreferredContactMethod")] Customer customer)
        {
            if (id != customer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(customer);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerExists(customer.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // GET: Customers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                _context.Customers.Remove(customer);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.Id == id);
        }
    }
}