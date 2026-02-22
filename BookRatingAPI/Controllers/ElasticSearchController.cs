using BookRatingAPI.DTOs;
using BookRatingAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookRatingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ElasticSearchController : ControllerBase
    {
        private readonly ILogger<ElasticSearchController> _logger;
        private readonly IElasticSearchService _elastic;

        public ElasticSearchController(
            ILogger<ElasticSearchController> logger,
            IElasticSearchService elastic
        )
        {
            _logger = logger;
            _elastic = elastic;
        }

        [HttpGet("GetBooks")]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetBooks(
            [FromQuery] string? title,
            [FromQuery] string? author
        )
        {
            var books = await _elastic.GetBooksElastic(title, author);
            return Ok(books);
        }

        [HttpGet("TopRated")]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetTopRated()
        {
            var books = await _elastic.GetTopRatedBooks();
            return Ok(books);
        }

        [HttpGet("ByCategory/{category}")]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetByCategory(string category)
        {
            var books = await _elastic.GetBooksByCategory(category);
            return Ok(books);
        }

        [HttpGet("ByYear/{year}")]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetByYear(int year)
        {
            var books = await _elastic.GetBooksByYear(year);
            return Ok(books);
        }

        [HttpGet("RatingFilter/{rating}")]
        public async Task<ActionResult<IEnumerable<BookDto>>> FilterByRating(int rating)
        {
            var books = await _elastic.RatingFiltering(rating);
            return Ok(books);
        }

        [HttpGet("SortBooks")]
        public async Task<ActionResult<IEnumerable<BookDto>>> SortBooks(
            [FromQuery] string sortBy = "rating",
            [FromQuery] string sortOrder = "desc"
        )
        {
            var validSortFields = new[] { "title", "author", "category", "rating", "year" };
            if (!validSortFields.Contains(sortBy.ToLower()))
                return BadRequest(
                    $"Invalid sortBy value. Valid options: {string.Join(", ", validSortFields)}"
                );

            if (sortOrder.ToLower() != "asc" && sortOrder.ToLower() != "desc")
                return BadRequest("Invalid sortOrder value. Use 'asc' or 'desc'.");

            var books = await _elastic.Sort(sortBy, sortOrder);
            return Ok(books);
        }
    }
}
