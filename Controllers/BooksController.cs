using Library.DTOs.Books;
using Library.DTOs.Common;
using Library.DTOs.Copies;
using Library.Filters;
using Library.Models.Enums;
using Library.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Controllers;

[ApiController]
[Route("api/books")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly ICopyService _copyService;

    public BooksController(IBookService bookService, ICopyService copyService)
    {
        _bookService = bookService;
        _copyService = copyService;
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
    [HttpPatch("{id:int}")]
    [Authorize(Roles = "LIBRARIAN")]
    public async Task<ActionResult<BookResponse>> Update(int id, UpdateBookRequest request)
    {
        return Ok(await _bookService.UpdateAsync(id, request));
    }

    [HttpGet("{id:int}/copies")]
    [Authorize(Roles = "LIBRARIAN")]
    public async Task<ActionResult<List<CopyResponse>>> GetCopies(int id, [FromQuery] BookCopyStatus? status)
    {
        return Ok(await _copyService.GetByBookAsync(id, status));
    }

    [HttpPost("{id:int}/copies")]
    [Authorize(Roles = "LIBRARIAN")]
    [ServiceFilter(typeof(ValidationFilter), Order = -3000)]
    public async Task<ActionResult<CopyResponse>> AddCopy(int id, CreateCopyRequest request)
    {
        return Ok(await _copyService.CreateAsync(id, request));
    }
}
