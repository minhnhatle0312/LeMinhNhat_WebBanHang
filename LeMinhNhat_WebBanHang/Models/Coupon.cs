using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeMinhNhat_WebBanHang.Models
{
    public class Coupon
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        [Required]
        [StringLength(20)]
        public string DiscountType { get; set; } // "Percentage" or "FixedAmount"

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinimumOrderAmount { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
