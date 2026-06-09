using BookSwap.Db.Data;
using BookSwap.Db.Entities;
using BookSwap.Db.Interfaces;
using BookSwap.Db.Repositories;
using BookSwap.Web.Data;
using BookSwap.Web.Helpers;
using BookSwap.Web.Hubs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Запуск приложения BookSwap");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    var useMockData = builder.Configuration.GetValue<bool>("UseMockData");

    if (useMockData)
        builder.Services.AddDbContext<BookExchangeDbContext>(options => options.UseInMemoryDatabase("BookSwap"));
    else
    {
        var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");
        builder.Services.AddDbContext<BookExchangeDbContext>(options => options.UseNpgsql(connStr));
    }

    builder.Services.AddIdentity<User, IdentityRole>(options =>
    {
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
        options.Password.RequireUppercase = true;
        options.Password.RequireDigit = true;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<BookExchangeDbContext>()
    .AddDefaultTokenProviders();

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
    });

    if (useMockData)
        builder.Services.AddScoped<IUnitOfWork, BookSwap.Web.Mocks.MockUnitOfWork>();
    else
    {
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
        builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    }
    builder.Services.AddTransient<IEmailSender, BookSwap.Web.Helpers.ConsoleEmailSender>();
    builder.Services.AddSingleton<BookSwap.Web.Services.GigaChatService>();
    builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
    builder.Services.AddControllersWithViews();
    builder.Services.AddSignalR();

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("ai", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                httpContext.User.Identity?.Name
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1)
                }));
    });

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();

    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
    app.MapHub<ChatHub>("/hubs/chat");
    app.MapHub<DiscussionHub>("/hubs/discussion");

    await DbSeeder.SeedAsync(app.Services);

    if (useMockData)
    {
        using var scope = app.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BookExchangeDbContext>();
        await BookSwap.Web.Mocks.MockDataStore.LoadFromDbContextAsync(ctx);
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Приложение завершилось с ошибкой");
}
finally
{
    Log.CloseAndFlush();
}
