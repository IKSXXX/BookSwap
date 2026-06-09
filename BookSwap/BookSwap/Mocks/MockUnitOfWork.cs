using BookSwap.Db.Entities;
using BookSwap.Db.Interfaces;

namespace BookSwap.Web.Mocks;

public class MockUnitOfWork : IUnitOfWork
{
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
    private IRepository<Notification>? _notifications;

    public IRepository<Book> Books => _books ??= new MockRepository<Book>(MockDataStore.Books);
    public IRepository<BookOwner> BookOwners => _bookOwners ??= new MockRepository<BookOwner>(MockDataStore.BookOwners);
    public IRepository<ExchangeRequest> Exchanges => _exchanges ??= new MockRepository<ExchangeRequest>(MockDataStore.Exchanges);
    public IRepository<Message> Messages => _messages ??= new MockRepository<Message>(MockDataStore.Messages);
    public IRepository<Review> Reviews => _reviews ??= new MockRepository<Review>(MockDataStore.Reviews);
    public IRepository<Favorite> Favorites => _favorites ??= new MockRepository<Favorite>(MockDataStore.Favorites);
    public IRepository<Discussion> Discussions => _discussions ??= new MockRepository<Discussion>(MockDataStore.Discussions);
    public IRepository<DiscussionMessage> DiscussionMessages => _discussionMessages ??= new MockRepository<DiscussionMessage>(MockDataStore.DiscussionMessages);
    public IRepository<QuizQuestion> QuizQuestions => _quiz ??= new MockRepository<QuizQuestion>(MockDataStore.QuizQuestions);
    public IRepository<BookOfTheDay> BooksOfTheDay => _bookOfDay ??= new MockRepository<BookOfTheDay>(MockDataStore.BooksOfTheDay);
    public IRepository<Notification> Notifications => _notifications ??= new MockRepository<Notification>(MockDataStore.Notifications);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
