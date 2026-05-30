using BookExchange.Web.Data;
using BookExchange.Web.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookExchange.Web.Data;

public static class DbSeeder
{
    public const string AdminRole = "Admin";
    public const string UserRole = "User";
    public const string AdminEmail = "admin@bookswap.com";
    public const string AdminPassword = "Admin123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BookExchangeDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        foreach (var role in new[] { AdminRole, UserRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var admin = await userManager.FindByEmailAsync(AdminEmail);
        if (admin == null)
        {
            admin = new User
            {
                UserName = "admin",
                Email = AdminEmail,
                EmailConfirmed = true,
                Location = "Система"
            };
            await userManager.CreateAsync(admin, AdminPassword);
        }
        if (!await userManager.IsInRoleAsync(admin, AdminRole))
            await userManager.AddToRoleAsync(admin, AdminRole);
        if (!await userManager.IsInRoleAsync(admin, UserRole))
            await userManager.AddToRoleAsync(admin, UserRole);

        var demoUsers = new (string name, string email, string city)[]
        {
            ("anna", "anna@bookswap.com", "Москва"),
            ("igor", "igor@bookswap.com", "Санкт-Петербург"),
            ("elena", "elena@bookswap.com", "Казань"),
        };

        var userMap = new Dictionary<string, User>();
        foreach (var (name, email, city) in demoUsers)
        {
            var u = await userManager.FindByEmailAsync(email);
            if (u == null)
            {
                u = new User { UserName = name, Email = email, EmailConfirmed = true, Location = city };
                await userManager.CreateAsync(u, "Pass123!");
                await userManager.AddToRoleAsync(u, UserRole);
            }
            userMap[name] = u;
        }

        if (!await ctx.Books.AnyAsync())
        {
            var books = new List<Book>
            {
                new() { Title = "Преступление и наказание", Author = "Ф. Достоевский", Genre = "Классика", Condition = BookCondition.Good, Year = 1866, Description = "Великий роман о преступлении и наказании, о раскаянии и вере.", IsAvailable = true },
                new() { Title = "1984", Author = "Дж. Оруэлл", Genre = "Антиутопия", Condition = BookCondition.Excellent, Year = 1949, Description = "Классическая антиутопия о тоталитарном обществе.", IsAvailable = true },
                new() { Title = "Мастер и Маргарита", Author = "М. Булгаков", Genre = "Классика", Condition = BookCondition.Acceptable, Year = 1967, Description = "Мистический роман о любви, дьяволе и Москве 30-х.", IsAvailable = true },
                new() { Title = "Гарри Поттер и философский камень", Author = "Дж. Роулинг", Genre = "Фэнтези", Condition = BookCondition.Good, Year = 1997, Description = "Первая книга о мальчике-волшебнике.", IsAvailable = true },
                new() { Title = "Игра престолов", Author = "Дж. Мартин", Genre = "Фэнтези", Condition = BookCondition.Good, Year = 1996, Description = "Эпическая сага о борьбе за Железный трон.", IsAvailable = true },
            };
            await ctx.Books.AddRangeAsync(books);
            await ctx.SaveChangesAsync();

            ctx.BookOwners.AddRange(
                new BookOwner { BookId = books[0].Id, UserId = userMap["anna"].Id, IsPrimary = true },
                new BookOwner { BookId = books[1].Id, UserId = userMap["igor"].Id, IsPrimary = true },
                new BookOwner { BookId = books[2].Id, UserId = userMap["elena"].Id, IsPrimary = true },
                new BookOwner { BookId = books[3].Id, UserId = userMap["anna"].Id, IsPrimary = true },
                new BookOwner { BookId = books[4].Id, UserId = userMap["igor"].Id, IsPrimary = true }
            );
            await ctx.SaveChangesAsync();
        }
    }
}
