using BookExchange.Db.Entities;

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
}
