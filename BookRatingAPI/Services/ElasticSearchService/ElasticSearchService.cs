using BookRatingAPI.Data;
using BookRatingAPI.DTOs;
using BookRatingAPI.Models;
using Microsoft.EntityFrameworkCore;
using Nest;

namespace BookRatingAPI.Services
{
    public class ElasticSearchService : IElasticSearchService
    {
        private readonly AppDbContext _context;
        private readonly IElasticClient _elastic;
        private readonly ILogger<ElasticSearchService> _logger;

        public ElasticSearchService(
            AppDbContext context,
            IElasticClient elastic,
            ILogger<ElasticSearchService> logger
        )
        {
            _context = context;
            _elastic = elastic;
            _logger = logger;
        }

        public async Task Migrate()
        {
            const int batchSize = 500;
            int totalMigrated = 0;

            var totalCount = await _context.Books.CountAsync();
            _logger.LogInformation("Starting migration of {totalCount} books", totalCount);

            for (int i = 0; i < totalCount; i += batchSize)
            {
                var batch = await _context
                    .Books.Include(b => b.Category)
                    .Include(b => b.Ratings)
                    .OrderBy(b => b.Id)
                    .Skip(i)
                    .Take(batchSize)
                    .ToListAsync();

                if (!batch.Any())
                    continue;

                var batchToIndex = batch.Select(b => MapToDto(b)).ToList();

                var response = await _elastic.BulkAsync(b =>
                    b.IndexMany(
                            batchToIndex,
                            (descriptor, doc) =>
                            {
                                if (doc.Id == 0)
                                    throw new InvalidOperationException("Book Id cannot be 0");

                                return descriptor.Id(doc.Id);
                            }
                        )
                        .Refresh(Elasticsearch.Net.Refresh.WaitFor)
                );

                if (response.Errors)
                {
                    foreach (var item in response.ItemsWithErrors)
                        _logger.LogError(
                            "Failed Book {Id}: {Error.Reason}",
                            item.Id,
                            item.Error.Reason
                        );
                }
                totalMigrated += batchToIndex.Count;
                _logger.LogInformation("Migrated {Count} / {Total}", totalMigrated, totalCount);
            }
        }

        public async Task<List<BookDto>> GetBooksElastic(string? title, string? author)
        {
            var mustQueries = new List<Func<QueryContainerDescriptor<BookDto>, QueryContainer>>();

            if (!string.IsNullOrWhiteSpace(title))
            {
                mustQueries.Add(q =>
                    q.Fuzzy(fz => fz.Field(f => f.Title).Value(title).Fuzziness(Fuzziness.Auto))
                );
            }

            if (!string.IsNullOrWhiteSpace(author))
            {
                mustQueries.Add(q =>
                    q.Fuzzy(fz => fz.Field(f => f.Author).Value(author).Fuzziness(Fuzziness.Auto))
                );
            }

            var searchResponse = await _elastic.SearchAsync<BookDto>(s =>
            {
                s.Index("books").Size(10000);

                if (mustQueries.Any())
                    s.Query(q => q.Bool(b => b.Must(mustQueries)));
                else
                    s.Query(q => q.MatchAll());

                return s;
            });

            if (!searchResponse.IsValid)
                _logger.LogError(
                    "Elasticsearch search failed: {DebugInfo}",
                    searchResponse.DebugInformation
                );
            else
                _logger.LogInformation(
                    "Elasticsearch query executed: {DebugInfo}",
                    searchResponse.DebugInformation
                );

            var books = searchResponse.Documents.ToList();

            if (!books.Any())
                _logger.LogInformation(
                    "No books found. Title='{Title}', Author='{Author}'",
                    title,
                    author
                );

            return books;
        }

        public async Task<List<BookDto>> GetTopRatedBooks()
        {
            var searchResponse = await _elastic.SearchAsync<BookDto>(s =>
                s.Index("books")
                    .Size(10)
                    .Query(q => q.MatchAll())
                    .Sort(s =>
                        s.Field(f => f.AverageRating, SortOrder.Descending)
                            .Field(f => f.RatingCount, SortOrder.Descending)
                    )
            );

            var books = searchResponse.Documents.ToList();
            if (!books.Any())
                _logger.LogInformation("Books Empty");

            return books;
        }

        public async Task<List<BookDto>> GetBooksByCategory(string category)
        {
            var searchResponse = await _elastic.SearchAsync<BookDto>(s =>
                s.Index("books")
                    .Size(10000)
                    .Query(q =>
                    {
                        if (string.IsNullOrWhiteSpace(category))
                            _logger.LogInformation("Category Field Empty!");

                        return q.Bool(b =>
                            b.Must(q => q.Term(t => t.Field(f => f.CategoryName).Value(category)))
                        );
                    })
            );

            var books = searchResponse.Documents.ToList();
            if (!books.Any())
                _logger.LogInformation("Books Empty");

            return books;
        }

        public async Task<List<BookDto>> GetBooksByYear(int year)
        {
            var searchResponse = await _elastic.SearchAsync<BookDto>(s =>
                s.Index("books")
                    .Size(10000)
                    .Query(q =>
                        q.Bool(b =>
                            b.Must(q => q.Term(t => t.Field(f => f.PublicationYear).Value(year)))
                        )
                    )
            );

            var books = searchResponse.Documents.ToList();

            if (!books.Any())
                _logger.LogInformation("Books Empty");

            return books;
        }

        public async Task<List<BookDto>> RatingFiltering(int rating)
        {
            var searchResponse = await _elastic.SearchAsync<BookDto>(s =>
                s.Index("books")
                    .Size(10000)
                    .Query(q =>
                        q.Range(r =>
                            r.Field(f => f.AverageRating)
                                .GreaterThanOrEquals(rating)
                                .LessThan(rating + 1)
                        )
                    )
            );

            var books = searchResponse.Documents.ToList();

            if (!books.Any())
                _logger.LogInformation("Books Empty");

            return books;
        }

        public async Task<List<BookDto>> Sort(string sortBy, string sortOrder)
        {
            var searchResponse = await _elastic.SearchAsync<BookDto>(s =>
                s.Index("books").Query(q => q.MatchAll()).Sort(GetSort(sortBy, sortOrder))
            );

            var books = searchResponse.Documents.ToList();

            if (!books.Any())
                _logger.LogInformation("Books Empty");

            return books;
        }

        public async Task UpsertBook(int id)
        {
            var updatedBook = await _context
                .Books.Include(b => b.Category)
                .Include(b => b.Ratings)
                .FirstOrDefaultAsync(b => b.Id == id);

            var result = MapToDto(updatedBook);

            var response = await _elastic.IndexAsync(
                result,
                i => i.Index("books").Id(result.Id).Refresh(Elasticsearch.Net.Refresh.True)
            );

            if (!response.IsValid)
                _logger.LogError(
                    "Failed to upsert Book {Id}: {Reason}",
                    id,
                    response.OriginalException?.Message ?? response.ServerError?.ToString()
                );
            else
                _logger.LogInformation("Upserted Book {Id} successfully", id);
        }

        public async Task DeleteBook(int bookId)
        {
            var response = await _elastic.DeleteAsync<BookDto>(bookId, d => d.Index("books"));

            if (!response.IsValid)
                _logger.LogError(
                    "Failed to delete Book {Id}: {Reason}",
                    bookId,
                    response.OriginalException?.Message ?? response.ServerError?.ToString()
                );
            else
                _logger.LogInformation("Deleted Book {Id} successfully", bookId);
        }

        private static BookDto MapToDto(Book book)
        {
            return new BookDto
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                Description = book.Description,
                CoverImageUrl = book.CoverImageUrl,
                PublicationYear = book.PublicationYear,
                ISBN = book.ISBN,
                CategoryId = book.CategoryId,
                CategoryName = book.Category.Name,
                AverageRating = book.Ratings.Any() ? book.Ratings.Average(r => r.Score) : 0,
                RatingCount = book.Ratings.Count,
            };
        }

        private Func<SortDescriptor<BookDto>, IPromise<IList<ISort>>> GetSort(
            string sortBy,
            string sortOrder
        )
        {
            var ascending = sortOrder.ToLower() == "asc";

            return s =>
            {
                switch (sortBy.ToLower())
                {
                    case "title":
                        return ascending
                            ? s.Ascending("title.keyword")
                            : s.Descending("title.keyword");

                    case "author":
                        return ascending
                            ? s.Ascending("author.keyword")
                            : s.Descending("author.keyword");

                    case "rating":
                        return ascending
                            ? s.Ascending(f => f.AverageRating).Ascending(f => f.RatingCount)
                            : s.Descending(f => f.AverageRating).Descending(f => f.RatingCount);

                    case "year":
                        return ascending
                            ? s.Ascending(f => f.PublicationYear)
                            : s.Descending(f => f.PublicationYear);

                    default:
                        return s.Descending(f => f.AverageRating);
                }
            };
        }
    }
}
