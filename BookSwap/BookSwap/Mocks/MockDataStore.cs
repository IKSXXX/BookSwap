using BookSwap.Db.Entities;
using BookSwap.Db.Data;
using Microsoft.EntityFrameworkCore;

namespace BookSwap.Web.Mocks;

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
    public static List<Notification> Notifications { get; } = [];

    public static async Task LoadFromDbContextAsync(BookSwapDbContext ctx)
    {
        Books.Clear();
        Books.AddRange(await ctx.Books.AsNoTracking()
            .Include(b => b.BookOwners).ThenInclude(bo => bo.User)
            .ToListAsync());

        BookOwners.Clear();
        BookOwners.AddRange(await ctx.BookOwners.AsNoTracking()
            .Include(bo => bo.User).Include(bo => bo.Book)
            .ToListAsync());

        Exchanges.Clear();
        Exchanges.AddRange(await ctx.ExchangeRequests.AsNoTracking()
            .Include(e => e.Sender).Include(e => e.Receiver)
            .Include(e => e.BookRequested).Include(e => e.BookOffered)
            .ToListAsync());

        Messages.Clear();
        Messages.AddRange(await ctx.Messages.AsNoTracking().ToListAsync());

        Reviews.Clear();
        Reviews.AddRange(await ctx.Reviews.AsNoTracking()
            .Include(r => r.FromUser).Include(r => r.ToUser)
            .ToListAsync());

        Favorites.Clear();
        Favorites.AddRange(await ctx.Favorites.AsNoTracking()
            .Include(f => f.Book!).ThenInclude(b => b.BookOwners).ThenInclude(bo => bo.User)
            .ToListAsync());

        Discussions.Clear();
        Discussions.AddRange(await ctx.Discussions.AsNoTracking()
            .Include(d => d.Book).ToListAsync());

        DiscussionMessages.Clear();
        DiscussionMessages.AddRange(await ctx.DiscussionMessages.AsNoTracking()
            .Include(dm => dm.User).ToListAsync());

        QuizQuestions.Clear();
        QuizQuestions.AddRange(await ctx.QuizQuestions.AsNoTracking()
            .Include(q => q.Book).ToListAsync());

        BooksOfTheDay.Clear();
        BooksOfTheDay.AddRange(await ctx.BooksOfTheDay.AsNoTracking()
            .Include(bod => bod.Book!).ThenInclude(b => b.BookOwners).ThenInclude(bo => bo.User)
            .ToListAsync());
    }
}
