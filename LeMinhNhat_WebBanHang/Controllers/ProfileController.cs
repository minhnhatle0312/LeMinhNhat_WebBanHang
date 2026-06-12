using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeMinhNhat_WebBanHang.Models;
using LeMinhNhat_WebBanHang.DataAccess;

namespace LeMinhNhat_WebBanHang.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Load user orders
            var orders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.Orders = orders;
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

            if (order == null) return NotFound();

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Update(string fullName, string? address, int? age, IFormFile? profilePicture)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (string.IsNullOrEmpty(fullName))
            {
                TempData["Error"] = "Họ và tên không được để trống";
                return RedirectToAction(nameof(Index));
            }

            user.FullName = fullName;
            user.Address = address;
            user.Age = age;

            if (profilePicture != null && profilePicture.Length > 0)
            {
                var extension = Path.GetExtension(profilePicture.FileName).ToLower();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                if (allowedExtensions.Contains(extension))
                {
                    var uniqueName = Guid.NewGuid().ToString() + extension;
                    var avatarFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/avatars");
                    if (!Directory.Exists(avatarFolder))
                    {
                        Directory.CreateDirectory(avatarFolder);
                    }
                    var savePath = Path.Combine(avatarFolder, uniqueName);
                    using (var stream = new FileStream(savePath, FileMode.Create))
                    {
                        await profilePicture.CopyToAsync(stream);
                    }
                    user.ProfilePictureUrl = "/images/avatars/" + uniqueName;
                }
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Refresh sign in cookie to update claims
                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = "Cập nhật thông tin hồ sơ thành công!";
            }
            else
            {
                TempData["Error"] = "Cập nhật hồ sơ thất bại: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
