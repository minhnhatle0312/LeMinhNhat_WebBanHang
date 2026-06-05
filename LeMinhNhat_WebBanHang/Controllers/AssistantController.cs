using Microsoft.AspNetCore.Mvc;
using LeMinhNhat_WebBanHang.Repositories;
using LeMinhNhat_WebBanHang.Models;
using System;
using System.Linq;
using System.Text;

namespace LeMinhNhat_WebBanHang.Controllers
{
    public class AssistantController : Controller
    {
        private readonly IProductRepository _productRepository;

        public AssistantController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        [HttpPost]
        public IActionResult Ask(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return Json(new { reply = "Tôi có thể giúp gì cho bạn về cấu hình máy tính hoặc lựa chọn laptop hôm nay?" });
            }

            var query = message.ToLower().Trim();
            var reply = new StringBuilder();
            var products = _productRepository.GetAll().ToList();

            if (query.Contains("hello") || query.Contains("xin chào") || query.Contains("hi"))
            {
                reply.Append("<p>👋 Xin chào! Tôi là <strong>TechBot</strong> - Trợ lý cấu hình phần cứng ảo của bạn.</p>");
                reply.Append("<p>Tôi có thể tư vấn cho bạn:</p>");
                reply.Append("<ul>");
                reply.Append("<li>💡 Gợi ý cấu hình dựng PC (nhập <em>'build pc'</em> hoặc <em>'cấu hình'</em>)</li>");
                reply.Append("<li>💻 Chọn mua Laptop Gaming (nhập <em>'laptop'</em>)</li>");
                reply.Append("<li>💰 Tìm linh kiện giá rẻ dưới 500$ (nhập <em>'giá rẻ'</em>)</li>");
                reply.Append("</ul>");
            }
            else if (query.Contains("build pc") || query.Contains("cấu hình") || query.Contains("pc"))
            {
                reply.Append("<p>🖥️ <strong>Cấu hình PC Gaming/Đồ họa Đề xuất (Tầm trung - Cao cấp):</strong></p>");
                reply.Append("<ul>");
                reply.Append("<li><strong>CPU:</strong> Intel Core i5-13600K hoặc AMD Ryzen 5 7600X</li>");
                reply.Append("<li><strong>VGA:</strong> NVIDIA RTX 4060 Ti hoặc RTX 4070 (Cho Gaming 2K)</li>");
                reply.Append("<li><strong>RAM:</strong> 32GB DDR5 5600MHz Dual Channel</li>");
                reply.Append("<li><strong>SSD:</strong> 1TB PCIe NVMe Gen 4 (Tốc độ đọc > 5000MB/s)</li>");
                reply.Append("</ul>");

                var desktops = products.Where(p => p.CategoryId == 2).Take(2).ToList();
                if (desktops.Any())
                {
                    reply.Append("<p>📦 <strong>Sản phẩm PC sẵn có tại TechStore:</strong></p>");
                    foreach (var d in desktops)
                    {
                        reply.Append($"- <a href='/Product/Display/{d.Id}' target='_blank' class='fw-bold text-primary'>{d.Name}</a> - <strong class='text-danger'>{d.Price:N2} $</strong><br/>");
                    }
                }
            }
            else if (query.Contains("laptop"))
            {
                reply.Append("<p>💻 <strong>Tư vấn mua Laptop:</strong></p>");
                reply.Append("<p>Đối với laptop gaming và đồ họa chuyên nghiệp, bạn nên chọn các dòng máy sở hữu card đồ họa rời NVIDIA RTX 40 Series và CPU Intel dòng H hoặc AMD dòng HS.</p>");

                var laptops = products.Where(p => p.CategoryId == 1).Take(2).ToList();
                if (laptops.Any())
                {
                    reply.Append("<p>🔥 <strong>Laptop nổi bật tại TechStore:</strong></p>");
                    foreach (var l in laptops)
                    {
                        reply.Append($"- <a href='/Product/Display/{l.Id}' target='_blank' class='fw-bold text-primary'>{l.Name}</a> - <strong class='text-danger'>{l.Price:N2} $</strong><br/>");
                    }
                }
            }
            else if (query.Contains("giá rẻ") || query.Contains("rẻ") || query.Contains("budget"))
            {
                reply.Append("<p>💰 <strong>Sản phẩm phân khúc tiết kiệm (dưới 500 $):</strong></p>");
                var cheapProducts = products.Where(p => p.Price < 500).Take(3).ToList();
                if (cheapProducts.Any())
                {
                    reply.Append("<ul>");
                    foreach (var p in cheapProducts)
                    {
                        reply.Append($"<li><a href='/Product/Display/{p.Id}' target='_blank' class='fw-bold text-primary'>{p.Name}</a> - <span class='text-success fw-bold'>{p.Price:N2} $</span></li>");
                    }
                    reply.Append("</ul>");
                }
                else
                {
                    reply.Append("<p>Hiện tại phân khúc dưới 500$ trong kho đang hết hàng. Hãy xem thêm các phân khúc tầm trung nhé!</p>");
                }
            }
            else if (query.Contains("gaming") || query.Contains("game") || query.Contains("chơi game"))
            {
                reply.Append("<p>🎮 <strong>Yêu cầu Cấu hình chơi Game mượt mà:</strong></p>");
                reply.Append("<p>Để chiến mượt các tựa game AAA hiện nay ở độ phân giải Full HD / 2K, bạn cần tối thiểu VGA RTX 3060 hoặc RTX 4060, đi kèm vi xử lý thế hệ mới có số nhân đơn nhân mạnh mẽ.</p>");
                var items = products.Where(p => p.Name.ToLower().Contains("rog") || p.Name.ToLower().Contains("strix") || p.Price > 1000).Take(2).ToList();
                if (items.Any())
                {
                    reply.Append("<p>⭐ <strong>Thiết bị Gaming đỉnh cao bán chạy:</strong></p>");
                    foreach (var item in items)
                    {
                        reply.Append($"- <a href='/Product/Display/{item.Id}' target='_blank' class='fw-bold text-primary'>{item.Name}</a> - <strong class='text-danger'>{item.Price:N2} $</strong><br/>");
                    }
                }
            }
            else
            {
                reply.Append("<p>🤖 Tôi đã ghi nhận câu hỏi. Bạn có muốn tư vấn chi tiết về:</p>");
                reply.Append("<ul>");
                reply.Append("<li><em>'build pc'</em> - Lên cấu hình linh kiện PC</li>");
                reply.Append("<li><em>'laptop'</em> - Chọn mua Laptop phù hợp học tập / làm việc</li>");
                reply.Append("<li><em>'giá rẻ'</em> - Lọc linh kiện phân khúc giá tốt</li>");
                reply.Append("</ul>");
            }

            return Json(new { reply = reply.ToString() });
        }
    }
}
