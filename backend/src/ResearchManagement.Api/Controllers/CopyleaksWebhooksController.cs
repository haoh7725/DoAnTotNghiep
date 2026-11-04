using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchManagement.Application.Plagiarism;
using ResearchManagement.Application.Plagiarism.Models;
using ResearchManagement.Infrastructure.Plagiarism;

namespace ResearchManagement.Api.Controllers;

[ApiController, AllowAnonymous, Route("api/integrations/copyleaks/webhooks")]
public sealed class CopyleaksWebhooksController(
    PlagiarismCheckService service,
    CopyleaksOptions options) : ControllerBase
{
    [HttpPost("{scanId}/{status}")]
    public async Task<IActionResult> Receive(string scanId, string status, JsonElement payload, CancellationToken ct)
    {
        if (!SecretMatches(Request.Headers["X-Copyleaks-Webhook-Secret"].ToString()))
            return Unauthorized();

        if (status.Equals("completed", StringComparison.OrdinalIgnoreCase))
        {
            var document = payload.TryGetProperty("scannedDocument", out var d) ? d : default;
            var results = payload.TryGetProperty("results", out var r) ? r : default;
            var score = results.ValueKind == JsonValueKind.Object && results.TryGetProperty("score", out var s) &&
                        s.TryGetProperty("aggregatedScore", out var a) ? a.GetDecimal() : 0m;
            var sourceCount = CountArray(results, "internet") + CountArray(results, "database") +
                              CountArray(results, "batch") + CountArray(results, "repositories");
            var totalWords = GetInt(document, "totalWords");
            var credits = GetInt(document, "credits");
            string? report = null;
            if (payload.TryGetProperty("downloadableReport", out var downloadable) &&
                downloadable.TryGetProperty("report", out var reportElement))
                report = reportElement.GetString();
            await service.HandleCompletedAsync(new CopyleaksWebhookResult(
                scanId, score, sourceCount, totalWords, credits, report), ct);
        }
        else if (status.Equals("error", StringComparison.OrdinalIgnoreCase))
        {
            var message = payload.TryGetProperty("error", out var error) ? error.ToString() :
                "Copyleaks không thể hoàn thành kiểm tra.";
            await service.HandleErrorAsync(scanId, message, ct);
        }
        return Ok();
    }

    private bool SecretMatches(string supplied)
    {
        if (string.IsNullOrEmpty(options.WebhookSecret) || string.IsNullOrEmpty(supplied)) return false;
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(options.WebhookSecret), Encoding.UTF8.GetBytes(supplied));
    }

    private static int CountArray(JsonElement parent, string name) =>
        parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.Array ? value.GetArrayLength() : 0;

    private static int GetInt(JsonElement parent, string name) =>
        parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(name, out var value) &&
        value.TryGetInt32(out var number) ? number : 0;
}
