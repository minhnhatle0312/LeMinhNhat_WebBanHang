using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LeMinhNhat_WebBanHang.Models;

namespace LeMinhNhat_WebBanHang.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email không được để trống")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
            [StringLength(100, ErrorMessage = "{0} phải dài từ {2} đến tối đa {1} ký tự.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu mới")]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Xác nhận mật khẩu mới")]
            [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
            public string ConfirmNewPassword { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null)
                {
                    // For security, don't reveal that the user does not exist
                    ModelState.AddModelError(string.Empty, "Email không tồn tại trong hệ thống.");
                    return Page();
                }

                // Quick reset for academic/demo convenience:
                var removeToken = await _userManager.RemovePasswordAsync(user);
                if (removeToken.Succeeded)
                {
                    var addResult = await _userManager.AddPasswordAsync(user, Input.NewPassword);
                    if (addResult.Succeeded)
                    {
                        TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công! Bạn có thể đăng nhập ngay bây giờ.";
                        return RedirectToPage("./Login");
                    }
                }

                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi đặt lại mật khẩu.");
            }

            return Page();
        }
    }
}
