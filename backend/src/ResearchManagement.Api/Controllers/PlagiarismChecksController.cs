using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchManagement.Application.Plagiarism;
using ResearchManagement.Application.Plagiarism.Models;

namespace ResearchManagement.Api.Controllers;

[ApiController, Authorize, Route("api/products/{productId:long}/plagiarism-checks")]
public sealed class PlagiarismChecksController(PlagiarismCheckService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlagiarismCheckResponse>>> List(long productId, CancellationToken ct) =>
        Ok(await service.ListAsync(productId, ct));

    [HttpPost]
    public async Task<ActionResult<PlagiarismCheckResponse>> Create(long productId,
        CreatePlagiarismCheckRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(productId, request, ct);
        return Accepted(result);
    }
}
