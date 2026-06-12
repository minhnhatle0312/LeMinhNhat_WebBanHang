using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LeMinhNhat_WebBanHang.Models;
using LeMinhNhat_WebBanHang.Repositories;
using LeMinhNhat_WebBanHang.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeMinhNhat_WebBanHang.Controllers
{
    public class CartController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private const string CART_SESSION_KEY = "TechStore_Cart";

        public CartController(
            IProductRepository productRepository, 
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _productRepository = productRepository;
            _userManager = userManager;
            _context = context;
        }

        private List<CartItem> GetCartItems()
        {
            var sessionData = HttpContext.Session.GetString(CART_SESSION_KEY);
            return string.IsNullOrEmpty(sessionData) 
                ? new List<CartItem>() 
                : JsonSerializer.Deserialize<List<CartItem>>(sessionData);
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString(CART_SESSION_KEY, JsonSerializer.Serialize(cart));
        }

        public IActionResult Index()
        {
            var cart = GetCartItems();
            var cartProductIds = cart.Select(i => i.ProductId).ToList();

            // Get 4 products that are not currently in the cart for cross-selling
            var recommended = _productRepository.GetAll()
                .Where(p => !cartProductIds.Contains(p.Id))
                .Take(4)
                .ToList();

            ViewBag.RecommendedProducts = recommended;

            // Calculate pricing summary
            decimal subtotal = cart.Sum(i => i.TotalPrice);
            var coupon = GetAppliedCoupon(subtotal);
            ViewBag.CouponCode = coupon?.Code ?? "";
            ViewBag.Discount = CalculateDiscount(coupon, subtotal);
            ViewBag.ShippingFee = CalculateShippingFee(subtotal, "Standard", coupon);
            ViewBag.GrandTotal = Math.Max(0, subtotal + ViewBag.ShippingFee - ViewBag.Discount);

            return View(cart);
        }

        private Coupon? GetAppliedCoupon(decimal subtotal)
        {
            var code = HttpContext.Session.GetString("TechStore_Coupon");
            if (string.IsNullOrEmpty(code)) return null;

            var coupon = _context.Coupons.FirstOrDefault(c => c.Code == code && c.IsActive && c.ExpiryDate >= DateTime.Now);
            if (coupon == null || subtotal < coupon.MinimumOrderAmount)
            {
                HttpContext.Session.Remove("TechStore_Coupon");
                return null;
            }
            return coupon;
        }

        private decimal CalculateDiscount(Coupon? coupon, decimal subtotal)
        {
            if (coupon == null) return 0;
            if (coupon.DiscountType == "Percentage")
            {
                return Math.Round(subtotal * (coupon.DiscountValue / 100), 2);
            }
            else
            {
                return coupon.DiscountValue;
            }
        }

        private decimal CalculateShippingFee(decimal subtotal, string? shippingMethod, Coupon? coupon)
        {
            if (subtotal == 0) return 0;
            if (coupon != null && coupon.Code == "FREESHIP") return 0;

            decimal baseFee = (shippingMethod == "Express") ? 30.00m : 15.00m;
            if (subtotal >= 200.00m)
            {
                return 0; // Free standard shipping for orders >= $200
            }
            return baseFee;
        }

        [HttpPost]
        public IActionResult ApplyCoupon(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                HttpContext.Session.Remove("TechStore_Coupon");
                return Json(new { success = false, message = "Vui lòng nhập mã giảm giá" });
            }

            var coupon = _context.Coupons.FirstOrDefault(c => c.Code == code && c.IsActive && c.ExpiryDate >= DateTime.Now);
            if (coupon == null)
            {
                return Json(new { success = false, message = "Mã giảm giá không tồn tại hoặc đã hết hạn" });
            }

            var cart = GetCartItems();
            decimal subTotal = cart.Sum(i => i.TotalPrice);

            if (subTotal < coupon.MinimumOrderAmount)
            {
                return Json(new { success = false, message = $"Đơn hàng tối thiểu phải từ ${coupon.MinimumOrderAmount} để sử dụng mã này" });
            }

            HttpContext.Session.SetString("TechStore_Coupon", code);

            decimal discount = CalculateDiscount(coupon, subTotal);
            decimal shippingFee = CalculateShippingFee(subTotal, "Standard", coupon);
            decimal grandTotal = Math.Max(0, subTotal + shippingFee - discount);

            return Json(new { 
                success = true, 
                message = $"Áp dụng mã thành công: {coupon.Description}",
                discount = discount,
                shippingFee = shippingFee,
                grandTotal = grandTotal
            });
        }

        [HttpPost]
        public IActionResult AddToCart(int productId, int quantity = 1)
        {
            var product = _productRepository.GetById(productId);
            if (product == null) return Json(new { success = false, message = "Sản phẩm không tồn tại" });

            var cart = GetCartItems();
            var existing = cart.FirstOrDefault(c => c.ProductId == productId);
            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductImage = product.ImageUrl ?? "/images/no-image.png",
                    Price = product.Price,
                    Quantity = quantity
                });
            }

            SaveCart(cart);
            return Json(new { success = true, totalItems = cart.Sum(i => i.Quantity), grandTotal = cart.Sum(i => i.TotalPrice) });
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            if (quantity <= 0)
            {
                return RemoveFromCart(productId);
            }

            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                item.Quantity = quantity;
                SaveCart(cart);

                decimal subTotal = cart.Sum(i => i.TotalPrice);
                var coupon = GetAppliedCoupon(subTotal);
                decimal discount = CalculateDiscount(coupon, subTotal);
                decimal shippingFee = CalculateShippingFee(subTotal, "Standard", coupon);
                decimal grandTotal = Math.Max(0, subTotal + shippingFee - discount);

                return Json(new { 
                    success = true, 
                    totalItems = cart.Sum(i => i.Quantity), 
                    itemTotal = item.TotalPrice,
                    subTotal = subTotal,
                    discount = discount,
                    shippingFee = shippingFee,
                    grandTotal = grandTotal,
                    couponCode = coupon?.Code ?? "",
                    couponMessage = coupon != null ? $"Mã '{coupon.Code}' đang được áp dụng" : ""
                });
            }
            return Json(new { success = false, message = "Sản phẩm không có trong giỏ hàng" });
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);

                decimal subTotal = cart.Sum(i => i.TotalPrice);
                var coupon = GetAppliedCoupon(subTotal);
                decimal discount = CalculateDiscount(coupon, subTotal);
                decimal shippingFee = CalculateShippingFee(subTotal, "Standard", coupon);
                decimal grandTotal = Math.Max(0, subTotal + shippingFee - discount);

                return Json(new { 
                    success = true, 
                    totalItems = cart.Sum(i => i.Quantity),
                    subTotal = subTotal,
                    discount = discount,
                    shippingFee = shippingFee,
                    grandTotal = grandTotal,
                    couponCode = coupon?.Code ?? "",
                    couponMessage = coupon != null ? $"Mã '{coupon.Code}' đang được áp dụng" : ""
                });
            }
            return Json(new { success = false, message = "Sản phẩm không có trong giỏ hàng" });
        }

        [HttpGet]
        public IActionResult GetCartSummary()
        {
            var cart = GetCartItems();
            decimal subTotal = cart.Sum(i => i.TotalPrice);
            var coupon = GetAppliedCoupon(subTotal);
            decimal discount = CalculateDiscount(coupon, subTotal);
            decimal shippingFee = CalculateShippingFee(subTotal, "Standard", coupon);
            decimal grandTotal = Math.Max(0, subTotal + shippingFee - discount);

            return Json(new { 
                items = cart, 
                totalItems = cart.Sum(i => i.Quantity), 
                subTotal = subTotal,
                discount = discount,
                shippingFee = shippingFee,
                grandTotal = grandTotal
            });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCartItems();
            if (cart.Count == 0)
            {
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            decimal subTotal = cart.Sum(i => i.TotalPrice);
            var coupon = GetAppliedCoupon(subTotal);
            decimal discount = CalculateDiscount(coupon, subTotal);
            decimal shippingFee = CalculateShippingFee(subTotal, "Standard", coupon);
            decimal grandTotal = Math.Max(0, subTotal + shippingFee - discount);

            var checkoutModel = new Order
            {
                UserId = user.Id,
                ReceiverName = user.FullName,
                ShippingAddress = user.Address ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                Province = "",
                District = "",
                Ward = "",
                ShippingMethod = "Standard",
                PaymentMethod = "CreditCard",
                ShippingFee = shippingFee,
                DiscountAmount = discount,
                CouponCode = coupon?.Code,
                TotalAmount = grandTotal
            };

            ViewBag.CartItems = cart;
            ViewBag.SubTotal = subTotal;
            ViewBag.Discount = discount;
            ViewBag.ShippingFee = shippingFee;
            ViewBag.GrandTotal = grandTotal;
            ViewBag.CouponCode = coupon?.Code ?? "";

            return View(checkoutModel);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Checkout(Order orderModel)
        {
            var cart = GetCartItems();
            if (cart.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Giỏ hàng của bạn đang trống!");
                ViewBag.CartItems = cart;
                return View(orderModel);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            decimal subTotal = cart.Sum(i => i.TotalPrice);
            var coupon = GetAppliedCoupon(subTotal);
            decimal discount = CalculateDiscount(coupon, subTotal);
            decimal shippingFee = CalculateShippingFee(subTotal, orderModel.ShippingMethod, coupon);
            decimal grandTotal = Math.Max(0, subTotal + shippingFee - discount);

            // Re-assign proper backend states
            orderModel.UserId = user.Id;
            orderModel.OrderDate = DateTime.Now;
            orderModel.TotalAmount = grandTotal;
            orderModel.DiscountAmount = discount;
            orderModel.ShippingFee = shippingFee;
            orderModel.CouponCode = coupon?.Code;
            orderModel.Status = "Pending";

            // Format address nicely
            string detailedAddress = orderModel.ShippingAddress;
            if (!string.IsNullOrEmpty(orderModel.Ward) && !string.IsNullOrEmpty(orderModel.District) && !string.IsNullOrEmpty(orderModel.Province))
            {
                detailedAddress = $"{orderModel.ShippingAddress}, {orderModel.Ward}, {orderModel.District}, {orderModel.Province}";
            }
            orderModel.ShippingAddress = detailedAddress;

            // Remove model state validations for navigation properties
            ModelState.Remove("User");
            ModelState.Remove("UserId");

            if (ModelState.IsValid)
            {
                _context.Orders.Add(orderModel);
                await _context.SaveChangesAsync();

                // Save OrderDetails
                foreach (var item in cart)
                {
                    var detail = new OrderDetail
                    {
                        OrderId = orderModel.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.Price
                    };
                    _context.OrderDetails.Add(detail);
                }
                await _context.SaveChangesAsync();

                // Clear Cart Session and Coupon Session
                HttpContext.Session.Remove(CART_SESSION_KEY);
                HttpContext.Session.Remove("TechStore_Coupon");

                return RedirectToAction(nameof(OrderSuccess), new { id = orderModel.Id });
            }

            ViewBag.CartItems = cart;
            ViewBag.SubTotal = subTotal;
            ViewBag.Discount = discount;
            ViewBag.ShippingFee = shippingFee;
            ViewBag.GrandTotal = grandTotal;
            ViewBag.CouponCode = coupon?.Code ?? "";
            return View(orderModel);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ReOrder(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == user.Id);

            if (order == null) return NotFound();

            var cart = GetCartItems();

            foreach (var item in order.OrderDetails)
            {
                var existing = cart.FirstOrDefault(c => c.ProductId == item.ProductId);
                if (existing != null)
                {
                    existing.Quantity += item.Quantity;
                }
                else
                {
                    cart.Add(new CartItem
                    {
                        ProductId = item.ProductId,
                        ProductName = item.Product?.Name ?? "Sản phẩm",
                        ProductImage = item.Product?.ImageUrl ?? "/images/no-image.png",
                        Price = item.Price,
                        Quantity = item.Quantity
                    });
                }
            }

            SaveCart(cart);

            TempData["Success"] = "Đã thêm lại tất cả sản phẩm của đơn hàng #" + orderId + " vào giỏ hàng.";
            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> OrderSuccess(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null || order.UserId != _userManager.GetUserId(User))
            {
                return NotFound();
            }

            return View(order);
        }
    }
}
