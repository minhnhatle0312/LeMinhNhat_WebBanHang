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
            return View(cart);
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
                return Json(new { success = true, totalItems = cart.Sum(i => i.Quantity), grandTotal = cart.Sum(i => i.TotalPrice) });
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
                return Json(new { success = true, totalItems = cart.Sum(i => i.Quantity), grandTotal = cart.Sum(i => i.TotalPrice) });
            }
            return Json(new { success = false, message = "Sản phẩm không có trong giỏ hàng" });
        }

        [HttpGet]
        public IActionResult GetCartSummary()
        {
            var cart = GetCartItems();
            return Json(new { items = cart, totalItems = cart.Sum(i => i.Quantity), grandTotal = cart.Sum(i => i.TotalPrice) });
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

            var checkoutModel = new Order
            {
                UserId = user.Id,
                ShippingAddress = user.Address ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                TotalAmount = cart.Sum(i => i.TotalPrice)
            };

            ViewBag.CartItems = cart;
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

            // Setup proper backend states
            orderModel.UserId = user.Id;
            orderModel.OrderDate = DateTime.Now;
            orderModel.TotalAmount = cart.Sum(i => i.TotalPrice);
            orderModel.Status = "Pending";

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

                // Clear Cart Session
                HttpContext.Session.Remove(CART_SESSION_KEY);

                return RedirectToAction(nameof(OrderSuccess), new { id = orderModel.Id });
            }

            ViewBag.CartItems = cart;
            return View(orderModel);
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
