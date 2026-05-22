using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace LeMinhNhat_WebBanHang.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm"), StringLength(100)]
        public string Name { get; set; }

        [Range(0.01, 10000.00, ErrorMessage = "Giá sản phẩm phải lớn hơn 0")]
        public decimal Price { get; set; }

        public string Description { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        public int CategoryId { get; set; }

        // BỔ SUNG ĐỂ LƯU ĐƯỜNG DẪN ẢNH ĐẠI DIỆN VÀ ẢNH MÔ TẢ
        public string? ImageUrl { get; set; }
        public List<string>? ImageUrls { get; set; } = new List<string>();
    }
}