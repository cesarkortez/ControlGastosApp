using Microsoft.AspNetCore.Identity;

namespace ControlGastosApp.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAdminAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string adminEmail = "admin@admin.com";
            string password = "Admin123!";

            // Crear el rol "Admin" si no existe
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // Buscar si el usuario ya existe
            var user = await userManager.FindByEmailAsync(adminEmail);
            if (user == null)
            {
                var newUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(newUser, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newUser, "Admin");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    throw new Exception($"No se pudo crear el usuario admin: {errors}");
                }
            }
            else
            {
                // Asegurarse de que tiene el rol
                var roles = await userManager.GetRolesAsync(user);
                if (!roles.Contains("Admin"))
                {
                    await userManager.AddToRoleAsync(user, "Admin");
                }

            }
        }
    }
}
