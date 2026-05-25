using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vjezba.Model.Data;
using Vjezba.Model.Models;

namespace Vjezba.Model.Controllers
{
    [Route("users")]
    public class UsersController : Controller
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("{id:int}/soft-delete")]
        [Authorize(Roles = "Admin")]
        public IActionResult SoftDelete(int id)
        {
            var user = _context.Users.FirstOrDefault(x => x.Id == id);
            if (user is null)
            {
                return NotFound();
            }

            user.DeletedAt = DateTime.UtcNow;
            _context.SaveChanges();

            return RedirectToAction("Index", "Home", new { selectedType = "user" });
        }

        [HttpPost("create")]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Create(UserCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { errors = BuildErrors() });
            }

            var normalizedEmail = model.Email.Trim();
            var emailExists = _context.Users.IgnoreQueryFilters()
                .Any(x => x.Email.ToLower() == normalizedEmail.ToLower());
            if (emailExists)
            {
                ModelState.AddModelError(nameof(UserCreateViewModel.Email), "Email already exists.");
                return BadRequest(new { errors = BuildErrors() });
            }

            var user = new User
            {
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                Email = normalizedEmail,
                PhoneNumber = model.PhoneNumber.Trim(),
                RegistrationDate = DateTime.UtcNow
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok(new { id = user.Id });
        }

        [HttpPost("edit")]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Edit(UserEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { errors = BuildErrors() });
            }

            var user = _context.Users.FirstOrDefault(x => x.Id == model.Id);
            if (user is null)
            {
                return NotFound();
            }

            var normalizedEmail = model.Email.Trim();
            var emailExists = _context.Users.IgnoreQueryFilters()
                .Any(x => x.Email.ToLower() == normalizedEmail.ToLower() && x.Id != model.Id);
            if (emailExists)
            {
                ModelState.AddModelError(nameof(UserEditViewModel.Email), "Email already exists.");
                return BadRequest(new { errors = BuildErrors() });
            }

            user.FirstName = model.FirstName.Trim();
            user.LastName = model.LastName.Trim();
            user.Email = normalizedEmail;
            user.PhoneNumber = model.PhoneNumber.Trim();

            _context.SaveChanges();

            return Ok(new { id = user.Id });
        }

        private IDictionary<string, string[]> BuildErrors()
        {
            return ModelState
                .Where(entry => entry.Value is not null && entry.Value.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value?.Errors.Select(error => error.ErrorMessage).ToArray() ?? Array.Empty<string>());
        }
    }
}
