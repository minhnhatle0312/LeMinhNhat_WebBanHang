using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeMinhNhat_WebBanHang.DataAccess;
using LeMinhNhat_WebBanHang.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeMinhNhat_WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Calculate General KPIs
            var completedOrders = await _context.Orders
                .Where(o => o.Status == "Completed" || o.Status == "Delivered")
                .ToListAsync();

            var totalOrdersList = await _context.Orders.ToListAsync();

            decimal totalRevenue = completedOrders.Sum(o => o.TotalAmount);
            int totalOrdersCount = totalOrdersList.Count;
            
            // Average Order Value (AOV)
            decimal aov = completedOrders.Any() ? totalRevenue / completedOrders.Count : 0;

            // Conversion Rate calculation (simulated based on visitors and order count)
            // If we have 0 users, assume baseline visits, else base on user count
            int registeredUsersCount = await _context.Users.CountAsync();
            int simulatedSessions = Math.Max(100, registeredUsersCount * 25 + 42);
            double conversionRate = totalOrdersCount > 0 
                ? Math.Round(((double)totalOrdersCount / simulatedSessions) * 100, 2) 
                : 0.0;

            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalOrders = totalOrdersCount;
            ViewBag.AOV = aov;
            ViewBag.ConversionRate = conversionRate;
            ViewBag.VisitorSessions = simulatedSessions;

            // 2. Fetch Last 6 Months Revenue Trend
            var last6Months = Enumerable.Range(0, 6)
                .Select(i => DateTime.Today.AddMonths(-i))
                .Select(d => new { Year = d.Year, Month = d.Month })
                .Reverse()
                .ToList();

            var monthlyData = await _context.Orders
                .Where(o => (o.Status == "Completed" || o.Status == "Delivered") && o.OrderDate >= DateTime.Today.AddMonths(-5))
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new { 
                    Year = g.Key.Year, 
                    Month = g.Key.Month, 
                    Revenue = g.Sum(o => o.TotalAmount) 
                })
                .ToListAsync();

            var chartLabels = new List<string>();
            var chartValues = new List<decimal>();

            foreach (var m in last6Months)
            {
                chartLabels.Add($"{m.Month}/{m.Year}");
                var matched = monthlyData.FirstOrDefault(d => d.Year == m.Year && d.Month == m.Month);
                chartValues.Add(matched?.Revenue ?? 0);
            }

            ViewBag.ChartLabels = chartLabels;
            ViewBag.ChartValues = chartValues;

            // 3. Top 5 Selling Products ranking
            var topProducts = await _context.OrderDetails
                .Include(od => od.Product)
                .GroupBy(od => od.ProductId)
                .Select(g => new TopSellingProductViewModel
                {
                    ProductId = g.Key,
                    ProductName = g.Max(od => od.Product != null ? od.Product.Name : "Sản phẩm"),
                    ProductImage = g.Max(od => od.Product != null ? od.Product.ImageUrl : "/images/no-image.png"),
                    Price = g.Max(od => od.Price),
                    QuantitySold = g.Sum(od => od.Quantity),
                    TotalRevenue = g.Sum(od => od.Quantity * od.Price)
                })
                .OrderByDescending(p => p.QuantitySold)
                .Take(5)
                .ToListAsync();

            ViewBag.TopProducts = topProducts;

            return View();
        }
    }

    public class TopSellingProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public decimal Price { get; set; }
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
