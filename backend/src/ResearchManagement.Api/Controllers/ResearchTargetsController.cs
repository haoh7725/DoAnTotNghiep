using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchManagement.Application.ResearchTargets;
using ResearchManagement.Application.ResearchTargets.Models;

namespace ResearchManagement.Api.Controllers;

[ApiController]
[Route("api/research-targets")]
[Authorize]
public sealed class ResearchTargetsController(
    ResearchTargetService researchTargetService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ResearchTargetResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await researchTargetService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ResearchTargetResponse>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await researchTargetService.GetByIdAsync(
            id,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ResearchTargetResponse>> Create(
        CreateResearchTargetRequest request,
        CancellationToken cancellationToken)
    {
        var result = await researchTargetService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ResearchTargetResponse>> Update(
        long id,
        UpdateResearchTargetRequest request,
        CancellationToken cancellationToken)
    {
        var result = await researchTargetService.UpdateAsync(
            id,
            request,
            cancellationToken);

        return Ok(result);
    }
}