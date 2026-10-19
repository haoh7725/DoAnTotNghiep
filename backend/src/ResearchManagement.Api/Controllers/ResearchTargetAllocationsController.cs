using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchManagement.Application.ResearchTargetAllocations;
using ResearchManagement.Application.ResearchTargetAllocations.Models;
using ResearchManagement.Api.Authorization;
namespace ResearchManagement.Api.Controllers;

[ApiController]
[Route("api/research-target-allocations")]
[Authorize]
public sealed class ResearchTargetAllocationsController(
    ResearchTargetAllocationService service) : ControllerBase
{
    [HttpGet("{id:long}")]
    [Authorize(Policy = Policies.Management)]
    public async Task<ActionResult<ResearchTargetAllocationResponse>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Policy = Policies.Management)]
    public async Task<ActionResult<List<ResearchTargetAllocationResponse>>> GetByResearchTarget(
        [FromQuery] long researchTargetId,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByResearchTargetIdAsync(
            researchTargetId,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Policies.ResearchOffice)]
    public async Task<ActionResult<ResearchTargetAllocationResponse>> Create(
        CreateResearchTargetAllocationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = Policies.ResearchOffice)]
    public async Task<ActionResult<ResearchTargetAllocationResponse>> Update(
        long id,
        UpdateResearchTargetAllocationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(
            id,
            request,
            cancellationToken);

        return Ok(result);
    }
}