using BookStoreApi.Data;
using BookStoreApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStoreApi.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class BooksController : ControllerBase
	{
		private readonly BookStoreContext _context;

		public BooksController(BookStoreContext context)
		{
			_context = context;
		}

		//Get :api/Books
		[HttpGet]
		public async Task<ActionResult<IEnumerable<Book>>> GetBooks(
			string? author,
			string? sort,
			int page = 1,
			int size = 100

			)
		{
			var books = _context.Books.AsQueryable();
			if (!string.IsNullOrEmpty(author))
			{
				books = _context.Books.Where(b => b.Author.Contains(author));
			}
			//Sorting
			if (sort == "price_asc")
			{
				books = books.OrderBy(b => b.Price);
			}
			else if (sort == "price_desc")
			{
				books = books.OrderByDescending(b => b.Price);

			}
			else if (sort == "title_asc")
			{
				books = books.OrderBy(b => b.Title);
			}
			else if (sort == "title_desc")
			{
				books = books.OrderByDescending(b => b.Title);
			}

			//Pagination
			books=books.Skip((page-1)*size).Take(size);
			return await books.ToListAsync();

		}


		[HttpGet("{id}")]
		public async Task<ActionResult<Book>> GetBook(int id)
		{
			var book = await _context.Books.FindAsync(id);

			if (book == null)
			{
				return NotFound();
			}
			return Ok(book);
		}

		[HttpPost]
		public async Task<ActionResult<Book>> PostBook(Book book)
		{
			_context.Books.Add(book);
			await _context.SaveChangesAsync();
			return CreatedAtAction(nameof(GetBook), new { id = book.Id }, book);

		}

		[HttpPut("{id}")]
		public async Task<ActionResult> PutBook(int id, Book book)
		{
			if (id != book.Id)
			{
				return BadRequest();
			}
			_context.Entry(book).State = EntityState.Modified;

			try
			{
				await _context.SaveChangesAsync();
			}
			catch(DbUpdateConcurrencyException e)
			{
				if (!_context.Books.Any(b => b.Id == id))
				{
					return NotFound();
				}
				else
				{
					throw;
				}
			}
			return NoContent();


		}
		[Authorize(Roles ="Admin")]
		[HttpDelete("{id}")]
		public async Task<ActionResult> DeleteBook(int id)
		{
			var book = await _context.Books.FindAsync(id);
			if (book == null)  return NotFound();
				_context.Books.Remove(book);
			await _context.SaveChangesAsync();
			return NoContent();
				

		}
	}
	}

