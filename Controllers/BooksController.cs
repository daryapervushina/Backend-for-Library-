using Library.DTOs.Books;
using Library.DTOs.Common;
using Library.Services;
using Microsoft.AspNetCore.Mvc;

namespace Library.Controllers
{
    [ApiController]
    [Route("/api/books")]
    public class BooksController: ControllerBase
    {
        private readonly IBookService _bookService;
        public BookController(IBookService BookService)
        {
            _bookService = BookService;
        }
        [HttpPost]
        public async Task<ActionResult<PagedResponse<BookResponse>>> GetBooksAsync ([FromQuery]BookQuery bookQuery)
        {

        }
    }
}