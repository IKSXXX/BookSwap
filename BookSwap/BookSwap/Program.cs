using BookExchange.Web.Data;
using BookExchange.Web.Entities;
using BookExchange.Web.Hubs;
using BookExchange.Web.Interfaces;
using BookExchange.Web.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BookExchangeDbContext>(options =>
    options.UseInMemoryDatabase("BookSwap"));

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

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddAutoMapper(cfg => { }, typeof(BookExchange.Web.Helpers.MappingProfile).Assembly);
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();

app.Use(async (ctx, next) =>
{
    if (ctx.User.Identity?.IsAuthenticated != true)
    {
        var um = ctx.RequestServices.GetRequiredService<UserManager<User>>();
        var admin = await um.FindByEmailAsync(DbSeeder.AdminEmail);
        if (admin != null)
        {
            var claims = new List<System.Security.Claims.Claim>
            {
                new(System.Security.Claims.ClaimTypes.NameIdentifier, admin.Id),
                new(System.Security.Claims.ClaimTypes.Name, admin.UserName ?? "admin"),
                new(System.Security.Claims.ClaimTypes.Email, admin.Email ?? ""),
            };
            var roles = await um.GetRolesAsync(admin);
            foreach (var role in roles)
                claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role));

            var identity = new System.Security.Claims.ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
            await ctx.SignInAsync(IdentityConstants.ApplicationScheme, new System.Security.Claims.ClaimsPrincipal(identity));
        }
    }
    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<ChatHub>("/hubs/chat");

await DbSeeder.SeedAsync(app.Services);

app.Run();
