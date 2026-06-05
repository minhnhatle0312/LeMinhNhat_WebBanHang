using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeMinhNhat_WebBanHang.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeMinhNhat_WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserManagerController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserManagerController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRolesViewModel = new List<UserRolesViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRolesViewModel.Add(new UserRolesViewModel
                {
                    User = user,
                    Roles = roles
                });
            }

            ViewBag.AvailableRoles = _roleManager.Roles.Select(r => r.Name).ToList();
            return View(userRolesViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeRole(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            
            // Do not demote yourself (the logged-in admin) to prevent lockout
            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId && newRole != SD.Role_Admin)
            {
                TempData["Error"] = "Bạn không thể tự hạ quyền Admin của chính mình!";
                return RedirectToAction(nameof(Index));
            }

            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                TempData["Error"] = "Lỗi khi xóa vai trò cũ.";
                return RedirectToAction(nameof(Index));
            }

            var addResult = await _userManager.AddToRoleAsync(user, newRole);
            if (addResult.Succeeded)
            {
                TempData["Success"] = $"Đã thay đổi vai trò của {user.FullName} thành {newRole}!";
            }
            else
            {
                TempData["Error"] = "Lỗi khi gán vai trò mới.";
            }

            return RedirectToAction(nameof(Index));
        }
    }

    public class UserRolesViewModel
    {
        public ApplicationUser User { get; set; }
        public IList<string> Roles { get; set; }
    }
}
