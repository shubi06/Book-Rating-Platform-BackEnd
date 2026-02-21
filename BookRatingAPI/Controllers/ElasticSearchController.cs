using BookRatingAPI.DTOs;
using BookRatingAPI.Services;
using Microsoft.AspNetCore.Authorization;
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

        [HttpPost("migrate")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Migrate()
        {
            try
            {
                await _elastic.Migrate();
                return Ok("Migration Succesfule!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Elasticsearch migration");
                return StatusCode(500, "Migration failed!");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetBooks(
            [FromQuery] string? title,
            [FromQuery] string? author
        )
        {
            var books = await _elastic.GetBooksElastic(title, author);
            return Ok(books);
        }
    }
}
