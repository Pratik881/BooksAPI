using BookStoreApi.Data;
using BookStoreApi.DTO;
using BookStoreApi.Models;
using Microsoft.AspNetCore.Authorization;
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


        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookDTO>>> GetBooks(
            string? author,
            string? sort,
            int page = 1,
            int size = 100)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            var booksQuery = _context.Books.Where(b => b.UserId == userId);

            if (!string.IsNullOrEmpty(author))
            {
                booksQuery = booksQuery.Where(b => b.Author.ToLower().Contains(author.ToLower()));
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
            var books = await booksQuery.Select(
                b => new BookDTO
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author=b.Author,
                    Price=b.Price

                }).ToListAsync();

            if (books.Count == 0)
            {
                return NotFound("No books found for this user.");
            }

            return Ok(books);
        }

        [HttpPost]
        public async Task<ActionResult<Book>> PostBook(AddBookRequest request)
        {
            if (request == null)
            {
                return BadRequest("Invalid request");
            }

            if (string.IsNullOrWhiteSpace(request.Title) ||
               string.IsNullOrWhiteSpace(request.Author) ||
               request.Price <= 0)
            {
                return BadRequest("Title, Author, and Price are required.");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
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
                return BadRequest("Invalid book data.");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
            if (book == null)
            {
                return NotFound("Book not found or doesn't belong to the user.");
            }

            book.Title = bookDto.Title;
            book.Author = bookDto.Author;
            book.Price = bookDto.Price;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict("The book was updated by another process.");
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteBook(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
            if (book == null)
            {
                return NotFound("Book not found or doesn't belong to the user.");
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
