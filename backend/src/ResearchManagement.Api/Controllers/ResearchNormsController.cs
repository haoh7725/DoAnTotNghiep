using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchManagement.Application.ResearchNorms;
using ResearchManagement.Application.ResearchNorms.Models;

namespace ResearchManagement.Api.Controllers;

[ApiController]
[Route("api/research-norms")]
[Authorize]
public sealed class ResearchNormsController(
    ResearchNormService researchNormService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ResearchNormResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await researchNormService.GetAllAsync(
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ResearchNormResponse>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await researchNormService.GetByIdAsync(
            id,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ResearchNormResponse>> Create(
        CreateResearchNormRequest request,
        CancellationToken cancellationToken)
    {
        var result = await researchNormService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ResearchNormResponse>> Update(
        long id,
        UpdateResearchNormRequest request,
        CancellationToken cancellationToken)
    {
        var result = await researchNormService.UpdateAsync(
            id,
            request,
            cancellationToken);

        return Ok(result);
    }
}