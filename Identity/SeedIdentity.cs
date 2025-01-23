using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Alpha.Identity
{
    public static class SeedIdentity
    {
        public static async Task Seed(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            // Get roles and ensure roles are valid
            var roles = configuration.GetSection("Data:Roles").GetChildren()
                .Select(x => x.Value)
                .Where(role => !string.IsNullOrEmpty(role))
                .ToArray();

            foreach (var role in roles)
            {
                if (role == null) continue; // Safety check
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Get users
            var users = configuration.GetSection("Data:Users").GetChildren();

            foreach (var section in users)
            {
                var username = section.GetValue<string>("username");
                var password = section.GetValue<string>("password");
                var email = section.GetValue<string>("email");
                var role = section.GetValue<string>("role");
                var firstName = section.GetValue<string>("firstName");
                var lastName = section.GetValue<string>("lastName");

                // Validate required fields
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(role))
                {
                    Console.WriteLine("Skipping invalid user due to missing required fields.");
                    continue;
                }

                // Check if user already exists
                var existingUser = await userManager.FindByNameAsync(username);
                if (existingUser == null)
                {
                    var user = new User()
                    {
                        UserName = username,
                        Email = email,
                        FirstName = firstName ?? "DefaultFirstName", // Handle null firstName
                        LastName = lastName ?? "DefaultLastName",   // Handle null lastName
                        EmailConfirmed = true
                    };

                    // Create user with null safety for password
                    var result = await userManager.CreateAsync(user, password ?? throw new ArgumentNullException(nameof(password), "Password cannot be null."));
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, role ?? throw new ArgumentNullException(nameof(role), "Role cannot be null."));
                    }
                    else
                    {
                        Console.WriteLine($"Failed to create user: {username}");
                    }
                }
                else
                {
                    Console.WriteLine($"User '{username}' already exists.");
                }
            }
        }
    }
}
