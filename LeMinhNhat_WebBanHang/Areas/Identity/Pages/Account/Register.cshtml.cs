using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using LeMinhNhat_WebBanHang.Models;

namespace LeMinhNhat_WebBanHang.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public SelectList RoleList { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email không được để trống")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Mật khẩu không được để trống")]
            [StringLength(100, ErrorMessage = "{0} phải dài từ {2} đến tối đa {1} ký tự.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Xác nhận mật khẩu")]
            [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
            public string ConfirmPassword { get; set; }

            [Required(ErrorMessage = "Họ và tên không được để trống")]
            [StringLength(100)]
            [Display(Name = "Họ và tên")]
            public string FullName { get; set; }

            [StringLength(255)]
            [Display(Name = "Địa chỉ")]
            public string? Address { get; set; }

            [Range(1, 120, ErrorMessage = "Tuổi không hợp lệ")]
            [Display(Name = "Tuổi")]
            public int? Age { get; set; }

            [Display(Name = "Ảnh đại diện")]
            public IFormFile? ProfilePicture { get; set; }

            [Display(Name = "Vai trò (Role)")]
            public string? SelectedRole { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // Populate Roles for user selection
            var roles = _roleManager.Roles.Select(r => r.Name).ToList();
            RoleList = new SelectList(roles);
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    FullName = Input.FullName,
                    Address = Input.Address,
                    Age = Input.Age,
                    AccountCreatedAt = DateTime.Now
                };

                // Handle Profile Picture upload
                if (Input.ProfilePicture != null)
                {
                    var extension = Path.GetExtension(Input.ProfilePicture.FileName).ToLower();
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
                            await Input.ProfilePicture.CopyToAsync(stream);
                        }
                        user.ProfilePictureUrl = "/images/avatars/" + uniqueName;
                    }
                }

                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // Assign Selected Role or default to Customer
                    var assignedRole = !string.IsNullOrEmpty(Input.SelectedRole) ? Input.SelectedRole : SD.Role_Customer;
                    if (await _roleManager.RoleExistsAsync(assignedRole))
                    {
                        await _userManager.AddToRoleAsync(user, assignedRole);
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Repopulate roles if validation fails
            var rolesList = _roleManager.Roles.Select(r => r.Name).ToList();
            RoleList = new SelectList(rolesList);
            return Page();
        }
    }
}
