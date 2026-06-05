using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeMinhNhat_WebBanHang.Models;
using LeMinhNhat_WebBanHang.Repositories;

namespace LeMinhNhat_WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoryManagerController : Controller
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryManagerController(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public IActionResult Index()
        {
            var categories = _categoryRepository.GetAllCategories();
            return View(categories);
        }

        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Add(Category category)
        {
            if (ModelState.IsValid)
            {
                _categoryRepository.Add(category);
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        [HttpGet]
        public IActionResult Update(int id)
        {
            var category = _categoryRepository.GetById(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        public IActionResult Update(Category category)
        {
            if (ModelState.IsValid)
            {
                _categoryRepository.Update(category);
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var category = _categoryRepository.GetById(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            _categoryRepository.Delete(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
