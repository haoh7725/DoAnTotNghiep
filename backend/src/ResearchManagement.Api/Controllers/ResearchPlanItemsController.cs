using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchManagement.Application.ResearchPlans;
using ResearchManagement.Application.ResearchPlans.Models;

namespace ResearchManagement.Api.Controllers;

[ApiController]
[Route("api/research-plan-items")]
[Authorize]
public sealed class ResearchPlanItemsController(
    ResearchPlanItemService researchPlanItemService) : ControllerBase
{
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ResearchPlanItemResponse>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await researchPlanItemService.GetByIdAsync(
            id,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("/api/research-plans/{researchPlanId:long}/items")]
    public async Task<ActionResult<IReadOnlyList<ResearchPlanItemResponse>>> GetByResearchPlanId(
        long researchPlanId,
        CancellationToken cancellationToken)
    {
        var result = await researchPlanItemService.GetByResearchPlanIdAsync(
            researchPlanId,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("/api/research-plans/{researchPlanId:long}/items")]
    public async Task<ActionResult<ResearchPlanItemResponse>> Create(
        long researchPlanId,
        [FromBody] CreateResearchPlanItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await researchPlanItemService.CreateAsync(
            researchPlanId,
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ResearchPlanItemResponse>> Update(
        long id,
        [FromBody] UpdateResearchPlanItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await researchPlanItemService.UpdateAsync(
            id,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPut("{id:long}/progress")]
    public async Task<ActionResult<ResearchPlanItemResponse>> UpdateProgress(
        long id,
        [FromBody] UpdateResearchPlanItemProgressRequest request,
        CancellationToken cancellationToken)
    {
        var result = await researchPlanItemService.UpdateProgressAsync(
            id,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:long}/progress-history")]
    public async Task<ActionResult<IReadOnlyList<ProgressHistoryResponse>>> GetProgressHistory(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await researchPlanItemService.GetProgressHistoryAsync(
            id,
            cancellationToken);

        return Ok(result);
    }
}