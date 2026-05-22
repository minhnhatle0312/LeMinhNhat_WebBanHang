using LeMinhNhat_WebBanHang.Models;
using LeMinhNhat_WebBanHang.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace LeMinhNhat_WebBanHang.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryController(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        // Hiển thị danh sách tất cả Danh mục
        public IActionResult Index()
        {
            var categories = _categoryRepository.GetAllCategories();
            return View(categories);
        }

        // GET: Hiển thị form thêm mới
        public IActionResult Add()
        {
            return View();
        }

        // POST: Tiếp nhận dữ liệu thêm mới từ form gửi lên
        [HttpPost]
        public IActionResult Add(Category category)
        {
            if (ModelState.IsValid)
            {
                _categoryRepository.Add(category);
                return RedirectToAction("Index");
            }
            return View(category);
        }

        // GET: Hiển thị form chỉnh sửa danh mục theo Id
        public IActionResult Update(int id)
        {
            var category = _categoryRepository.GetById(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: Tiếp nhận dữ liệu cập nhật danh mục
        [HttpPost]
        public IActionResult Update(Category category)
        {
            if (ModelState.IsValid)
            {
                _categoryRepository.Update(category);
                return RedirectToAction("Index");
            }
            return View(category);
        }

        // GET: Hiển thị trang xác nhận xóa danh mục
        public IActionResult Delete(int id)
        {
            var category = _categoryRepository.GetById(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: Thực hiện xóa danh mục sau khi người dùng bấm xác nhận
        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            _categoryRepository.Delete(id);
            return RedirectToAction("Index");
        }
    }
}