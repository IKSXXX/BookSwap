using BookExchange.Web.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BookExchange.Web.Data;

public class BookExchangeDbContext : IdentityDbContext<User>
{
    public BookExchangeDbContext(DbContextOptions<BookExchangeDbContext> options)
        : base(options)
    {
    }
}
