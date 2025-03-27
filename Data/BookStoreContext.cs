using BookStoreApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BookStoreApi.Data
{
	public class BookStoreContext:DbContext
	{
		public BookStoreContext(DbContextOptions<BookStoreContext> options) : base(options)
		{

		}
		public DbSet<Book> Books { get; set; }
		public DbSet<User> Users { get; set; }

		public DbSet<RefreshToken> RefreshTokens { get; set; }
	}
}
