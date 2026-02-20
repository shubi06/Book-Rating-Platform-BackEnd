using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BookRatingAPI.DTOs;
using BookRatingAPI.Models.Enums;
using BookRatingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookRatingAPI.Controllers;

[ApiController]
[Route("api/readinglist")]
[Authorize]
public class ReadingListController : ControllerBase
{
    private readonly IReadingListService _readingListService;

    public ReadingListController(IReadingListService readingListService)
    {
        _readingListService = readingListService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ReadingListEntryDto>>> GetReadingList([FromQuery] ReadingStatus? status)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var entries = await _readingListService.GetReadingListAsync(userId, status);
        return Ok(entries);
    }

    [HttpPost]
    public async Task<ActionResult<ReadingListEntryDto>> AddToReadingList(AddToReadingListDto dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var (entry, error) = await _readingListService.AddToReadingListAsync(userId, dto);

        if (error != null)
        {
            if (error == "Book not found.")
                return NotFound(new { message = error });

            // Duplicate entry
            return Conflict(new { message = error });
        }

        return CreatedAtAction(nameof(AddToReadingList), new { }, entry);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ReadingListEntryDto>> UpdateStatus(int id, UpdateReadingListStatusDto dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var (entry, error) = await _readingListService.UpdateStatusAsync(userId, id, dto);

        if (error != null)
            return NotFound(new { message = error });

        return Ok(entry);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RemoveFromReadingList(int id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var removed = await _readingListService.RemoveFromReadingListAsync(userId, id);

        if (!removed)
            return NotFound(new { message = "Reading list entry not found." });

        return NoContent();
    }
}
