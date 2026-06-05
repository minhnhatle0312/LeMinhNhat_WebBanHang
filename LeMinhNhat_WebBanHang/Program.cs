using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LeMinhNhat_WebBanHang.DataAccess;
using LeMinhNhat_WebBanHang.Models;
using LeMinhNhat_WebBanHang.Repositories;

var builder = WebApplication.CreateBuilder(args);

// 1. Tích hợp Controllers & Razor Pages cho Identity UI hoạt động độc lập
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
// Enable Session for shopping cart and transient UI state
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 2. Kết nối Database Laragon MySQL / SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Đăng ký Identity sử dụng cấu trúc thực thể ApplicationUser mới mở rộng
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Cấu hình đường dẫn điều phối khi người dùng chưa đăng nhập hoặc bị từ chối truy cập
builder.Services.ConfigureApplicationCookie(options => {
    options.LoginPath = $"/Identity/Account/Login";
    options.LogoutPath = $"/Identity/Account/Logout";
    options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
});

// Đăng ký kiến trúc Repository hiện tại của bạn (Sử dụng Entity Framework)
builder.Services.AddScoped<IProductRepository, EFProductRepository>();
builder.Services.AddScoped<ICategoryRepository, EFCategoryRepository>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Thứ tự bắt buộc: Xác thực danh tính trước -> Phân bổ quyền hạn sau
app.UseAuthentication();
app.UseAuthorization();

// Enable session middleware
app.UseSession();

// Seed roles and default admin user at startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        // Run seeder (synchronous wait is acceptable at startup)
        LeMinhNhat_WebBanHang.Services.RoleSeeder.InitializeAsync(roleManager, userManager).GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        var logger = services.GetService<ILogger<Program>>();
        logger?.LogError(ex, "Error while seeding roles and admin user.");
    }
}

// 4. Định tuyến ưu tiên Area thiết lập trước, Default Route thiết lập sau
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();