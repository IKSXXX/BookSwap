using BookSwap.Db.Entities;
using BookSwap.Db.Interfaces;
using BookSwap.Web.Mocks;

namespace BookSwap.Tests.Testing;

public sealed class TestUnitOfWork : IUnitOfWork
{
    public List<Book> Books { get; } = [];
    public List<BookOwner> BookOwners { get; } = [];
    public List<ExchangeRequest> Exchanges { get; } = [];
    public List<Message> Messages { get; } = [];
    public List<Review> Reviews { get; } = [];
    public List<Favorite> Favorites { get; } = [];
    public List<Discussion> Discussions { get; } = [];
    public List<DiscussionMessage> DiscussionMessages { get; } = [];
    public List<QuizQuestion> QuizQuestions { get; } = [];
    public List<BookOfTheDay> BooksOfTheDay { get; } = [];
    public List<Notification> Notifications { get; } = [];

    IRepository<Book> IUnitOfWork.Books => new MockRepository<Book>(Books);
    IRepository<BookOwner> IUnitOfWork.BookOwners => new MockRepository<BookOwner>(BookOwners);
    IRepository<ExchangeRequest> IUnitOfWork.Exchanges => new MockRepository<ExchangeRequest>(Exchanges);
    IRepository<Message> IUnitOfWork.Messages => new MockRepository<Message>(Messages);
    IRepository<Review> IUnitOfWork.Reviews => new MockRepository<Review>(Reviews);
    IRepository<Favorite> IUnitOfWork.Favorites => new MockRepository<Favorite>(Favorites);
    IRepository<Discussion> IUnitOfWork.Discussions => new MockRepository<Discussion>(Discussions);
    IRepository<DiscussionMessage> IUnitOfWork.DiscussionMessages => new MockRepository<DiscussionMessage>(DiscussionMessages);
    IRepository<QuizQuestion> IUnitOfWork.QuizQuestions => new MockRepository<QuizQuestion>(QuizQuestions);
    IRepository<BookOfTheDay> IUnitOfWork.BooksOfTheDay => new MockRepository<BookOfTheDay>(BooksOfTheDay);
    IRepository<Notification> IUnitOfWork.Notifications => new MockRepository<Notification>(Notifications);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
