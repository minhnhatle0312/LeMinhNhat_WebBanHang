using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeMinhNhat_WebBanHang.Models;
using LeMinhNhat_WebBanHang.DataAccess;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeMinhNhat_WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class OrderManagerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderManagerController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            await CheckAndAwardPoints(order, status);

            order.Status = status;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật trạng thái đơn hàng thành công!";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatusAjax(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return Json(new { success = false, message = "Đơn hàng không tồn tại" });

            await CheckAndAwardPoints(order, status);

            order.Status = status;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cập nhật trạng thái thành công!", status = status });
        }

        [HttpGet]
        public async Task<IActionResult> ExportOrdersCsv()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var csv = new StringBuilder();
            // CSV Header with BOM prefix to support UTF-8 (Excel friendly)
            csv.AppendLine("Ma Don,Khach Hang,Email,Ngay Dat,Tong Tien,Phi Giao,Khuyen Mai,Voucher,Trang Thai,Sdt,Dia Chi");

            foreach (var order in orders)
            {
                string customerName = (order.ReceiverName ?? order.User?.FullName ?? "N/A").Replace(",", " ");
                string email = (order.User?.Email ?? "N/A").Replace(",", " ");
                string date = order.OrderDate.ToString("dd/MM/yyyy HH:mm");
                string total = order.TotalAmount.ToString("N2");
                string ship = order.ShippingFee.ToString("N2");
                string discount = order.DiscountAmount.ToString("N2");
                string voucher = (order.CouponCode ?? "").Replace(",", " ");
                string status = order.Status;
                string phone = order.PhoneNumber;
                string address = order.ShippingAddress.Replace(",", " ").Replace("\n", " ").Replace("\r", " ");

                csv.AppendLine($"{order.Id},{customerName},{email},{date},{total},{ship},{discount},{voucher},{status},{phone},{address}");
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return File(bytes, "text/csv", $"TechStore_DonHang_{DateTime.Now:yyyyMMddHHmmss}.csv");
        }

        private async Task CheckAndAwardPoints(Order order, string newStatus)
        {
            string oldStatus = order.Status;
            // Transitioning to Completed/Delivered, and was not previously Completed
            if ((newStatus == "Completed" || newStatus == "Delivered") && 
                oldStatus != "Completed" && oldStatus != "Delivered")
            {
                var user = await _context.Users.FindAsync(order.UserId);
                if (user != null)
                {
                    // Earn 1 point per $1 spent
                    int pointsEarned = (int)Math.Round(order.TotalAmount);
                    user.LoyaltyPoints += pointsEarned;

                    // Update membership rank
                    if (user.LoyaltyPoints >= 3000)
                        user.MembershipRank = "Kim Cương";
                    else if (user.LoyaltyPoints >= 1500)
                        user.MembershipRank = "Vàng";
                    else if (user.LoyaltyPoints >= 500)
                        user.MembershipRank = "Bạc";
                    else
                        user.MembershipRank = "Đồng";
                }
            }
        }
    }
}
