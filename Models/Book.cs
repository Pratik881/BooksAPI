using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BookStoreApi.Models
{
	public class Book
	{
		public int Id { get; set; }
		public string Title { get; set; }

		public string Author { get; set; }

		[Precision(18,4)]
		public decimal Price { get; set; }

		public int UserId {  get; set; }

		public User User { get; set; }

	}
}
