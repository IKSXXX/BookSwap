using BookSwap.Db.Data;
using BookSwap.Db.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookSwap.Web.Data;

public static class DbSeeder
{
    public const string AdminRole = "Admin";
    public const string UserRole = "User";
    public const string AdminEmail = "admin@bookswap.com";
    public const string AdminPassword = "Admin123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BookSwapDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        if (ctx.Database.IsRelational())
        {
            await ctx.Database.MigrateAsync();
            // Ensure Notifications table exists (may not be covered by existing migrations)
            await ctx.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""Notifications"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""UserId"" TEXT NOT NULL,
                    ""Type"" TEXT NOT NULL DEFAULT 'exchange',
                    ""Text"" TEXT NOT NULL,
                    ""RelatedUrl"" TEXT,
                    ""IsRead"" BOOLEAN NOT NULL DEFAULT FALSE,
                    ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMPTZ,
                    CONSTRAINT ""FK_Notifications_AspNetUsers_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ""IX_Notifications_UserId_IsRead"" ON ""Notifications"" (""UserId"", ""IsRead"");
            ");
        }

        foreach (var role in new[] { AdminRole, UserRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var admin = await userManager.FindByEmailAsync(AdminEmail);
        if (admin == null)
        {
            admin = new User { UserName = "admin", Email = AdminEmail, EmailConfirmed = true, Location = "Система", AvatarPath = "https://api.dicebear.com/9.x/thumbs/svg?seed=admin&backgroundColor=transparent" };
            await userManager.CreateAsync(admin, AdminPassword);
        }
        if (!await userManager.IsInRoleAsync(admin, AdminRole)) await userManager.AddToRoleAsync(admin, AdminRole);
        if (!await userManager.IsInRoleAsync(admin, UserRole)) await userManager.AddToRoleAsync(admin, UserRole);

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
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new User { UserName = name, Email = email, EmailConfirmed = true, Location = city, AvatarPath = $"https://api.dicebear.com/9.x/thumbs/svg?seed={name}&backgroundColor=transparent", RegistrationDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(10, 200)) };
                await userManager.CreateAsync(user, "Pass123!");
                await userManager.AddToRoleAsync(user, UserRole);
            }
            userMap[name] = user;
        }

        if (!await ctx.Books.AnyAsync())
        {
            var books = new List<Book>
            {
                new() { Title = "Преступление и наказание", Author = "Ф. Достоевский", Genre = "Классика", Condition = BookCondition.Good, Year = 1866, Description = "Роман о студенте Раскольникове, решившемся на убийство ради идеи.", CoverImagePath = "/images/books/crime.jpg", IsAvailable = true },
                new() { Title = "1984", Author = "Дж. Оруэлл", Genre = "Антиутопия", Condition = BookCondition.Excellent, Year = 1949, Description = "Антиутопия о тоталитарном обществе под контролем Большого Брата.", CoverImagePath = "/images/books/1984.jpg", IsAvailable = true },
                new() { Title = "Мастер и Маргарита", Author = "М. Булгаков", Genre = "Классика", Condition = BookCondition.Acceptable, Year = 1967, Description = "Мистический роман о визите дьявола в Москву.", CoverImagePath = "/images/books/master.jpg", IsAvailable = true },
                new() { Title = "Война и мир", Author = "Л. Толстой", Genre = "Классика", Condition = BookCondition.Excellent, Year = 1869, Description = "Эпопея о судьбах людей на фоне наполеоновских войн.", CoverImagePath = "/images/books/war.jpg", IsAvailable = true },
                new() { Title = "Гарри Поттер и философский камень", Author = "Дж. Роулинг", Genre = "Фэнтези", Condition = BookCondition.Good, Year = 1997, Description = "Первая книга о мальчике-волшебнике, узнавшем о своей магии.", CoverImagePath = "/images/books/harry.jpg", IsAvailable = true },
                new() { Title = "Игра престолов", Author = "Дж. Мартин", Genre = "Фэнтези", Condition = BookCondition.Good, Year = 1996, Description = "Эпическая сага о борьбе за Железный трон Семи Королевств.", CoverImagePath = "/images/books/thrones.jpg", IsAvailable = true },
                new() { Title = "Евгений Онегин", Author = "А. Пушкин", Genre = "Классика", Condition = BookCondition.Good, Year = 1833, Description = "Роман в стихах о светском денди и деревенской девушке.", CoverImagePath = "/images/books/onegin.jpg", IsAvailable = true },
                new() { Title = "Шерлок Холмс", Author = "А.К. Дойл", Genre = "Детектив", Condition = BookCondition.Excellent, Year = 1892, Description = "Сборник рассказов о гениальном детективе и его верном друге.", CoverImagePath = "/images/books/holmes.jpg", IsAvailable = true },
                new() { Title = "Великий Гэтсби", Author = "Ф.С. Фицджеральд", Genre = "Классика", Condition = BookCondition.Excellent, Year = 1925, Description = "История загадочного миллионера и его неразделённой любви.", CoverImagePath = "/images/books/gatsby.jpg", IsAvailable = true },
                new() { Title = "Сто лет одиночества", Author = "Г. Маркес", Genre = "Магический реализм", Condition = BookCondition.Good, Year = 1967, Description = "Хроника семьи Буэндиа в вымышленном городе Макондо.", CoverImagePath = "/images/books/solitude.jpg", IsAvailable = true },
                new() { Title = "Три товарища", Author = "Э.М. Ремарк", Genre = "Классика", Condition = BookCondition.Good, Year = 1936, Description = "История дружбы и любви в послевоенной Германии.", CoverImagePath = "/images/books/comrades.jpg", IsAvailable = true },
                new() { Title = "Отцы и дети", Author = "И. Тургенев", Genre = "Классика", Condition = BookCondition.Good, Year = 1862, Description = "Роман о конфликте поколений и идеологий в России XIX века.", CoverImagePath = "/images/books/fathers.jpg", IsAvailable = true },
                new() { Title = "Анна Каренина", Author = "Л. Толстой", Genre = "Классика", Condition = BookCondition.Good, Year = 1878, Description = "Трагическая история любви замужней дворянки.", CoverImagePath = "/images/books/anna.jpg", IsAvailable = true },
                new() { Title = "Процесс", Author = "Ф. Кафка", Genre = "Классика", Condition = BookCondition.Acceptable, Year = 1925, Description = "Философский роман о человеке, обвинённом по неизвестной причине.", CoverImagePath = "/images/books/trial.jpg", IsAvailable = true },
                new() { Title = "Заводной апельсин", Author = "Э. Бёрджесс", Genre = "Антиутопия", Condition = BookCondition.Good, Year = 1962, Description = "Провокационный роман о насилии и свободе воли.", CoverImagePath = "/images/books/orange.jpg", IsAvailable = true },
                new() { Title = "О дивный новый мир", Author = "О. Хаксли", Genre = "Антиутопия", Condition = BookCondition.Good, Year = 1932, Description = "Мир, где людей создают в пробирках и контролируют сомой.", CoverImagePath = "/images/books/brave.jpg", IsAvailable = true },
                new() { Title = "Убить пересмешника", Author = "Х. Ли", Genre = "Классика", Condition = BookCondition.Good, Year = 1960, Description = "История расовой несправедливости глазами ребёнка в американском Юге.", CoverImagePath = "/images/books/mockingbird.jpg", IsAvailable = true },
                new() { Title = "Над пропастью во ржи", Author = "Дж. Сэлинджер", Genre = "Классика", Condition = BookCondition.Excellent, Year = 1951, Description = "Исповедь бунтующего подростка в большом городе.", CoverImagePath = "/images/books/rye.jpg", IsAvailable = true },
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
                new BookOwner { BookId = books[3].Id, UserId = userMap["pavel"].Id, IsPrimary = false },
                new BookOwner { BookId = books[4].Id, UserId = userMap["dmitry"].Id, IsPrimary = true },
                new BookOwner { BookId = books[5].Id, UserId = userMap["pavel"].Id, IsPrimary = true },
                new BookOwner { BookId = books[5].Id, UserId = userMap["sergey"].Id, IsPrimary = false },
                new BookOwner { BookId = books[6].Id, UserId = userMap["maria"].Id, IsPrimary = true },
                new BookOwner { BookId = books[7].Id, UserId = userMap["dmitry"].Id, IsPrimary = true },
                new BookOwner { BookId = books[8].Id, UserId = userMap["maria"].Id, IsPrimary = true },
                new BookOwner { BookId = books[9].Id, UserId = userMap["olga"].Id, IsPrimary = true },
                new BookOwner { BookId = books[10].Id, UserId = userMap["sergey"].Id, IsPrimary = true },
                new BookOwner { BookId = books[11].Id, UserId = userMap["elena"].Id, IsPrimary = true },
                new BookOwner { BookId = books[12].Id, UserId = userMap["anna"].Id, IsPrimary = true },
                new BookOwner { BookId = books[13].Id, UserId = userMap["olga"].Id, IsPrimary = true },
                new BookOwner { BookId = books[14].Id, UserId = userMap["pavel"].Id, IsPrimary = true },
                new BookOwner { BookId = books[15].Id, UserId = userMap["sergey"].Id, IsPrimary = true },
                new BookOwner { BookId = books[16].Id, UserId = userMap["olga"].Id, IsPrimary = true },
                new BookOwner { BookId = books[17].Id, UserId = userMap["anna"].Id, IsPrimary = true }
            );

            ctx.ExchangeRequests.AddRange(
                new ExchangeRequest { BookRequestedId = books[1].Id, SenderId = userMap["anna"].Id, ReceiverId = userMap["igor"].Id, Status = ExchangeStatus.Completed },
                new ExchangeRequest { BookRequestedId = books[0].Id, SenderId = userMap["igor"].Id, ReceiverId = userMap["anna"].Id, Status = ExchangeStatus.Completed },
                new ExchangeRequest { BookRequestedId = books[4].Id, SenderId = userMap["elena"].Id, ReceiverId = userMap["dmitry"].Id, Status = ExchangeStatus.Completed }
            );
            await ctx.SaveChangesAsync();

            ctx.Reviews.AddRange(
                new Review { FromUserId = userMap["anna"].Id, ToUserId = userMap["igor"].Id, Rating = 5, Comment = "Отличный обмен, книга в идеальном состоянии!", ExchangeRequestId = ctx.ExchangeRequests.First().Id },
                new Review { FromUserId = userMap["igor"].Id, ToUserId = userMap["anna"].Id, Rating = 4, Comment = "Спасибо! Быстро и удобно.", ExchangeRequestId = ctx.ExchangeRequests.Skip(1).First().Id },
                new Review { FromUserId = userMap["elena"].Id, ToUserId = userMap["dmitry"].Id, Rating = 5, Comment = "Рекомендую, всё прошло гладко.", ExchangeRequestId = ctx.ExchangeRequests.Skip(2).First().Id }
            );
            await ctx.SaveChangesAsync();

            foreach (var user in userMap.Values)
            {
                var ratings = await ctx.Reviews.Where(r => r.ToUserId == user.Id).Select(r => r.Rating).ToListAsync();
                user.Rating = ratings.Count > 0 ? Math.Round(ratings.Average() ?? 0, 2) : 0;
            }
            await ctx.SaveChangesAsync();

            ctx.BooksOfTheDay.Add(new BookOfTheDay { BookId = books[1].Id, Date = DateTime.UtcNow.Date });

            ctx.QuizQuestions.AddRange(
                new QuizQuestion { BookId = books[0].Id, Quote = "Тварь я дрожащая или право имею?", CorrectAnswer = "Преступление и наказание", Option2 = "Война и мир", Option3 = "Мастер и Маргарита", Option4 = "1984" },
                new QuizQuestion { BookId = books[1].Id, Quote = "Большой Брат следит за тобой.", CorrectAnswer = "1984", Option2 = "Заводной апельсин", Option3 = "Процесс", Option4 = "Игра престолов" },
                new QuizQuestion { BookId = books[2].Id, Quote = "Рукописи не горят.", CorrectAnswer = "Мастер и Маргарита", Option2 = "Преступление и наказание", Option3 = "Анна Каренина", Option4 = "Три товарища" },
                new QuizQuestion { BookId = books[4].Id, Quote = "Да, я волшебник.", CorrectAnswer = "Гарри Поттер и философский камень", Option2 = "Игра престолов", Option3 = "1984", Option4 = "Сто лет одиночества" },
                new QuizQuestion { BookId = books[6].Id, Quote = "Когда играешь в игру престолов — побеждаешь или умираешь.", CorrectAnswer = "Игра престолов", Option2 = "Гарри Поттер и философский камень", Option3 = "Заводной апельсин", Option4 = "Процесс" }
            );

            var discussion = new Discussion { BookId = books[1].Id, UserId = userMap["anna"].Id, Title = "Актуальна ли антиутопия сегодня?" };
            ctx.Discussions.Add(discussion);
            await ctx.SaveChangesAsync();

            ctx.DiscussionMessages.AddRange(
                new DiscussionMessage { DiscussionId = discussion.Id, UserId = userMap["anna"].Id, Text = "По-моему, книга как никогда актуальна. Что думаете?" },
                new DiscussionMessage { DiscussionId = discussion.Id, UserId = userMap["igor"].Id, Text = "Согласен! Особенно про новояз и манипуляции языком." }
            );

            await ctx.SaveChangesAsync();
        }
    }
}
