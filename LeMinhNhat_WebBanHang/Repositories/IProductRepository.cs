using LeMinhNhat_WebBanHang.Models;
using System.Collections.Generic;

namespace LeMinhNhat_WebBanHang.Repositories
{
    public interface IProductRepository
    {
        IEnumerable<Product> GetAll();
        Product GetById(int id);
        void Add(Product product);
        void Update(Product product);
        void Delete(int id);
    }
}