using BookExchange.Db.Entities;
using BookExchange.Db.Data;
using Microsoft.EntityFrameworkCore;

namespace BookExchange.Web.Mocks;

public static class MockDataStore
{
    public static List<Book> Books { get; } = [];
    public static List<BookOwner> BookOwners { get; } = [];
    public static List<ExchangeRequest> Exchanges { get; } = [];
    public static List<Message> Messages { get; } = [];
    public static List<Review> Reviews { get; } = [];
    public static List<Favorite> Favorites { get; } = [];
    public static List<Discussion> Discussions { get; } = [];
    public static List<DiscussionMessage> DiscussionMessages { get; } = [];
    public static List<QuizQuestion> QuizQuestions { get; } = [];
    public static List<BookOfTheDay> BooksOfTheDay { get; } = [];

    public static async Task LoadFromDbContextAsync(BookExchangeDbContext ctx)
    {
        Books.Clear(); Books.AddRange(await ctx.Books.AsNoTracking().ToListAsync());
        BookOwners.Clear(); BookOwners.AddRange(await ctx.BookOwners.AsNoTracking().ToListAsync());
        Exchanges.Clear(); Exchanges.AddRange(await ctx.ExchangeRequests.AsNoTracking()
            .Include(e => e.Sender).Include(e => e.Receiver)
            .Include(e => e.BookRequested).Include(e => e.BookOffered)
            .ToListAsync());
        Messages.Clear(); Messages.AddRange(await ctx.Messages.AsNoTracking().ToListAsync());
        Reviews.Clear(); Reviews.AddRange(await ctx.Reviews.AsNoTracking().ToListAsync());
        Favorites.Clear(); Favorites.AddRange(await ctx.Favorites.AsNoTracking().ToListAsync());
        Discussions.Clear(); Discussions.AddRange(await ctx.Discussions.AsNoTracking()
            .Include(d => d.Book).ToListAsync());
        DiscussionMessages.Clear(); DiscussionMessages.AddRange(await ctx.DiscussionMessages.AsNoTracking()
            .Include(dm => dm.User).ToListAsync());
        QuizQuestions.Clear(); QuizQuestions.AddRange(await ctx.QuizQuestions.AsNoTracking()
            .Include(q => q.Book).ToListAsync());
        BooksOfTheDay.Clear(); BooksOfTheDay.AddRange(await ctx.BooksOfTheDay.AsNoTracking().ToListAsync());
    }
}
