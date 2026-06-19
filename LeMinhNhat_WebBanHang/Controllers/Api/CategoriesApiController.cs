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
    /// RESTful API endpoint quản lý danh mục sản phẩm.
    /// - GET    /api/categories         → Lấy toàn bộ danh mục (public)
    /// - GET    /api/categories/{id}    → Lấy chi tiết một danh mục (public)
    /// - GET    /api/categories/{id}/products  → Lấy sản phẩm thuộc danh mục (public)
    /// - POST   /api/categories         → Tạo danh mục mới (Admin only)
    /// - PUT    /api/categories/{id}    → Cập nhật danh mục (Admin only)
    /// - DELETE /api/categories/{id}    → Xóa danh mục (Admin only)
    /// </summary>
    [Route("api/categories")]
    [ApiController]
    [Produces("application/json")]
    public class CategoriesApiController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ApplicationDbContext _context;

        public CategoriesApiController(
            ICategoryRepository categoryRepository,
            ApplicationDbContext context)
        {
            _categoryRepository = categoryRepository;
            _context = context;
        }

        // ──────────────── DTOs ────────────────

        public class CategoryDto
        {
            [Required(ErrorMessage = "Tên danh mục không được để trống")]
            [StringLength(50, ErrorMessage = "Tên danh mục tối đa 50 ký tự")]
            public string Name { get; set; } = string.Empty;
        }

        // ──────────────── GET ALL ────────────────

        /// <summary>GET /api/categories - Lấy tất cả danh mục kèm số lượng sản phẩm</summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetAll()
        {
            var categories = _context.Categories
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    ProductCount = _context.Products.Count(p => p.CategoryId == c.Id)
                })
                .OrderBy(c => c.Id)
                .ToList();

            return Ok(new
            {
                Data = categories,
                Total = categories.Count
            });
        }

        // ──────────────── GET BY ID ────────────────

        /// <summary>GET /api/categories/{id} - Lấy chi tiết danh mục</summary>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetById(int id)
        {
            var category = _context.Categories
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    ProductCount = _context.Products.Count(p => p.CategoryId == c.Id)
                })
                .FirstOrDefault();

            if (category == null)
                return NotFound(new { Message = $"Không tìm thấy danh mục ID = {id}" });

            return Ok(category);
        }

        // ──────────────── GET PRODUCTS BY CATEGORY ────────────────

        /// <summary>GET /api/categories/{id}/products - Lấy sản phẩm thuộc danh mục</summary>
        [HttpGet("{id:int}/products")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetProductsByCategory(int id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12)
        {
            var category = _categoryRepository.GetById(id);
            if (category == null)
                return NotFound(new { Message = $"Không tìm thấy danh mục ID = {id}" });

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 12;

            var query = _context.Products.Where(p => p.CategoryId == id);
            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var products = query
                .OrderBy(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.Description,
                    p.ImageUrl
                })
                .ToList();

            return Ok(new
            {
                Category = new { category.Id, category.Name },
                Data = products,
                Pagination = new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages
                }
            });
        }

        // ──────────────── POST (CREATE) ────────────────

        /// <summary>POST /api/categories - Tạo danh mục mới (Admin only)</summary>
        [HttpPost]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult Create([FromBody] CategoryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = "Dữ liệu không hợp lệ", Errors = ModelState });

            // Check duplicate name
            bool exists = _context.Categories.Any(c => c.Name.ToLower() == dto.Name.ToLower().Trim());
            if (exists)
                return Conflict(new { Message = $"Danh mục '{dto.Name}' đã tồn tại trong hệ thống!" });

            var category = new Category { Name = dto.Name.Trim() };
            _categoryRepository.Add(category);

            return CreatedAtAction(nameof(GetById), new { id = category.Id }, new
            {
                category.Id,
                category.Name,
                Message = "Tạo danh mục thành công!"
            });
        }

        // ──────────────── PUT (FULL UPDATE) ────────────────

        /// <summary>PUT /api/categories/{id} - Cập nhật danh mục (Admin only)</summary>
        [HttpPut("{id:int}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult Update(int id, [FromBody] CategoryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = "Dữ liệu không hợp lệ", Errors = ModelState });

            var existing = _categoryRepository.GetById(id);
            if (existing == null)
                return NotFound(new { Message = $"Không tìm thấy danh mục ID = {id}" });

            bool duplicate = _context.Categories.Any(c =>
                c.Name.ToLower() == dto.Name.ToLower().Trim() && c.Id != id);
            if (duplicate)
                return Conflict(new { Message = $"Tên danh mục '{dto.Name}' đã được sử dụng!" });

            existing.Name = dto.Name.Trim();
            _categoryRepository.Update(existing);

            return Ok(new
            {
                existing.Id,
                existing.Name,
                Message = "Cập nhật danh mục thành công!"
            });
        }

        // ──────────────── DELETE ────────────────

        /// <summary>DELETE /api/categories/{id} - Xóa danh mục (Admin only)</summary>
        [HttpDelete("{id:int}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult Delete(int id)
        {
            var existing = _categoryRepository.GetById(id);
            if (existing == null)
                return NotFound(new { Message = $"Không tìm thấy danh mục ID = {id}" });

            bool hasProducts = _context.Products.Any(p => p.CategoryId == id);
            if (hasProducts)
                return Conflict(new
                {
                    Message = "Không thể xóa danh mục này vì vẫn còn sản phẩm liên kết. Hãy chuyển sản phẩm sang danh mục khác trước."
                });

            _categoryRepository.Delete(id);

            return Ok(new { Message = $"Đã xóa danh mục ID={id} thành công!" });
        }
    }
}
