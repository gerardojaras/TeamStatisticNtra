using Microsoft.AspNetCore.Mvc;
using TeamSearch.Application.Services;
using TeamSearch.Shared;
using TeamSearch.Shared.Dtos;

namespace TeamSearch.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class TeamRecordsController : ControllerBase
{
    private readonly ITeamRecordService _service;

    public TeamRecordsController(ITeamRecordService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse<List<TeamRecordDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<TeamRecordDto>>>> List([FromQuery] TeamRecordQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = query ?? new TeamRecordQuery();
        var list = await _service.ListAsync(q, cancellationToken).ConfigureAwait(false);
        var total = await _service.CountAsync(q, cancellationToken).ConfigureAwait(false);
        var meta = new { total, page = q.Page, pageSize = q.PageSize };
        return Ok(ApiResponse<List<TeamRecordDto>>.SuccessResponse(list, meta));
    }

    [HttpGet("{id:int}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse<TeamRecordDto?>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TeamRecordDto?>>> Get(int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return Ok(item == null
            ? ApiResponse<TeamRecordDto?>.Failure("not_found", "Not found")
            : ApiResponse<TeamRecordDto?>.SuccessResponse(item));
    }
}