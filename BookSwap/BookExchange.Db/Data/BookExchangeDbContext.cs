using BookSwap.Db.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BookSwap.Db.Data;

public class BookExchangeDbContext : IdentityDbContext<User>
{
    public BookExchangeDbContext(DbContextOptions<BookExchangeDbContext> options)
        : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookOwner> BookOwners => Set<BookOwner>();
    public DbSet<ExchangeRequest> ExchangeRequests => Set<ExchangeRequest>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Discussion> Discussions => Set<Discussion>();
    public DbSet<DiscussionMessage> DiscussionMessages => Set<DiscussionMessage>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<BookOfTheDay> BooksOfTheDay => Set<BookOfTheDay>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Book>(e =>
        {
            e.HasIndex(x => x.Genre);
            e.HasIndex(x => x.Title);
        });

        b.Entity<BookOwner>(e =>
        {
            e.HasIndex(x => new { x.BookId, x.UserId }).IsUnique();
            e.HasOne(x => x.Book).WithMany(bk => bk.BookOwners).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany(u => u.BookOwners).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ExchangeRequest>(e =>
        {
            e.HasOne(x => x.Sender).WithMany(u => u.SentRequests).HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Receiver).WithMany(u => u.ReceivedRequests).HasForeignKey(x => x.ReceiverId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.BookRequested).WithMany().HasForeignKey(x => x.BookRequestedId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.BookOffered).WithMany().HasForeignKey(x => x.BookOfferedId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<Message>(e =>
        {
            e.HasOne(x => x.ExchangeRequest).WithMany(r => r.Messages).HasForeignKey(x => x.ExchangeRequestId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Review>(e =>
        {
            e.HasOne(x => x.FromUser).WithMany(u => u.ReviewsGiven).HasForeignKey(x => x.FromUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ToUser).WithMany(u => u.ReviewsReceived).HasForeignKey(x => x.ToUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ExchangeRequest).WithMany(r => r.Reviews).HasForeignKey(x => x.ExchangeRequestId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Favorite>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.BookId, x.IsWishlist }).IsUnique();
            e.HasOne(x => x.User).WithMany(u => u.Favorites).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Book).WithMany(bk => bk.FavoritedBy).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Discussion>(e =>
        {
            e.HasOne(x => x.Book).WithMany(bk => bk.Discussions).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany(u => u.Discussions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<DiscussionMessage>(e =>
        {
            e.HasOne(x => x.Discussion).WithMany(d => d.Messages).HasForeignKey(x => x.DiscussionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany(u => u.DiscussionMessages).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<QuizQuestion>(e =>
        {
            e.HasOne(x => x.Book).WithMany(bk => bk.QuizQuestions).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<BookOfTheDay>(e =>
        {
            e.HasIndex(x => x.Date).IsUnique();
            e.HasOne(x => x.Book).WithMany().HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Notification>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.IsRead });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override int SaveChanges()
    {
        StampAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampAuditFields()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.CreatedAt == default)
                entry.Entity.CreatedAt = now;
            else if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }
    }
}
