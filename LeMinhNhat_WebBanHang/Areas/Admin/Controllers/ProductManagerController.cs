using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using LeMinhNhat_WebBanHang.Models;
using LeMinhNhat_WebBanHang.Repositories;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LeMinhNhat_WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductManagerController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public ProductManagerController(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        public IActionResult Index()
        {
            var products = _productRepository.GetAll();
            return View(products);
        }

        [HttpGet]
        public IActionResult Add()
        {
            var categories = _categoryRepository.GetAllCategories();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Add(Product product, IFormFile? imageUrl)
        {
            if (ModelState.IsValid)
            {
                if (imageUrl != null && imageUrl.Length > 0)
                {
                    product.ImageUrl = await SaveImage(imageUrl);
                }
                _productRepository.Add(product);
                return RedirectToAction(nameof(Index));
            }
            var categories = _categoryRepository.GetAllCategories();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(product);
        }

        [HttpGet]
        public IActionResult Update(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null) return NotFound();

            var categories = _categoryRepository.GetAllCategories();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Update(Product product, IFormFile? imageUrl)
        {
            if (ModelState.IsValid)
            {
                var existing = _productRepository.GetById(product.Id);
                if (existing == null) return NotFound();

                if (imageUrl != null && imageUrl.Length > 0)
                {
                    product.ImageUrl = await SaveImage(imageUrl);
                }
                else
                {
                    product.ImageUrl = existing.ImageUrl;
                }

                product.ImageUrls = existing.ImageUrls; // Preserve extra images if any

                _productRepository.Update(product);
                return RedirectToAction(nameof(Index));
            }
            var categories = _categoryRepository.GetAllCategories();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            _productRepository.Delete(id);
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveImage(IFormFile image)
        {
            var extension = Path.GetExtension(image.FileName).ToLower();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidDataException("Định dạng file không hợp lệ.");
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
            var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", uniqueFileName);

            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            return "/images/" + uniqueFileName;
        }
    }
}
