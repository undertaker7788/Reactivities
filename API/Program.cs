using Application.Activities.Queries;
using Application.Core;
using Microsoft.EntityFrameworkCore;
using Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddCors();
builder.Services.AddMediatR(x => 
    x.RegisterServicesFromAssemblyContaining<GetActivityList.Handler>());
builder.Services.AddAutoMapper(cfg => 
    {
        cfg.LicenseKey = "eyJhbGciOiJSUzI1NiIsImtpZCI6Ikx1Y2t5UGVubnlTb2Z0d2FyZUxpY2Vuc2VLZXkvYmJiMTNhY2I1OTkwNGQ4OWI0Y2IxYzg1ZjA4OGNjZjkiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL2x1Y2t5cGVubnlzb2Z0d2FyZS5jb20iLCJhdWQiOiJMdWNreVBlbm55U29mdHdhcmUiLCJleHAiOiIxODA2Mjc4NDAwIiwiaWF0IjoiMTc3NDgxMTU0MiIsImFjY291bnRfaWQiOiIwMTlkM2IwMTYzYTY3MjA5OGY5NTRiOGU4MTVkMzk0NiIsImN1c3RvbWVyX2lkIjoiY3RtXzAxa214ZzV3bXBmenN6djIwc25ybjdzNTlzIiwic3ViX2lkIjoiLSIsImVkaXRpb24iOiIwIiwidHlwZSI6IjIifQ.olYYB7cAnAzeAqGHivzjd8T40lw1j2TF3gArbYLYgB4iiEeIZUh_ifZ3zEbFr7S0h-e812JAjEBf9-uuYXaKf3xsMFqLXkkGKHCSzAIhK7n4uGk4umI43aaTEVqQK2nv3XQcbOpQsqOajjFsPmcCYhfvSj30X-2FnHZ4O99gq27S38qtM-xVcvv6pcPUhEFO-L3BvwAJN-Bj_UD5JGMJIhyco9RQOLfH7FU1QRGayAwvrnARfG7VZ4YxeZMoCMLbHOoMpYmJgJogf3vBMsNIMlrHzpK9gNgsMVR8i_PzVh8lAzwwZgn0LKeBENNHha1rKRxa3IzWwEcp_3Z3lzAsAQ";
        // cfg.AddProfile<MappingProfiles>();
        // cfg.AddMaps(typeof(MappingProfiles).Assembly);
    }, typeof(MappingProfiles).Assembly);
// builder.Services.AddAutoMapper(cfg => {}, typeof(MappingProfiles).Assembly);
// builder.Services.AddAutoMapper(typeof(MappingProfiles).Assembly);


var app = builder.Build();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
// }

// 直接使用 https 的，所以這裡不去管 redirection
// app.UseHttpsRedirection();

// 之後才會用到，我先註解掉
// app.UseAuthorization();
app.UseCors(options => options.AllowAnyHeader().AllowAnyMethod()
    .WithOrigins("http://localhost:3000", "https://localhost:3000", "https://localhost:3001"));

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
