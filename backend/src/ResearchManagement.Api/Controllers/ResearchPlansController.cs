using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchManagement.Application.ResearchPlans;
using ResearchManagement.Application.ResearchPlans.Models;

namespace ResearchManagement.Api.Controllers;

[ApiController]
[Route("api/research-plans")]
[Authorize]
public sealed class ResearchPlansController(
    ResearchPlanService researchPlanService) : ControllerBase
{
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ResearchPlanResponse>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await researchPlanService.GetByIdAsync(
            id,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ResearchPlanResponse>> Create(
        [FromBody] CreateResearchPlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await researchPlanService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ResearchPlanResponse>> Update(
        long id,
        [FromBody] UpdateResearchPlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await researchPlanService.UpdateAsync(
            id,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:long}/submit")]
    public async Task<ActionResult<ResearchPlanResponse>> Submit(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await researchPlanService.SubmitAsync(
            id,
            cancellationToken);

        return Ok(result);
    }
}