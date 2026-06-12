using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace LeMinhNhat_WebBanHang.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [StringLength(100)]
        public string FullName { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        [Range(1, 120, ErrorMessage = "Tuổi không hợp lệ")]
        public int? Age { get; set; }

        public string? ProfilePictureUrl { get; set; }

        public int LoyaltyPoints { get; set; } = 0;

        [StringLength(50)]
        public string MembershipRank { get; set; } = "Đồng";

        public DateTime AccountCreatedAt { get; set; } = DateTime.Now;
    }
}