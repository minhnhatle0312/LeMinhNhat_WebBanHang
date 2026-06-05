using LeMinhNhat_WebBanHang.Models;
using LeMinhNhat_WebBanHang.DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace LeMinhNhat_WebBanHang.Repositories
{
    public class EFProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        // Tiêm DbContext thông qua Constructor
        public EFProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Product> GetAll()
        {
            // Trả về danh sách sản phẩm
            return _context.Products.ToList();
        }

        public Product GetById(int id)
        {
            return _context.Products.FirstOrDefault(p => p.Id == id);
        }

        public void Add(Product product)
        {
            _context.Products.Add(product);
            _context.SaveChanges(); // Lệnh đồng bộ lưu trực tiếp vào database
        }

        public void Update(Product product)
        {
            // Tìm xem trong Tracker của DbContext hiện tại có thực thể nào cùng Id đang được theo dõi không
            var local = _context.Products
                .Local
                .FirstOrDefault(entry => entry.Id == product.Id);

            // Nếu tìm thấy thực thể đang bị giữ trong bộ nhớ local, ta lập tức bỏ theo dõi nó (Detach)
            if (local != null)
            {
                _context.Entry(local).State = EntityState.Detached;
            }

            // Đánh dấu thực thể mới nhận từ View này là Modified để chuẩn bị cập nhật dữ liệu
            _context.Entry(product).State = EntityState.Modified;

            // Lưu thay đổi xuống cơ sở dữ liệu
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var product = GetById(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                _context.SaveChanges(); // Thực thi xóa trong database
            }
        }
    }
}