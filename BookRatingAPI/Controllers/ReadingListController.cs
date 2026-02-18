using System.Security.Claims;
using System.Threading.Tasks;
using BookRatingAPI.DTOs;
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
}
