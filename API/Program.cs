using Microsoft.EntityFrameworkCore;
using Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
// }

// 直接使用 https 的，所以這裡不去管 redirection
// app.UseHttpsRedirection();

// 之後才會用到，我先註解掉
// app.UseAuthorization();

app.MapControllers();

// using 區塊結束後，裡面的東西就會 Dispose
using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;

try
{
    // 用 code 來觸發執行 migration，並嘗試初始化 DB 內的資料
    var context = services.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    await DbInitializer.SeedData(context);
}
catch(Exception ex)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occured during migration.");
}

app.Run();
