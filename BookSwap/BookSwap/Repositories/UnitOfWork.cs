using BookExchange.Web.Data;
using BookExchange.Web.Entities;
using BookExchange.Web.Interfaces;

namespace BookExchange.Web.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly BookExchangeDbContext _ctx;

    private IRepository<Book>? _books;
    private IRepository<BookOwner>? _bookOwners;
    private IRepository<ExchangeRequest>? _exchanges;
    private IRepository<Message>? _messages;
    private IRepository<Review>? _reviews;
    private IRepository<Favorite>? _favorites;
    private IRepository<Discussion>? _discussions;
    private IRepository<DiscussionMessage>? _discussionMessages;
    private IRepository<QuizQuestion>? _quiz;
    private IRepository<BookOfTheDay>? _bookOfDay;

    public UnitOfWork(BookExchangeDbContext ctx) => _ctx = ctx;

    public IRepository<Book> Books => _books ??= new Repository<Book>(_ctx);
    public IRepository<BookOwner> BookOwners => _bookOwners ??= new Repository<BookOwner>(_ctx);
    public IRepository<ExchangeRequest> Exchanges => _exchanges ??= new Repository<ExchangeRequest>(_ctx);
    public IRepository<Message> Messages => _messages ??= new Repository<Message>(_ctx);
    public IRepository<Review> Reviews => _reviews ??= new Repository<Review>(_ctx);
    public IRepository<Favorite> Favorites => _favorites ??= new Repository<Favorite>(_ctx);
    public IRepository<Discussion> Discussions => _discussions ??= new Repository<Discussion>(_ctx);
    public IRepository<DiscussionMessage> DiscussionMessages => _discussionMessages ??= new Repository<DiscussionMessage>(_ctx);
    public IRepository<QuizQuestion> QuizQuestions => _quiz ??= new Repository<QuizQuestion>(_ctx);
    public IRepository<BookOfTheDay> BooksOfTheDay => _bookOfDay ??= new Repository<BookOfTheDay>(_ctx);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _ctx.SaveChangesAsync(ct);

    public async ValueTask DisposeAsync() => await _ctx.DisposeAsync();
}
