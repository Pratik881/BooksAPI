using BookStoreApi.Data;
using BookStoreApi.DTO;
using BookStoreApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
			var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
			if (userId == 0)
			{
				return Unauthorized("Invalid token or user not authenticated ");
			}
			var booksQuery = _context.Books.Where(b => b.UserId == userId);
			if (!string.IsNullOrEmpty(author))
			{
				booksQuery = booksQuery.Where(b => b.Author.Contains(author));
			}
			booksQuery = sort switch
			{
				"price_asc" => booksQuery.OrderBy(b => b.Price),
				"price_desc" => booksQuery.OrderByDescending(b => b.Price),
				"title_asc" => booksQuery.OrderBy(b => b.Title),
				"title_desc" => booksQuery.OrderByDescending(b => b.Title),
				_ => booksQuery
			};
			booksQuery = booksQuery.Skip((page - 1) * size).Take(size);

			return await booksQuery.ToListAsync();
		}

		[HttpPost]
		public async Task<ActionResult<Book>> PostBook(AddBookRequest request)
		{

			var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
			if (userIdClaim == null)
			{
				return Unauthorized("Invalid token:User id not found");

			}
			int userId = int.Parse(userIdClaim.Value);
			var user = await _context.Users.FindAsync(userId);
			if (user == null)
			{
				return BadRequest("user not found");
			}

			var book = new Book
			{
				Title = request.Title,
				Author = request.Author,
				Price = request.Price,
				UserId = userId

			};
			_context.Books.Add(book);
			await _context.SaveChangesAsync();
			var bookDTO = new BookDTO
			{
				Id = book.Id,
				Title = book.Title,
				Author = book.Author,
				Price = book.Price
			};
			return CreatedAtAction(nameof(GetBooks), new { id = book.Id }, bookDTO);

		}

		[HttpPut("{id}")]
		public async Task<ActionResult> PutBook(int id, PutBookDto bookDto)
		{
			if (bookDto == null || id != bookDto.Id)
			{
				return BadRequest("Invalid Book data");
			}

			var book=await _context.Books.FindAsync(id);
			if(book == null)
			{
				return NotFound(); 
			}
			book.Title=bookDto.Title;
			book.Author=bookDto.Author;
			book.Price=bookDto.Price;
			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!_context.Books.Any(b => b.Id == id))
				{
					return NotFound();

				}
				throw;
			}
			return NoContent();
			
		}
		
		[HttpDelete("{id}")]
		public async Task<ActionResult> DeleteBook(int id)
		{
			var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
			var book = await _context.Books.FindAsync(id);
			if (book == null)
			{
				return NotFound();
			}
				

			if(book.UserId!= userId)
			{
				return Forbid();
			}
			_context.Books.Remove(book);
            try
            {
                await _context.SaveChangesAsync(); 
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, "Internal server error while deleting the book.");
            }

			return NoContent();



        }
	}
	}

