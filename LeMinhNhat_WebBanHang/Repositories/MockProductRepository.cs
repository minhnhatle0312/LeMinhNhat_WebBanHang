using LeMinhNhat_WebBanHang.Models;
using LeMinhNhat_WebBanHang.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace LeMinhNhat_WebBanHang.Repositories
{
    public class MockProductRepository : IProductRepository
    {
        // Chuyển sang biến static để dữ liệu được lưu lại trong RAM suốt vòng đời ứng dụng
        private static readonly List<Product> _products = new List<Product>
        {
            new Product {
                Id = 1,
                Name = "Laptop Gaming ASUS",
                Price = 1500,
                Description = "A high-end gaming laptop",
                CategoryId = 1,
                ImageUrl = "/images/sample-laptop.jpg",
                ImageUrls = new List<string>()
            }
        };

        public IEnumerable<Product> GetAll()
        {
            return _products;
        }

        public Product GetById(int id)
        {
            return _products.FirstOrDefault(p => p.Id == id);
        }

        public void Add(Product product)
        {
            // Tự động tăng ID an toàn kể cả khi danh sách trống
            product.Id = _products.Any() ? _products.Max(p => p.Id) + 1 : 1;
            _products.Add(product);
        }

        public void Update(Product product)
        {
            var index = _products.FindIndex(p => p.Id == product.Id);
            if (index != -1)
            {
                _products[index] = product;
            }
        }

        public void Delete(int id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product != null)
            {
                _products.Remove(product);
            }
        }
    }
}