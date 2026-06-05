using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeMinhNhat_WebBanHang.Models;
using LeMinhNhat_WebBanHang.DataAccess;
using System.Linq;
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

            order.Status = status;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật trạng thái đơn hàng thành công!";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }
    }
}
