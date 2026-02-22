using System.Collections.Generic;
using System.Threading.Tasks;
using BookRatingAPI.DTOs;
using BookRatingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BookRatingAPI.Controllers;

/// <summary>
/// Controller for managing books
/// </summary>
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

    /// <summary>
    /// Get all books with optional filtering
    /// </summary>
    /// <param name="search">Search term for title, author, or description</param>
    /// <param name="categoryId">Optional category ID to filter by</param>
    /// <returns>List of books</returns>
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

    /// <summary>
    /// Get a specific book by ID
    /// </summary>
    /// <param name="id">The ID of the book</param>
    /// <returns>The book if found</returns>
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

    /// <summary>
    /// Create a new book (Admin only)
    /// </summary>
    /// <param name="dto">Book creation data</param>
    /// <returns>The created book</returns>
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

    /// <summary>
    /// Update an existing book (Admin only)
    /// </summary>
    /// <param name="id">The ID of the book to update</param>
    /// <param name="dto">Updated book data</param>
    /// <returns>The updated book</returns>
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

    /// <summary>
    /// Delete a book (Admin only)
    /// </summary>
    /// <param name="id">The ID of the book to delete</param>
    /// <returns>No content on success</returns>
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
