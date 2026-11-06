using Library.DTOs.Books;
using Library.DTOs.Common;
using Library.Filters;
using Library.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Controllers;

[ApiController]
[Route("api/books")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<BookResponse>>> GetBooks([FromQuery] BookQuery query)
    {
        return Ok(await _bookService.GetBooksAsync(query));
    }

    [HttpPost]
    [Authorize(Roles = "LIBRARIAN")]
    [ServiceFilter(typeof(ValidationFilter), Order = -3000)]
    public async Task<ActionResult<BookResponse>> Create(CreateBookRequest request)
    {
        return Ok(await _bookService.CreateAsync(request));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "LIBRARIAN")]
    public async Task<ActionResult<BookResponse>> Update(int id, UpdateBookRequest request)
    {
        return Ok(await _bookService.UpdateAsync(id, request));
    }
}
