using Microsoft.AspNetCore.Mvc;
using LeMinhNhat_WebBanHang.Repositories;
using LeMinhNhat_WebBanHang.Models;

namespace LeMinhNhat_WebBanHang.Controllers
{
    public class FlashSaleController : Controller
    {
        private readonly IProductRepository _productRepository;
        private static readonly object _lock = new object();

        // Simulated flash sale data — in production this would be in a DB/Cache
        // Sale items: product IDs with their discounted price and limited stock
        private static readonly Dictionary<int, FlashSaleItem> _saleItems = new Dictionary<int, FlashSaleItem>();
        private static DateTime _saleEndTime = DateTime.UtcNow.AddMinutes(30);
        private static bool _saleInitialized = false;

        public FlashSaleController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
            EnsureSaleInitialized();
        }

        private void EnsureSaleInitialized()
        {
            lock (_lock)
            {
                if (!_saleInitialized || DateTime.UtcNow > _saleEndTime)
                {
                    // Reset sale every 30 minutes with new products
                    var allProducts = _productRepository.GetAll().ToList();
                    _saleItems.Clear();

                    // Pick up to 4 products for the flash sale with random discounts
                    var rng = new Random();
                    var selected = allProducts.OrderBy(_ => rng.Next()).Take(4).ToList();

                    foreach (var p in selected)
                    {
                        var discountPct = rng.Next(10, 40); // 10–39% off
                        var stock = rng.Next(2, 8);         // 2–7 units
                        _saleItems[p.Id] = new FlashSaleItem
                        {
                            ProductId = p.Id,
                            ProductName = p.Name,
                            ImageUrl = p.ImageUrl ?? "/images/no-image.png",
                            OriginalPrice = p.Price,
                            DiscountPercent = discountPct,
                            SalePrice = Math.Round(p.Price * (1 - discountPct / 100m), 2),
                            StockLeft = stock,
                            TotalStock = stock
                        };
                    }

                    _saleEndTime = DateTime.UtcNow.AddMinutes(30);
                    _saleInitialized = true;
                }
            }
        }

        // GET /FlashSale/Queue — waiting room
        public IActionResult Queue()
        {
            ViewBag.SaleEndTime = _saleEndTime.ToString("o"); // ISO 8601 for JS
            return View();
        }

        // GET /FlashSale/Sale — the actual flash sale grid
        public IActionResult Sale()
        {
            var items = _saleItems.Values.ToList();
            ViewBag.SaleEndTime = _saleEndTime.ToString("o");
            return View(items);
        }

        // POST /FlashSale/BuyNow — purchase one unit from the flash sale
        [HttpPost]
        public IActionResult BuyNow(int productId)
        {
            lock (_lock)
            {
                if (_saleItems.TryGetValue(productId, out var item))
                {
                    if (item.StockLeft > 0)
                    {
                        item.StockLeft--;
                        return Json(new { success = true, stockLeft = item.StockLeft, message = "Đã thêm vào giỏ hàng Flash Sale!" });
                    }
                    return Json(new { success = false, message = "Hết hàng rồi! Nhanh hơn lần sau nhé 😢" });
                }
            }
            return Json(new { success = false, message = "Sản phẩm không tồn tại trong Flash Sale." });
        }

        // GET /FlashSale/Status — returns remaining time + stock for AJAX polling
        [HttpGet]
        public IActionResult Status()
        {
            var secondsLeft = (int)Math.Max(0, (_saleEndTime - DateTime.UtcNow).TotalSeconds);
            var items = _saleItems.Values.Select(i => new {
                i.ProductId, i.StockLeft, i.TotalStock
            }).ToList();

            return Json(new { secondsLeft, items });
        }
    }

    public class FlashSaleItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public decimal OriginalPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int DiscountPercent { get; set; }
        public int StockLeft { get; set; }
        public int TotalStock { get; set; }
    }
}
