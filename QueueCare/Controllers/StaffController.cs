using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QueueCare.Models;
using QueueCare.Services;

namespace QueueCare.Controllers
{
    // FR-49: the admin creates receptionist and nurse accounts, resets passwords and deactivates accounts.
    [Authorize(Roles = "Admin")]
    public class StaffController : Controller
    {
        private static readonly string[] StaffRoles = { "Receptionist", "Nurse" };

        // Deactivate parks the lockout far in the future, which is how it is told apart
        // from the short NFR-09 lockout after failed logins.
        private static readonly DateTimeOffset DeactivatedUntil = new(9999, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private readonly UserManager<IdentityUser> _users;
        private readonly AuditService _audit;

        public StaffController(UserManager<IdentityUser> users, AuditService audit)
        {
            _users = users;
            _audit = audit;
        }

        public async Task<IActionResult> Index()
        {
            var rows = new List<StaffRowViewModel>();
            foreach (var role in StaffRoles)
            {
                foreach (var u in await _users.GetUsersInRoleAsync(role))
                {
                    rows.Add(new StaffRowViewModel
                    {
                        Id = u.Id,
                        Email = u.Email ?? u.UserName ?? "",
                        Role = role,
                        IsDeactivated = u.LockoutEnd.HasValue && u.LockoutEnd >= DeactivatedUntil,
                        IsLockedOut = u.LockoutEnd.HasValue
                                      && u.LockoutEnd > DateTimeOffset.UtcNow
                                      && u.LockoutEnd < DeactivatedUntil
                    });
                }
            }
            return View(rows.OrderBy(r => r.Role).ThenBy(r => r.Email).ToList());
        }

        public IActionResult Create() => View(new StaffCreateViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StaffCreateViewModel model)
        {
            if (!StaffRoles.Contains(model.Role))
            {
                ModelState.AddModelError(nameof(model.Role), "Choose Receptionist or Nurse.");
            }
            if (!ModelState.IsValid) return View(model);

            var user = new IdentityUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
            var created = await _users.CreateAsync(user, model.Password);
            if (!created.Succeeded)
            {
                foreach (var e in created.Errors) ModelState.AddModelError("", e.Description);
                return View(model);
            }

            await _users.AddToRoleAsync(user, model.Role);
            await _audit.LogAsync(AdminId(), $"Created {model.Role} account", "User", user.Id);
            TempData["Message"] = $"{model.Role} account created for {model.Email}.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await FindStaffAsync(id);
            if (user == null) return NotFound();
            return View(new StaffResetPasswordViewModel { Id = user.Id, Email = user.Email ?? "" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(StaffResetPasswordViewModel model)
        {
            var user = await FindStaffAsync(model.Id);
            if (user == null) return NotFound();
            model.Email = user.Email ?? "";
            if (!ModelState.IsValid) return View(model);

            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var result = await _users.ResetPasswordAsync(user, token, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return View(model);
            }

            await _audit.LogAsync(AdminId(), "Reset staff password", "User", user.Id);
            TempData["Message"] = $"Password reset for {user.Email}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(string id)
        {
            var user = await FindStaffAsync(id);
            if (user == null) return NotFound();

            await _users.SetLockoutEnabledAsync(user, true);
            await _users.SetLockoutEndDateAsync(user, DeactivatedUntil);
            await _audit.LogAsync(AdminId(), "Deactivated staff account", "User", user.Id);
            TempData["Message"] = $"{user.Email} is deactivated and can no longer log in.";
            return RedirectToAction(nameof(Index));
        }

        // Also clears a short NFR-09 lockout, so this doubles as an unlock button.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(string id)
        {
            var user = await FindStaffAsync(id);
            if (user == null) return NotFound();

            await _users.SetLockoutEndDateAsync(user, null);
            await _users.ResetAccessFailedCountAsync(user);
            await _audit.LogAsync(AdminId(), "Reactivated staff account", "User", user.Id);
            TempData["Message"] = $"{user.Email} is active again.";
            return RedirectToAction(nameof(Index));
        }

        // Only receptionist and nurse accounts can be managed here (never admins or patients).
        private async Task<IdentityUser?> FindStaffAsync(string? id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var user = await _users.FindByIdAsync(id);
            if (user == null) return null;
            var roles = await _users.GetRolesAsync(user);
            return roles.Any(r => StaffRoles.Contains(r)) ? user : null;
        }

        private string? AdminId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}