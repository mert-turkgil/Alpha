using Alpha.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Alpha.Data
{
    public static class SeedIdentity
    {
        public static async Task Seed(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IConfiguration config)
        {
            // Seed Roles
            var roles = config.GetSection("Data:Roles").Get<string[]>();
            if (roles != null)
            {
                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        var result = await roleManager.CreateAsync(new IdentityRole(role));
                        if (result.Succeeded)
                        {
                            Console.WriteLine($"✅ Role '{role}' created.");
                        }
                        else
                        {
                            Console.WriteLine($"❌ Failed to create role '{role}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"ℹ️ Role '{role}' already exists.");
                    }
                }
            }

            // Seed Users
            var users = config.GetSection("Data:Users").GetChildren();
            foreach (var userSection in users)
            {
                var username = userSection.GetValue<string>("username");
                var email = userSection.GetValue<string>("email");
                var password = userSection.GetValue<string>("password");
                var role = userSection.GetValue<string>("role");
                var firstName = userSection.GetValue<string>("firstName");
                var lastName = userSection.GetValue<string>("lastName");

                // ⚠️ Null kontrolü (CS8604 çözümü)
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    Console.WriteLine($"⚠️ Skipping user '{username}' because email or password is missing.");
                    continue;
                }

                var existingUser = await userManager.FindByEmailAsync(email);
                if (existingUser == null)
                {
                    var user = new User
                    {
                        UserName = username ?? email,
                        Email = email,
                        FirstName = firstName ?? "",
                        LastName = lastName ?? "",
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(user, password);
                    if (result.Succeeded)
                    {
                        Console.WriteLine($"✅ User '{username}' created.");
                        if (!string.IsNullOrEmpty(role))
                        {
                            await userManager.AddToRoleAsync(user, role);
                            Console.WriteLine($"🛡️ User '{username}' assigned to role '{role}'.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to create user '{username}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    Console.WriteLine($"ℹ️ User '{username}' already exists.");

                    // ✅ Rolü eksikse ekle
                    if (!string.IsNullOrEmpty(role) && !(await userManager.IsInRoleAsync(existingUser, role)))
                    {
                        await userManager.AddToRoleAsync(existingUser, role);
                        Console.WriteLine($"🛡️ Role '{role}' assigned to existing user '{username}'.");
                    }
                }
            }
        }
    }
}
