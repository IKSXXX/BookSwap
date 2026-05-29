using BookExchange.Web.Entities;

namespace BookExchange.Web.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IRepository<Book> Books { get; }
    IRepository<BookOwner> BookOwners { get; }
    IRepository<ExchangeRequest> Exchanges { get; }
    IRepository<Message> Messages { get; }
    IRepository<Review> Reviews { get; }
    IRepository<Favorite> Favorites { get; }
    IRepository<Discussion> Discussions { get; }
    IRepository<DiscussionMessage> DiscussionMessages { get; }
    IRepository<QuizQuestion> QuizQuestions { get; }
    IRepository<BookOfTheDay> BooksOfTheDay { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
