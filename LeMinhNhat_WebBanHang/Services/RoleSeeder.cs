using Microsoft.AspNetCore.Identity;
using LeMinhNhat_WebBanHang.Models;

namespace LeMinhNhat_WebBanHang.Services
{
    public static class RoleSeeder
    {
        public static async Task InitializeAsync(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            string[] roles = new[] { SD.Role_Admin, SD.Role_Customer, SD.Role_Employee };

            foreach (var r in roles)
            {
                if (!await roleManager.RoleExistsAsync(r))
                {
                    await roleManager.CreateAsync(new IdentityRole(r));
                }
            }

            // Create default admin if not exists
            var adminEmail = "admin@techstore.local";
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "System Admin"
                };

                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, SD.Role_Admin);
                }
            }
        }
    }
}
