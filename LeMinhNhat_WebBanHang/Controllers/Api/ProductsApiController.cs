using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeMinhNhat_WebBanHang.DataAccess;
using LeMinhNhat_WebBanHang.Models;
using LeMinhNhat_WebBanHang.Repositories;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LeMinhNhat_WebBanHang.Controllers.Api
{
    /// <summary>
    /// RESTful API endpoint quản lý sản phẩm.
    /// - GET    /api/products           → Lấy danh sách (hỗ trợ phân trang, tìm kiếm, lọc category)
    /// - GET    /api/products/{id}      → Lấy chi tiết một sản phẩm
    /// - POST   /api/products           → Thêm sản phẩm mới (yêu cầu JWT + role Admin)
    /// - PUT    /api/products/{id}      → Cập nhật toàn bộ sản phẩm (yêu cầu JWT + role Admin)
    /// - PATCH  /api/products/{id}      → Cập nhật một phần sản phẩm (yêu cầu JWT + role Admin)
    /// - DELETE /api/products/{id}      → Xóa sản phẩm (yêu cầu JWT + role Admin)
    /// </summary>
    [Route("api/products")]
    [ApiController]
    [Produces("application/json")]
    public class ProductsApiController : ControllerBase
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ApplicationDbContext _context;

        public ProductsApiController(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            ApplicationDbContext context)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _context = context;
        }

        // ──────────────── DTOs ────────────────

        public class ProductCreateDto
        {
            [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
            [StringLength(100, ErrorMessage = "Tên sản phẩm tối đa 100 ký tự")]
            public string Name { get; set; } = string.Empty;

            [Range(0.01, 10000000, ErrorMessage = "Giá phải lớn hơn 0")]
            public decimal Price { get; set; }

            public string? Description { get; set; }

            [Required(ErrorMessage = "Vui lòng chọn danh mục")]
            public int CategoryId { get; set; }

            public string? ImageUrl { get; set; }
            public List<string>? ImageUrls { get; set; }
        }

        public class ProductUpdateDto
        {
            [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
            [StringLength(100)]
            public string Name { get; set; } = string.Empty;

            [Range(0.01, 10000000)]
            public decimal Price { get; set; }

            public string? Description { get; set; }

            [Required]
            public int CategoryId { get; set; }

            public string? ImageUrl { get; set; }
            public List<string>? ImageUrls { get; set; }
        }

        public class ProductPatchDto
        {
            public string? Name { get; set; }
            public decimal? Price { get; set; }
            public string? Description { get; set; }
            public int? CategoryId { get; set; }
            public string? ImageUrl { get; set; }
        }

        // ──────────────── GET ALL ────────────────

        /// <summary>GET /api/products - Lấy danh sách sản phẩm có phân trang & bộ lọc</summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetAll(
            [FromQuery] string? search = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] string sortBy = "default",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12)
        {
            // Validate page parameters
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 12;

            var query = _context.Products.AsQueryable();

            // Filter by search keyword
            if (!string.IsNullOrWhiteSpace(search))
            {
                var kw = search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(kw) ||
                                         (p.Description != null && p.Description.ToLower().Contains(kw)));
            }

            // Filter by category
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // Filter by price range
            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            // Sorting
            query = sortBy switch
            {
                "priceAsc"  => query.OrderBy(p => p.Price),
                "priceDesc" => query.OrderByDescending(p => p.Price),
                "nameAsc"   => query.OrderBy(p => p.Name),
                "nameDesc"  => query.OrderByDescending(p => p.Name),
                "newest"    => query.OrderByDescending(p => p.Id),
                _           => query.OrderBy(p => p.Id)
            };

            // Total count before pagination (for metadata)
            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Pagination
            var products = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.Description,
                    p.CategoryId,
                    p.ImageUrl,
                    p.ImageUrls,
                    OriginalPrice = Math.Round(p.Price * 1.25m, 2)
                })
                .ToList();

            return Ok(new
            {
                Data = products,
                Pagination = new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    HasPrevious = page > 1,
                    HasNext = page < totalPages
                },
                Filters = new { search, categoryId, minPrice, maxPrice, sortBy }
            });
        }

        // ──────────────── GET BY ID ────────────────

        /// <summary>GET /api/products/{id} - Lấy chi tiết một sản phẩm</summary>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetById(int id)
        {
            var product = _context.Products
                .Where(p => p.Id == id)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.Description,
                    p.CategoryId,
                    p.ImageUrl,
                    p.ImageUrls,
                    OriginalPrice = Math.Round(p.Price * 1.25m, 2)
                })
                .FirstOrDefault();

            if (product == null)
                return NotFound(new { Message = $"Không tìm thấy sản phẩm với ID = {id}" });

            return Ok(product);
        }

        // ──────────────── POST (CREATE) ────────────────

        /// <summary>POST /api/products - Thêm sản phẩm mới (Admin only, JWT required)</summary>
        [HttpPost]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult Create([FromBody] ProductCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = "Dữ liệu không hợp lệ", Errors = ModelState });

            // Validate category exists
            var category = _categoryRepository.GetById(dto.CategoryId);
            if (category == null)
                return BadRequest(new { Message = $"Danh mục ID={dto.CategoryId} không tồn tại" });

            var product = new Product
            {
                Name = dto.Name.Trim(),
                Price = dto.Price,
                Description = dto.Description?.Trim(),
                CategoryId = dto.CategoryId,
                ImageUrl = dto.ImageUrl,
                ImageUrls = dto.ImageUrls ?? new List<string>()
            };

            _productRepository.Add(product);

            return CreatedAtAction(nameof(GetById), new { id = product.Id }, new
            {
                product.Id,
                product.Name,
                product.Price,
                product.Description,
                product.CategoryId,
                product.ImageUrl,
                product.ImageUrls,
                Message = "Thêm sản phẩm thành công!"
            });
        }

        // ──────────────── PUT (FULL UPDATE) ────────────────

        /// <summary>PUT /api/products/{id} - Cập nhật toàn bộ sản phẩm (Admin only, JWT required)</summary>
        [HttpPut("{id:int}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult Update(int id, [FromBody] ProductUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = "Dữ liệu không hợp lệ", Errors = ModelState });

            var existing = _productRepository.GetById(id);
            if (existing == null)
                return NotFound(new { Message = $"Không tìm thấy sản phẩm với ID = {id}" });

            var category = _categoryRepository.GetById(dto.CategoryId);
            if (category == null)
                return BadRequest(new { Message = $"Danh mục ID={dto.CategoryId} không tồn tại" });

            existing.Name = dto.Name.Trim();
            existing.Price = dto.Price;
            existing.Description = dto.Description?.Trim();
            existing.CategoryId = dto.CategoryId;
            existing.ImageUrl = dto.ImageUrl ?? existing.ImageUrl;
            existing.ImageUrls = dto.ImageUrls ?? existing.ImageUrls;

            _productRepository.Update(existing);

            return Ok(new
            {
                existing.Id,
                existing.Name,
                existing.Price,
                existing.Description,
                existing.CategoryId,
                existing.ImageUrl,
                existing.ImageUrls,
                Message = "Cập nhật sản phẩm thành công!"
            });
        }

        // ──────────────── PATCH (PARTIAL UPDATE) ────────────────

        /// <summary>PATCH /api/products/{id} - Cập nhật một phần sản phẩm (Admin only, JWT required)</summary>
        [HttpPatch("{id:int}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult Patch(int id, [FromBody] ProductPatchDto dto)
        {
            var existing = _productRepository.GetById(id);
            if (existing == null)
                return NotFound(new { Message = $"Không tìm thấy sản phẩm với ID = {id}" });

            // Only update provided fields
            if (!string.IsNullOrWhiteSpace(dto.Name))
                existing.Name = dto.Name.Trim();

            if (dto.Price.HasValue && dto.Price.Value > 0)
                existing.Price = dto.Price.Value;

            if (dto.Description != null)
                existing.Description = dto.Description.Trim();

            if (dto.CategoryId.HasValue)
            {
                var category = _categoryRepository.GetById(dto.CategoryId.Value);
                if (category == null)
                    return BadRequest(new { Message = $"Danh mục ID={dto.CategoryId.Value} không tồn tại" });
                existing.CategoryId = dto.CategoryId.Value;
            }

            if (dto.ImageUrl != null)
                existing.ImageUrl = dto.ImageUrl;

            _productRepository.Update(existing);

            return Ok(new
            {
                existing.Id,
                existing.Name,
                existing.Price,
                existing.CategoryId,
                Message = "Cập nhật thành công (partial)!"
            });
        }

        // ──────────────── DELETE ────────────────

        /// <summary>DELETE /api/products/{id} - Xóa sản phẩm (Admin only, JWT required)</summary>
        [HttpDelete("{id:int}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult Delete(int id)
        {
            var existing = _productRepository.GetById(id);
            if (existing == null)
                return NotFound(new { Message = $"Không tìm thấy sản phẩm với ID = {id}" });

            // Check if product has existing order details (prevent orphan protection)
            bool hasOrders = _context.OrderDetails.Any(od => od.ProductId == id);
            if (hasOrders)
                return Conflict(new
                {
                    Message = "Không thể xóa sản phẩm này vì đã có trong đơn hàng. Hãy ẩn sản phẩm thay vì xóa."
                });

            _productRepository.Delete(id);

            return Ok(new { Message = $"Đã xóa sản phẩm ID={id} thành công!" });
        }
    }
}
