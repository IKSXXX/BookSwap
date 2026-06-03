namespace BookExchange.Db.Entities;

public class BookOfTheDay : BaseEntity
{
    public int BookId { get; set; }
    public Book? Book { get; set; }
    public DateTime Date { get; set; }
}
