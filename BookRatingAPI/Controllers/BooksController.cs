using System.Collections.Generic;
using System.Threading.Tasks;
using BookRatingAPI.DTOs;
using BookRatingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BookRatingAPI.Controllers;

// Controller for managing books
// GET endpoints are public, CUD operations require Admin role
[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly ILogger<BooksController> _logger;

    public BooksController(IBookService bookService, ILogger<BooksController> logger)
    {
        _bookService = bookService;
        _logger = logger;
    }

    // GET /api/books
    // Public endpoint - no authentication required
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookDto>>> GetBooks(
        [FromQuery] string? search,
        [FromQuery] int? categoryId
    )
    {
        _logger.LogInformation("Fetching books with search={Search}, categoryId={CategoryId}", search, categoryId);
        var books = await _bookService.GetBooksAsync(search, categoryId);
        _logger.LogInformation("Retrieved {Count} books", books.Count);
        return Ok(books);
    }

    // GET /api/books/{id}
    // Public endpoint - no authentication required
    [HttpGet("{id}")]
    public async Task<ActionResult<BookDto>> GetBook(int id)
    {
        _logger.LogInformation("Fetching book with Id={BookId}", id);
        var book = await _bookService.GetBookByIdAsync(id);

        if (book == null)
        {
            _logger.LogWarning("Book not found: Id={BookId}", id);
            return NotFound(new { Message = "Book not found" });
        }

        return Ok(book);
    }

    // POST /api/books
    // Requires Admin role - JWT token with "Admin" role claim required
    // Authorization flow:
    // 1. UseAuthentication() validates JWT token and sets HttpContext.User
    // 2. UseAuthorization() checks [Authorize(Roles = "Admin")]
    // 3. Returns 401 if not authenticated, 403 if not Admin
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<BookDto>> CreateBook(CreateBookDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for book creation");
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Creating book: Title={Title}", dto.Title);
        var book = await _bookService.CreateBookAsync(dto);
        _logger.LogInformation("Book created successfully: Id={BookId}", book.Id);
        return CreatedAtAction(nameof(GetBook), new { id = book.Id }, book);
    }

    // PUT /api/books/{id}
    // Requires Admin role
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<BookDto>> UpdateBook(int id, CreateBookDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for book update: Id={BookId}", id);
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Updating book: Id={BookId}", id);
        var book = await _bookService.UpdateBookAsync(id, dto);

        if (book == null)
        {
            _logger.LogWarning("Book not found for update: Id={BookId}", id);
            return NotFound(new { Message = "Book not found" });
        }

        _logger.LogInformation("Book updated successfully: Id={BookId}", id);
        return Ok(book);
    }

    // DELETE /api/books/{id}
    // Requires Admin role
    // Consider implementing soft delete (mark as deleted) instead of hard delete
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBook(int id)
    {
        _logger.LogInformation("Deleting book: Id={BookId}", id);
        var deleted = await _bookService.DeleteBookAsync(id);

        if (!deleted)
        {
            _logger.LogWarning("Book not found for deletion: Id={BookId}", id);
            return NotFound(new { Message = "Book not found" });
        }

        _logger.LogInformation("Book deleted successfully: Id={BookId}", id);
        return NoContent();
    }
}
