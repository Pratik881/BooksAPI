﻿using System.ComponentModel.DataAnnotations;

namespace BookStoreApi.Models
{
	public class User
	{
		public int Id { get; set; }

		

		[Required]
		[EmailAddress]
		[StringLength(100)]
		public string Email { get; set; }

		[Required]
		public string PasswordHash { get; set; }

		public string Role { get; set; }

		public List<Book> Books { get;set; }=new List<Book>();


	}
}
