using BookSwap.Db.Data;
using BookSwap.Db.Entities;
using BookSwap.Db.Interfaces;

namespace BookSwap.Db.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly BookSwapDbContext _context;

    private IRepository<Book>? _books;
    private IRepository<BookOwner>? _bookOwners;
    private IRepository<ExchangeRequest>? _exchanges;
    private IRepository<Message>? _messages;
    private IRepository<Review>? _reviews;
    private IRepository<Favorite>? _favorites;
    private IRepository<Discussion>? _discussions;
    private IRepository<DiscussionMessage>? _discussionMessages;
    private IRepository<QuizQuestion>? _quizQuestions;
    private IRepository<BookOfTheDay>? _booksOfTheDay;
    private IRepository<Notification>? _notifications;

    public UnitOfWork(BookSwapDbContext context) => _context = context;

    public IRepository<Book> Books => _books ??= new Repository<Book>(_context);
    public IRepository<BookOwner> BookOwners => _bookOwners ??= new Repository<BookOwner>(_context);
    public IRepository<ExchangeRequest> Exchanges => _exchanges ??= new Repository<ExchangeRequest>(_context);
    public IRepository<Message> Messages => _messages ??= new Repository<Message>(_context);
    public IRepository<Review> Reviews => _reviews ??= new Repository<Review>(_context);
    public IRepository<Favorite> Favorites => _favorites ??= new Repository<Favorite>(_context);
    public IRepository<Discussion> Discussions => _discussions ??= new Repository<Discussion>(_context);
    public IRepository<DiscussionMessage> DiscussionMessages => _discussionMessages ??= new Repository<DiscussionMessage>(_context);
    public IRepository<QuizQuestion> QuizQuestions => _quizQuestions ??= new Repository<QuizQuestion>(_context);
    public IRepository<BookOfTheDay> BooksOfTheDay => _booksOfTheDay ??= new Repository<BookOfTheDay>(_context);
    public IRepository<Notification> Notifications => _notifications ??= new Repository<Notification>(_context);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);

    public async ValueTask DisposeAsync()
        => await _context.DisposeAsync();
}
