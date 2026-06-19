using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LeMinhNhat_WebBanHang.DataAccess;
using LeMinhNhat_WebBanHang.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace LeMinhNhat_WebBanHang.Controllers.Api
{
    /// <summary>
    /// RESTful API endpoint quản lý đơn hàng.
    ///
    /// Endpoint dành cho User (JWT required):
    /// - GET    /api/orders              → Lịch sử đơn hàng của chính người dùng
    /// - GET    /api/orders/{id}         → Chi tiết đơn hàng (chỉ xem của mình)
    /// - POST   /api/orders              → Tạo đơn hàng mới từ danh sách sản phẩm
    /// - PATCH  /api/orders/{id}/cancel  → Huỷ đơn hàng (chỉ khi Pending)
    ///
    /// Endpoint dành cho Admin (JWT + Admin role required):
    /// - GET    /api/orders/all          → Xem tất cả đơn hàng hệ thống
    /// - PUT    /api/orders/{id}/status  → Cập nhật trạng thái đơn hàng
    /// - DELETE /api/orders/{id}         → Xóa đơn hàng (Admin only)
    /// </summary>
    [Route("api/orders")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    public class OrdersApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrdersApiController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ──────────────── DTOs ────────────────

        public class OrderItemDto
        {
            [Required]
            [Range(1, int.MaxValue, ErrorMessage = "ProductId phải lớn hơn 0")]
            public int ProductId { get; set; }

            [Required]
            [Range(1, 1000, ErrorMessage = "Số lượng phải từ 1 đến 1000")]
            public int Quantity { get; set; }
        }

        public class CreateOrderDto
        {
            [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng")]
            [StringLength(255)]
            public string ShippingAddress { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
            [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
            public string PhoneNumber { get; set; } = string.Empty;

            public string? ReceiverName { get; set; }
            public string? Notes { get; set; }

            [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
            public string PaymentMethod { get; set; } = "COD"; // COD | BankTransfer | Wallet

            public string ShippingMethod { get; set; } = "Standard"; // Standard | Express

            public string? CouponCode { get; set; }

            [Required(ErrorMessage = "Danh sách sản phẩm không được rỗng")]
            [MinLength(1, ErrorMessage = "Cần ít nhất 1 sản phẩm")]
            public List<OrderItemDto> Items { get; set; } = new();
        }

        public class UpdateOrderStatusDto
        {
            [Required]
            public string Status { get; set; } = string.Empty; // Pending | Approved | Shipped | Completed | Cancelled
        }

        // ──────────────── GET MY ORDERS ────────────────

        /// <summary>GET /api/orders - Lịch sử đơn hàng của người dùng hiện tại (JWT)</summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyOrders(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Token không hợp lệ hoặc đã hết hạn." });

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 10;

            var query = _context.Orders
                .Where(o => o.UserId == userId)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(o => o.Status == status);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    o.Id,
                    o.OrderDate,
                    o.TotalAmount,
                    o.Status,
                    o.ShippingAddress,
                    o.PhoneNumber,
                    o.PaymentMethod,
                    o.ShippingMethod,
                    o.ShippingFee,
                    o.DiscountAmount,
                    o.CouponCode,
                    o.ReceiverName,
                    ItemCount = o.OrderDetails.Count
                })
                .ToListAsync();

            return Ok(new
            {
                Data = orders,
                Pagination = new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    HasPrevious = page > 1,
                    HasNext = page < totalPages
                }
            });
        }

        // ──────────────── GET ORDER DETAIL ────────────────

        /// <summary>GET /api/orders/{id} - Chi tiết đơn hàng (chỉ xem đơn của mình)</summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound(new { Message = $"Không tìm thấy đơn hàng ID = {id}" });

            // Security: Non-admin can only view their own orders
            if (!isAdmin && order.UserId != userId)
                return StatusCode(403, new { Message = "Bạn không có quyền xem đơn hàng này." });

            return Ok(new
            {
                order.Id,
                order.OrderDate,
                order.TotalAmount,
                order.Status,
                order.ShippingAddress,
                order.PhoneNumber,
                order.ReceiverName,
                order.Notes,
                order.PaymentMethod,
                order.ShippingMethod,
                order.ShippingFee,
                order.DiscountAmount,
                order.CouponCode,
                order.Province,
                order.District,
                order.Ward,
                OrderDetails = order.OrderDetails.Select(od => new
                {
                    od.Id,
                    od.ProductId,
                    ProductName = od.Product?.Name ?? "N/A",
                    ProductImage = od.Product?.ImageUrl ?? "",
                    od.Quantity,
                    od.Price,
                    LineTotal = od.Quantity * od.Price
                })
            });
        }

        // ──────────────── POST (CREATE ORDER) ────────────────

        /// <summary>POST /api/orders - Tạo đơn hàng mới từ mobile app (JWT required)</summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = "Dữ liệu không hợp lệ", Errors = ModelState });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Token không hợp lệ hoặc đã hết hạn." });

            // Validate payment method
            var validPayments = new[] { "COD", "BankTransfer", "Wallet", "CreditCard" };
            if (!validPayments.Contains(dto.PaymentMethod))
                return BadRequest(new { Message = $"Phương thức thanh toán không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validPayments)}" });

            // Validate shipping method
            var validShipping = new[] { "Standard", "Express" };
            if (!validShipping.Contains(dto.ShippingMethod))
                return BadRequest(new { Message = $"Phương thức vận chuyển không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validShipping)}" });

            // Load & validate all products
            var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            // Check all products exist
            var missingProductIds = productIds.Except(products.Select(p => p.Id)).ToList();
            if (missingProductIds.Any())
                return BadRequest(new
                {
                    Message = "Một số sản phẩm không tồn tại trong hệ thống.",
                    MissingProductIds = missingProductIds
                });

            // Calculate order totals
            decimal subtotal = dto.Items.Sum(item =>
            {
                var product = products.First(p => p.Id == item.ProductId);
                return product.Price * item.Quantity;
            });

            // Apply coupon if provided
            decimal discount = 0;
            string? appliedCoupon = null;
            if (!string.IsNullOrWhiteSpace(dto.CouponCode))
            {
                var coupon = await _context.Coupons.FirstOrDefaultAsync(c =>
                    c.Code == dto.CouponCode && c.IsActive && c.ExpiryDate >= DateTime.Now);

                if (coupon == null)
                    return BadRequest(new { Message = "Mã giảm giá không hợp lệ hoặc đã hết hạn." });

                if (subtotal < coupon.MinimumOrderAmount)
                    return BadRequest(new
                    {
                        Message = $"Đơn hàng phải từ ${coupon.MinimumOrderAmount} để dùng mã này. Hiện tại: ${subtotal}"
                    });

                discount = coupon.DiscountType == "Percentage"
                    ? Math.Round(subtotal * (coupon.DiscountValue / 100), 2)
                    : coupon.DiscountValue;

                appliedCoupon = dto.CouponCode;
            }

            // Calculate shipping fee
            decimal shippingFee = (dto.ShippingMethod == "Express") ? 30.00m : 15.00m;
            if (subtotal >= 200m || appliedCoupon == "FREESHIP") shippingFee = 0;

            decimal grandTotal = Math.Max(0, subtotal + shippingFee - discount);

            // Create Order entity
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                TotalAmount = grandTotal,
                Status = "Pending",
                ShippingAddress = dto.ShippingAddress.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                ReceiverName = dto.ReceiverName?.Trim(),
                Notes = dto.Notes?.Trim(),
                PaymentMethod = dto.PaymentMethod,
                ShippingMethod = dto.ShippingMethod,
                ShippingFee = shippingFee,
                DiscountAmount = discount,
                CouponCode = appliedCoupon
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Create OrderDetails
            foreach (var item in dto.Items)
            {
                var product = products.First(p => p.Id == item.ProductId);
                _context.OrderDetails.Add(new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = product.Price
                });
            }
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = order.Id }, new
            {
                order.Id,
                order.OrderDate,
                order.Status,
                order.TotalAmount,
                order.ShippingFee,
                order.DiscountAmount,
                order.CouponCode,
                ItemCount = dto.Items.Count,
                Message = "Đặt hàng thành công!"
            });
        }

        // ──────────────── PATCH CANCEL (USER) ────────────────

        /// <summary>PATCH /api/orders/{id}/cancel - Huỷ đơn hàng (chỉ khi trạng thái Pending)</summary>
        [HttpPatch("{id:int}/cancel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound(new { Message = $"Không tìm thấy đơn hàng ID = {id}" });

            // Security: Only owner or Admin can cancel
            if (!isAdmin && order.UserId != userId)
                return StatusCode(403, new { Message = "Bạn không có quyền huỷ đơn hàng này." });

            if (order.Status != "Pending")
                return BadRequest(new
                {
                    Message = $"Không thể huỷ đơn hàng ở trạng thái '{order.Status}'. Chỉ có thể huỷ khi trạng thái là Pending."
                });

            order.Status = "Cancelled";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                order.Id,
                order.Status,
                Message = "Đã huỷ đơn hàng thành công!"
            });
        }

        // ──────────────── GET ALL ORDERS (ADMIN) ────────────────

        /// <summary>GET /api/orders/all - Lấy tất cả đơn hàng hệ thống (Admin only)</summary>
        [HttpGet("all")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllOrders(
            [FromQuery] string? status = null,
            [FromQuery] string? userId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var query = _context.Orders.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(o => o.Status == status);

            if (!string.IsNullOrWhiteSpace(userId))
                query = query.Where(o => o.UserId == userId);

            if (fromDate.HasValue)
                query = query.Where(o => o.OrderDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(o => o.OrderDate <= toDate.Value.AddDays(1));

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    o.Id,
                    o.UserId,
                    UserEmail = o.User != null ? o.User.Email : "N/A",
                    UserFullName = o.User != null ? o.User.FullName : "N/A",
                    o.OrderDate,
                    o.TotalAmount,
                    o.Status,
                    o.ShippingAddress,
                    o.PhoneNumber,
                    o.ReceiverName,
                    o.PaymentMethod,
                    o.ShippingMethod,
                    o.CouponCode,
                    ItemCount = o.OrderDetails.Count
                })
                .ToListAsync();

            // Summary statistics for admin dashboard
            var statusSummary = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count(), Revenue = g.Sum(o => o.TotalAmount) })
                .ToListAsync();

            return Ok(new
            {
                Data = orders,
                Summary = statusSummary,
                Pagination = new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages
                }
            });
        }

        // ──────────────── PUT STATUS (ADMIN) ────────────────

        /// <summary>PUT /api/orders/{id}/status - Cập nhật trạng thái đơn hàng (Admin only)</summary>
        [HttpPut("{id:int}/status")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            var validStatuses = new[] { "Pending", "Approved", "Shipped", "Completed", "Cancelled" };
            if (!validStatuses.Contains(dto.Status))
                return BadRequest(new
                {
                    Message = $"Trạng thái không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validStatuses)}"
                });

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound(new { Message = $"Không tìm thấy đơn hàng ID = {id}" });

            // Enforce valid status transitions
            var allowedTransitions = new Dictionary<string, string[]>
            {
                { "Pending",    new[] { "Approved", "Cancelled" } },
                { "Approved",   new[] { "Shipped",  "Cancelled" } },
                { "Shipped",    new[] { "Completed" } },
                { "Completed",  Array.Empty<string>() },
                { "Cancelled",  Array.Empty<string>() }
            };

            if (!allowedTransitions.TryGetValue(order.Status, out var allowed) ||
                !allowed.Contains(dto.Status))
            {
                return BadRequest(new
                {
                    Message = $"Không thể chuyển trạng thái từ '{order.Status}' sang '{dto.Status}'.",
                    AllowedNext = allowedTransitions.GetValueOrDefault(order.Status, Array.Empty<string>())
                });
            }

            var previousStatus = order.Status;
            order.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                order.Id,
                PreviousStatus = previousStatus,
                CurrentStatus = order.Status,
                UpdatedAt = DateTime.Now,
                Message = $"Đã cập nhật trạng thái đơn hàng #{id} thành '{dto.Status}'"
            });
        }

        // ──────────────── DELETE (ADMIN) ────────────────

        /// <summary>DELETE /api/orders/{id} - Xóa đơn hàng vĩnh viễn (Admin only)</summary>
        [HttpDelete("{id:int}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound(new { Message = $"Không tìm thấy đơn hàng ID = {id}" });

            // Safety: only allow deletion of cancelled or completed orders
            if (order.Status != "Cancelled" && order.Status != "Completed")
                return BadRequest(new
                {
                    Message = $"Chỉ được xóa đơn hàng ở trạng thái 'Cancelled' hoặc 'Completed'. Hiện tại: '{order.Status}'"
                });

            // Remove details first (cascade)
            _context.OrderDetails.RemoveRange(order.OrderDetails);
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Đã xóa đơn hàng #{id} thành công!" });
        }
    }
}
