using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchManagement.Api.Authorization;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.Lecturers;
using ResearchManagement.Application.Lecturers.Models;

namespace ResearchManagement.Api.Controllers;

/// <summary>
/// Hồ sơ giảng viên và lý lịch khoa học. Controller chỉ chặn theo vai trò; phạm vi khoa/bộ môn
/// và quyền chính chủ do LecturerService, ScientificProfileService kiểm tra.
/// </summary>
[ApiController]
[Route("api/lecturers")]
[Authorize]
public sealed class LecturersController(
    LecturerService lecturers,
    ScientificProfileService profiles) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<LecturerResponse>>> Search(
        [FromQuery] LecturerSearchQuery query, CancellationToken cancellationToken) =>
        Ok(await lecturers.SearchAsync(query, cancellationToken));

    // Các route "me" phải đứng cùng nhóm với {id:long}; ràng buộc :long nên không xung đột.
    [HttpGet("me")]
    public async Task<ActionResult<LecturerResponse>> GetMine(CancellationToken cancellationToken) =>
        Ok(await lecturers.GetMineAsync(cancellationToken));

    [HttpPut("me")]
    public async Task<ActionResult<LecturerResponse>> UpdateMine(
        [FromBody] UpdateMyLecturerRequest request, CancellationToken cancellationToken) =>
        Ok(await lecturers.UpdateMineAsync(request, cancellationToken));

    [HttpGet("me/scientific-profile")]
    public async Task<ActionResult<ScientificProfileResponse>> GetMyProfile(CancellationToken cancellationToken) =>
        Ok(await profiles.GetMineAsync(cancellationToken));

    [HttpPut("me/scientific-profile")]
    public async Task<ActionResult<ScientificProfileResponse>> UpsertMyProfile(
        [FromBody] ScientificProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await profiles.UpsertMineAsync(request, cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<LecturerResponse>> GetById(long id, CancellationToken cancellationToken) =>
        Ok(await lecturers.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = Policies.ResearchOffice)]
    public async Task<ActionResult<LecturerResponse>> Create(
        [FromBody] CreateLecturerRequest request, CancellationToken cancellationToken)
    {
        var result = await lecturers.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = Policies.ResearchOffice)]
    public async Task<ActionResult<LecturerResponse>> Update(
        long id, [FromBody] UpdateLecturerRequest request, CancellationToken cancellationToken) =>
        Ok(await lecturers.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:long}")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await lecturers.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:long}/scientific-profile")]
    public async Task<ActionResult<ScientificProfileResponse>> GetProfile(
        long id, CancellationToken cancellationToken) =>
        Ok(await profiles.GetAsync(id, cancellationToken));

    [HttpPut("{id:long}/scientific-profile")]
    public async Task<ActionResult<ScientificProfileResponse>> UpsertProfile(
        long id, [FromBody] ScientificProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await profiles.UpsertAsync(id, request, cancellationToken));
}
