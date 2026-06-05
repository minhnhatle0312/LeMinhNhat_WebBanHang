using Microsoft.AspNetCore.Mvc;
using LeMinhNhat_WebBanHang.Repositories;
using LeMinhNhat_WebBanHang.Models;
using System.Linq;

namespace LeMinhNhat_WebBanHang.Controllers
{
    public class CompareController : Controller
    {
        private readonly IProductRepository _productRepository;

        public CompareController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public IActionResult Index(int? p1, int? p2)
        {
            var allProducts = _productRepository.GetAll().ToList();
            ViewBag.AllProducts = allProducts;

            Product? product1 = null;
            Product? product2 = null;

            if (p1.HasValue)
            {
                product1 = allProducts.FirstOrDefault(p => p.Id == p1.Value);
            }
            if (p2.HasValue)
            {
                product2 = allProducts.FirstOrDefault(p => p.Id == p2.Value);
            }

            return View(new CompareTuple { Product1 = product1, Product2 = product2 });
        }
    }

    public class CompareTuple
    {
        public Product? Product1 { get; set; }
        public Product? Product2 { get; set; }
    }
}
