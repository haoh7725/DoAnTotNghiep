using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.Plagiarism.Abstractions;
using ResearchManagement.Application.Plagiarism.Models;

namespace ResearchManagement.Infrastructure.Plagiarism;

public sealed class CopyleaksPlagiarismProvider(
    HttpClient http,
    CopyleaksOptions options,
    IMemoryCache cache) : IPlagiarismProvider
{
    private const string TokenCacheKey = "copyleaks-access-token";

    public async Task SubmitAsync(PlagiarismSubmission submission, CancellationToken ct)
    {
        EnsureConfigured();
        var token = await GetAccessTokenAsync(ct);
        var webhookUrl = $"{options.PublicBaseUrl}/api/integrations/copyleaks/webhooks/{submission.ScanId}/{{STATUS}}";
        var body = new
        {
            base64 = Convert.ToBase64String(submission.Content),
            filename = submission.FileName,
            properties = new
            {
                sandbox = options.Sandbox,
                developerPayload = submission.DeveloperPayload,
                sensitivityLevel = 3,
                cheatDetection = true,
                webhooks = new
                {
                    status = webhookUrl,
                    statusHeaders = new[] { new[] { "X-Copyleaks-Webhook-Secret", options.WebhookSecret } }
                },
                scanning = new { internet = true },
                indexing = new { copyleaksDb = false },
                exclude = new { quotes = true, citations = true, references = true, tableOfContents = true },
                pdf = new { create = true, title = $"Báo cáo tương đồng - {submission.FileName}", reportVersion = "latest" }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Put,
            $"https://api.copyleaks.com/v3/scans/submit/file/{submission.ScanId}")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(ct);
            throw new BusinessRuleException($"Copyleaks từ chối yêu cầu ({(int)response.StatusCode}): {Sanitize(detail)}");
        }
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (cache.TryGetValue<string>(TokenCacheKey, out var cached) && !string.IsNullOrWhiteSpace(cached))
            return cached;

        using var response = await http.PostAsJsonAsync("https://id.copyleaks.com/v3/account/login/api",
            new { email = options.Email, key = options.ApiKey }, ct);
        if (!response.IsSuccessStatusCode)
            throw new BusinessRuleException("Không thể xác thực với Copyleaks. Hãy kiểm tra email và API key.");
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct)
            ?? throw new BusinessRuleException("Copyleaks trả về phản hồi xác thực không hợp lệ.");
        cache.Set(TokenCacheKey, result.AccessToken, TimeSpan.FromHours(47));
        return result.AccessToken;
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.ApiKey) ||
            string.IsNullOrWhiteSpace(options.PublicBaseUrl) || string.IsNullOrWhiteSpace(options.WebhookSecret))
            throw new BusinessRuleException("Copyleaks chưa được cấu hình đầy đủ trên máy chủ.");
    }

    private static string Sanitize(string value) =>
        value.Length > 500 ? value[..500] : value.Replace('\r', ' ').Replace('\n', ' ');

    private sealed record LoginResponse([property: JsonPropertyName("access_token")] string AccessToken);
}
