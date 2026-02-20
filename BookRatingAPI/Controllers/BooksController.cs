using System.Collections.Generic;
using System.Threading.Tasks;
using BookRatingAPI.DTOs;
using BookRatingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookRatingAPI.Controllers;

/// <summary>
/// Controller for managing books
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
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
        var books = await _bookService.GetBooksAsync(search, categoryId);
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
        var book = await _bookService.GetBookByIdAsync(id);

        if (book == null)
            return NotFound();

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
        var book = await _bookService.CreateBookAsync(dto);
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
        var book = await _bookService.UpdateBookAsync(id, dto);

        if (book == null)
            return NotFound();

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
        var deleted = await _bookService.DeleteBookAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
