using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BookRatingAPI.DTOs;
using BookRatingAPI.Models.Enums;
using BookRatingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BookRatingAPI.Controllers;

[ApiController]
[Route("api/readinglist")]
[Authorize]
public class ReadingListController : ControllerBase
{
    private readonly IReadingListService _readingListService;
    private readonly ILogger<ReadingListController> _logger;

    public ReadingListController(IReadingListService readingListService, ILogger<ReadingListController> logger)
    {
        _readingListService = readingListService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<ReadingListEntryDto>>> GetReadingList([FromQuery] ReadingStatus? status)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Unauthorized access attempt to reading list");
            return Unauthorized();
        }

        _logger.LogInformation("Fetching reading list for UserId={UserId}, Status={Status}", userId, status);
        var entries = await _readingListService.GetReadingListAsync(userId, status);
        _logger.LogInformation("Retrieved {Count} reading list entries for UserId={UserId}", entries.Count, userId);
        return Ok(entries);
    }

    [HttpPost]
    public async Task<ActionResult<ReadingListEntryDto>> AddToReadingList(AddToReadingListDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for adding to reading list");
            return BadRequest(ModelState);
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Unauthorized access attempt to add to reading list");
            return Unauthorized();
        }

        _logger.LogInformation("Adding book to reading list: BookId={BookId}, UserId={UserId}", dto.BookId, userId);
        var (entry, error) = await _readingListService.AddToReadingListAsync(userId, dto);

        if (error != null)
        {
            if (error == "Book not found.")
            {
                _logger.LogWarning("Book not found: BookId={BookId}", dto.BookId);
                return NotFound(new { message = error });
            }

            _logger.LogWarning("Duplicate reading list entry: BookId={BookId}, UserId={UserId}", dto.BookId, userId);
            return Conflict(new { message = error });
        }

        _logger.LogInformation("Book added to reading list successfully: EntryId={EntryId}", entry!.Id);
        return CreatedAtAction(nameof(AddToReadingList), new { }, entry);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ReadingListEntryDto>> UpdateStatus(int id, UpdateReadingListStatusDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for updating reading list status: EntryId={EntryId}", id);
            return BadRequest(ModelState);
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Unauthorized access attempt to update reading list");
            return Unauthorized();
        }

        _logger.LogInformation("Updating reading list status: EntryId={EntryId}, UserId={UserId}", id, userId);
        var (entry, error) = await _readingListService.UpdateStatusAsync(userId, id, dto);

        if (error != null)
        {
            _logger.LogWarning("Reading list entry not found: EntryId={EntryId}, UserId={UserId}", id, userId);
            return NotFound(new { message = error });
        }

        _logger.LogInformation("Reading list status updated successfully: EntryId={EntryId}", id);
        return Ok(entry);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RemoveFromReadingList(int id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Unauthorized access attempt to remove from reading list");
            return Unauthorized();
        }

        _logger.LogInformation("Removing from reading list: EntryId={EntryId}, UserId={UserId}", id, userId);
        var removed = await _readingListService.RemoveFromReadingListAsync(userId, id);

        if (!removed)
        {
            _logger.LogWarning("Reading list entry not found: EntryId={EntryId}, UserId={UserId}", id, userId);
            return NotFound(new { message = "Reading list entry not found." });
        }

        _logger.LogInformation("Removed from reading list successfully: EntryId={EntryId}", id);
        return NoContent();
    }
}
