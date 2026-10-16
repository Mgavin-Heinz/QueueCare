using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QueueCare.Models;
using QueueCare.Validation;

namespace QueueCare.Data
{
    public static class IdentitySeeder
    {
        // The four roles in QueueCare
        public static readonly string[] Roles = { "Patient", "Receptionist", "Nurse", "Admin" };

        // Test password for demo accounts only (never a real password)
        private const string DemoPassword = "Demo123!";

        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

            // 1. Create roles if they dont exist
            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(role));
                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        throw new Exception($"Could not create role {role}: {errors}");
                    }
                }
            }

            // 2. Create one demo account per role
            await CreateUserAsync(userManager, "admin@queuecare.local", "Admin");
            await CreateUserAsync(userManager, "reception@queuecare.local", "Receptionist");
            await CreateUserAsync(userManager, "nurse@queuecare.local", "Nurse");
            await CreateUserAsync(userManager, "patient@queuecare.local", "Patient");

            // 3. Give the demo patient account a patient record, so booking can be tested
            var db = services.GetRequiredService<ApplicationDbContext>();
            var demoPatient = await userManager.FindByEmailAsync("patient@queuecare.local");

            if (demoPatient != null && !await db.Patients.AnyAsync(p => p.UserId == demoPatient.Id))
            {
                db.Patients.Add(new Patient
                {
                    FirstName = "Demo",
                    LastName = "Patient",
                    PhoneNumber = PhoneNumberValidator.Normalize("0821234567"),
                    UserId = demoPatient.Id,
                    CreatedAt = DateTime.Now
                });
                await db.SaveChangesAsync();
            }
        }

        private static async Task CreateUserAsync(UserManager<IdentityUser> userManager, string email, string role)
        {
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, DemoPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Could not create {email}: {errors}");
                }
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}