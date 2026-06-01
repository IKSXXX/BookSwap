using BookExchange.Db.Data;
using BookExchange.Db.Entities;
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

        if (ctx.Database.IsRelational())
            await ctx.Database.MigrateAsync();

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
                Location = "Система",
                AvatarPath = "https://api.dicebear.com/9.x/thumbs/svg?seed=admin&backgroundColor=transparent"
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
            ("dmitry", "dmitry@bookswap.com", "Новосибирск"),
            ("olga", "olga@bookswap.com", "Екатеринбург"),
            ("pavel", "pavel@bookswap.com", "Нижний Новгород"),
            ("maria", "maria@bookswap.com", "Самара"),
            ("sergey", "sergey@bookswap.com", "Ростов-на-Дону"),
        };

        var userMap = new Dictionary<string, User>();
        foreach (var (name, email, city) in demoUsers)
        {
            var u = await userManager.FindByEmailAsync(email);
            if (u == null)
            {
                u = new User
                {
                    UserName = name,
                    Email = email,
                    EmailConfirmed = true,
                    Location = city,
                    AvatarPath = $"https://api.dicebear.com/9.x/thumbs/svg?seed={name}&backgroundColor=transparent",
                    RegistrationDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(10, 200))
                };
                await userManager.CreateAsync(u, "Pass123!");
                await userManager.AddToRoleAsync(u, UserRole);
            }
            userMap[name] = u;
        }

        if (!await ctx.Books.AnyAsync())
        {
            var books = new List<Book>
            {
                new() { Title = "Преступление и наказание", Author = "Ф. Достоевский", Genre = "Классика", Condition = BookCondition.Good, Year = 1866, Description = "Великий роман о преступлении и наказании.", CoverImagePath = "/images/books/crime.jpg", IsAvailable = true },
                new() { Title = "1984", Author = "Дж. Оруэлл", Genre = "Антиутопия", Condition = BookCondition.Excellent, Year = 1949, Description = "Классическая антиутопия о тоталитарном обществе.", CoverImagePath = "/images/books/1984.jpg", IsAvailable = true },
                new() { Title = "Мастер и Маргарита", Author = "М. Булгаков", Genre = "Классика", Condition = BookCondition.Acceptable, Year = 1967, Description = "Мистический роман о любви, дьяволе и Москве 30-х.", CoverImagePath = "/images/books/master.jpg", IsAvailable = false },
                new() { Title = "Война и мир", Author = "Л. Толстой", Genre = "Классика", Condition = BookCondition.Excellent, Year = 1869, Description = "Эпопея о судьбах людей на фоне наполеоновских войн.", CoverImagePath = "/images/books/war.jpg", IsAvailable = true },
                new() { Title = "Гарри Поттер и философский камень", Author = "Дж. Роулинг", Genre = "Фэнтези", Condition = BookCondition.Good, Year = 1997, Description = "Первая книга о мальчике-волшебнике.", CoverImagePath = "/images/books/harry.jpg", IsAvailable = true },
                new() { Title = "Игра престолов", Author = "Дж. Мартин", Genre = "Фэнтези", Condition = BookCondition.Good, Year = 1996, Description = "Эпическая сага о борьбе за Железный трон.", CoverImagePath = "/images/books/thrones.jpg", IsAvailable = true },
                new() { Title = "Евгений Онегин", Author = "А. Пушкин", Genre = "Классика", Condition = BookCondition.Good, Year = 1833, Description = "Роман в стихах, энциклопедия русской жизни.", CoverImagePath = "/images/books/onegin.jpg", IsAvailable = true },
                new() { Title = "Шерлок Холмс", Author = "А.К. Дойл", Genre = "Детектив", Condition = BookCondition.Excellent, Year = 1892, Description = "Сборник рассказов о гениальном сыщике.", CoverImagePath = "/images/books/holmes.jpg", IsAvailable = true },
                new() { Title = "Великий Гэтсби", Author = "Ф.С. Фицджеральд", Genre = "Классика", Condition = BookCondition.Excellent, Year = 1925, Description = "История любви и разочарования в эпоху джаза.", CoverImagePath = "/images/books/gatsby.jpg", IsAvailable = true },
            };
            await ctx.Books.AddRangeAsync(books);
            await ctx.SaveChangesAsync();

            ctx.BookOwners.AddRange(
                new BookOwner { BookId = books[0].Id, UserId = userMap["anna"].Id, IsPrimary = true },
                new BookOwner { BookId = books[0].Id, UserId = userMap["igor"].Id, IsPrimary = false },
                new BookOwner { BookId = books[1].Id, UserId = userMap["igor"].Id, IsPrimary = true },
                new BookOwner { BookId = books[2].Id, UserId = userMap["elena"].Id, IsPrimary = true },
                new BookOwner { BookId = books[2].Id, UserId = userMap["dmitry"].Id, IsPrimary = false },
                new BookOwner { BookId = books[3].Id, UserId = userMap["anna"].Id, IsPrimary = true },
                new BookOwner { BookId = books[4].Id, UserId = userMap["dmitry"].Id, IsPrimary = true },
                new BookOwner { BookId = books[5].Id, UserId = userMap["pavel"].Id, IsPrimary = true },
                new BookOwner { BookId = books[5].Id, UserId = userMap["sergey"].Id, IsPrimary = false },
                new BookOwner { BookId = books[6].Id, UserId = userMap["maria"].Id, IsPrimary = true },
                new BookOwner { BookId = books[7].Id, UserId = userMap["dmitry"].Id, IsPrimary = true },
                new BookOwner { BookId = books[8].Id, UserId = userMap["maria"].Id, IsPrimary = true }
            );

            ctx.BooksOfTheDay.Add(new BookOfTheDay { BookId = books[1].Id, Date = DateTime.Now.Date });

            ctx.QuizQuestions.AddRange(
                new QuizQuestion { BookId = books[0].Id, Quote = "Тварь я дрожащая или право имею?", CorrectAnswer = "Преступление и наказание", Option2 = "Война и мир", Option3 = "Мастер и Маргарита", Option4 = "1984" },
                new QuizQuestion { BookId = books[1].Id, Quote = "Большой Брат следит за тобой.", CorrectAnswer = "1984", Option2 = "Заводной апельсин", Option3 = "Процесс", Option4 = "Игра престолов" },
                new QuizQuestion { BookId = books[2].Id, Quote = "Рукописи не горят.", CorrectAnswer = "Мастер и Маргарита", Option2 = "Преступление и наказание", Option3 = "Анна Каренина", Option4 = "Три товарища" },
                new QuizQuestion { BookId = books[4].Id, Quote = "Да, я волшебник.", CorrectAnswer = "Гарри Поттер и философский камень", Option2 = "Игра престолов", Option3 = "1984", Option4 = "Сто лет одиночества" },
                new QuizQuestion { BookId = books[6].Id, Quote = "Когда играешь в игру престолов — побеждаешь или умираешь.", CorrectAnswer = "Игра престолов", Option2 = "Гарри Поттер и философский камень", Option3 = "Заводной апельсин", Option4 = "Процесс" }
            );

            var disc = new Discussion { BookId = books[1].Id, UserId = userMap["anna"].Id, Title = "Актуальна ли антиутопия сегодня?" };
            ctx.Discussions.Add(disc);
            await ctx.SaveChangesAsync();

            ctx.DiscussionMessages.AddRange(
                new DiscussionMessage { DiscussionId = disc.Id, UserId = userMap["anna"].Id, Text = "По-моему, книга как никогда актуальна. Что думаете?" },
                new DiscussionMessage { DiscussionId = disc.Id, UserId = userMap["igor"].Id, Text = "Согласен! Особенно про новояз и манипуляции языком." }
            );

            await ctx.SaveChangesAsync();
        }
    }
}
