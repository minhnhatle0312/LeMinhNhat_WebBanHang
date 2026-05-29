using LeMinhNhat_WebBanHang.Repositories;
using LeMinhNhat_WebBanHang.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 1. Cấu hình DbContext kết nối SQL (Đảm bảo đã khai báo ConnectionStrings ở appsettings.json)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. THAY ĐỔI TẠI ĐÂY: Chuyển đổi từ Mock sang dùng Entity Framework Repository
// Hãy comment hoặc xóa bỏ 2 dòng Mock cũ:
// builder.Services.AddSingleton<IProductRepository, MockProductRepository>();
// builder.Services.AddScoped<ICategoryRepository, MockCategoryRepository>();

// Và thay thế bằng 2 dòng gọi cơ sở dữ liệu thật dưới đây:
builder.Services.AddScoped<IProductRepository, EFProductRepository>();
builder.Services.AddScoped<ICategoryRepository, EFCategoryRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Product}/{action=Index}/{id?}");

app.Run();