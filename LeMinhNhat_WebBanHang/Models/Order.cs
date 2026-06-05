using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeMinhNhat_WebBanHang.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng")]
        [StringLength(255)]
        public string ShippingAddress { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Shipped, Cancelled, Completed

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
