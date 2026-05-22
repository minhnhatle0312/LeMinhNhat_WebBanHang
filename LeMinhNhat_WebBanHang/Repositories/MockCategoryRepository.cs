using LeMinhNhat_WebBanHang.Models;
using System.Collections.Generic;
using System.Linq;

namespace LeMinhNhat_WebBanHang.Repositories
{
    public class MockCategoryRepository : ICategoryRepository
    {
        private readonly List<Category> _categoryList;

        public MockCategoryRepository()
        {
            _categoryList = new List<Category>
            {
                new Category { Id = 1, Name = "Laptop" },
                new Category { Id = 2, Name = "Desktop" }
            };
        }

        public IEnumerable<Category> GetAllCategories()
        {
            return _categoryList;
        }

        public Category GetById(int id)
        {
            return _categoryList.FirstOrDefault(c => c.Id == id);
        }

        public void Add(Category category)
        {
            category.Id = _categoryList.Any() ? _categoryList.Max(c => c.Id) + 1 : 1;
            _categoryList.Add(category);
        }

        public void Update(Category category)
        {
            var existing = GetById(category.Id);
            if (existing != null)
            {
                existing.Name = category.Name;
            }
        }

        public void Delete(int id)
        {
            var category = GetById(id);
            if (category != null)
            {
                _categoryList.Remove(category);
            }
        }
    }
}