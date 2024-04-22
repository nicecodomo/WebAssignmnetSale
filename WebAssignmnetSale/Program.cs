var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

////Session Integration
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // ตั้งเวลาหมดอายุของ Session
    options.Cookie.HttpOnly = true; // ทำให้ Cookie สามารถเข้าถึงได้แค่ผ่าน HTTP เท่านั้น
    options.Cookie.IsEssential = true; // กำหนดให้ Cookie เป็นส่วนสำคัญของการใช้งานแอปพลิเคชัน
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // เรียกใช้ Session Middleware

app.UseAuthorization();



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Login}/{id?}");

app.Run();
