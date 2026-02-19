namespace BookRatingAPI.Controllers
{
	using System.Security.Claims;
	using global::BookRatingAPI.Services;
	using Microsoft.AspNetCore.Authorization;
	using Microsoft.AspNetCore.Mvc;

	

	[Authorize]
	[ApiController]
	[Route("api/[controller]")]
	public class RatingsController : ControllerBase
	{
		private readonly IRatingService _ratingService;

		public RatingsController(IRatingService ratingService)
		{
			_ratingService = ratingService;
		}

		[AllowAnonymous]
		[HttpGet("book/{bookId}")]
		public async Task<ActionResult<IEnumerable<DTOs.RatingDto>>> GetBookRatings(int bookId)
		{
			var ratings = await _ratingService.GetBookRatingsAsync(bookId);
			return Ok(ratings);
		}

		[HttpGet("my")]
		public async Task<ActionResult<IEnumerable<DTOs.RatingDto>>> GetMyRatings()
		{
			var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
			var ratings = await _ratingService.GetUserRatingsAsync(userId);
			return Ok(ratings);
		}

		[HttpPost]
		public async Task<ActionResult<DTOs.RatingDto>> CreateRating(CreateRatingDto dto)
		{
			var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
			var rating = await _ratingService.CreateRatingAsync(userId, dto);

			if (rating == null)
				return BadRequest("You have already rated this book");

			return Ok(rating);
		}

		[HttpPut("{id}")]
		public async Task<ActionResult<DTOs.RatingDto>> UpdateRating(int id, CreateRatingDto dto)
		{
			var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
			var rating = await _ratingService.UpdateRatingAsync(id, userId, dto);

			if (rating == null)
				return NotFound();

			return Ok(rating);
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteRating(int id)
		{
			var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
			var deleted = await _ratingService.DeleteRatingAsync(id, userId);

			if (!deleted)
				return NotFound();

			return NoContent();
		}
	}

}
