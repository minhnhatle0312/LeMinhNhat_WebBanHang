using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using LeMinhNhat_WebBanHang.Models;

namespace LeMinhNhat_WebBanHang.DataAccess // Khuyên dùng .DataAccess để đồng bộ kiến trúc dự án
{
    // CẢI TIẾN CHÍNH: Kế thừa IdentityDbContext<ApplicationUser> thay vì DbContext thuần túy
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        // Hàm khởi tạo (Constructor) nhận DbContextOptions để cấu hình chuỗi kết nối từ Program.cs
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Định nghĩa tập hợp thực thể (Bảng) Sản phẩm
        public DbSet<Product> Products { get; set; }

        // Định nghĩa tập hợp thực thể (Bảng) Danh mục ngành hàng
        public DbSet<Category> Categories { get; set; }

        // Định nghĩa các thực thể đơn hàng
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        // Cấu hình Fluent API (Tùy biến bảng, thiết lập ràng buộc và tạo dữ liệu mẫu nếu cần)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // LƯU Ý QUAN TRỌNG: Phải gọi base.OnModelCreating(modelBuilder) đầu tiên
            // để Identity khởi tạo thành công các cấu trúc cấu hình bảng bảo mật mặc định.
            base.OnModelCreating(modelBuilder);

            // 1. Cấu hình bảng Products
            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("Products"); // Đặt tên bảng trong CSDL
                entity.HasKey(p => p.Id);   // Khóa chính

                entity.Property(p => p.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(p => p.Price)
                    .HasColumnType("decimal(18,2)"); // Khớp chuẩn kiểu dữ liệu tiền tệ trong SQL

                // Cấu hình mối quan hệ 1-Nhiều: Một Category có nhiều Products
                entity.HasOne<Category>()
                    .WithMany()
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.Cascade); // Xóa danh mục thì tự động xóa sản phẩm thuộc danh mục đó
            });

            // 2. Cấu hình bảng Categories
            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Categories");
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Name)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            // 3. Khởi tạo dữ liệu mẫu (Seed Data) cho hệ thống khi Migration lần đầu
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Laptop" },
                new Category { Id = 2, Name = "Desktop" }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Name = "Laptop ASUS ROG Strix",
                    Price = 1200.00m,
                    Description = "Dòng laptop gaming hiệu năng cao dành cho game thủ.",
                    CategoryId = 1,
                    ImageUrl = "/images/sample-laptop.png"
                }
            );

            // 4. Cấu hình bảng Orders
            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("Orders");
                entity.HasKey(o => o.Id);
                entity.HasOne(o => o.User)
                    .WithMany()
                    .HasForeignKey(o => o.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 5. Cấu hình bảng OrderDetails
            modelBuilder.Entity<OrderDetail>(entity =>
            {
                entity.ToTable("OrderDetails");
                entity.HasKey(od => od.Id);
                entity.HasOne(od => od.Order)
                    .WithMany(o => o.OrderDetails)
                    .HasForeignKey(od => od.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(od => od.Product)
                    .WithMany()
                    .HasForeignKey(od => od.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}