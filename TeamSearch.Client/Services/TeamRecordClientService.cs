using System.Text.Json;
using TeamSearch.Shared;
using TeamSearch.Shared.Dtos;

namespace TeamSearch.Client.Services;

public class TeamRecordClientService : ITeamRecordClient
{
    private readonly HttpClient _http;

    public TeamRecordClientService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResponse<List<TeamRecordDto>>> ListAsync(string? search = null, string? fields = null,
        int page = 1, int pageSize = 20, string? sortBy = null, string? sortDir = null,
        CancellationToken cancellationToken = default)
    {
        var q = new Dictionary<string, string?>
        {
            ["search"] = search,
            ["Fields"] = fields,
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
            ["sortBy"] = sortBy,
            ["sortDir"] = sortDir
        };

        var qs = string.Join('&',
            q.Where(kv => !string.IsNullOrEmpty(kv.Value))
                .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}"));
        var url = "TeamRecords" + (string.IsNullOrEmpty(qs) ? string.Empty : "?" + qs);

        HttpResponseMessage res;
        try
        {
            res = await _http.GetAsync(url, cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<TeamRecordDto>>.Failure("http_error", ex.Message);
        }

        string content;
        try
        {
            content = await res.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<TeamRecordDto>>.Failure("read_error", ex.Message);
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            // No body returned
            if (!res.IsSuccessStatusCode)
                return ApiResponse<List<TeamRecordDto>>.Failure("http_error",
                    $"Status {(int)res.StatusCode} {res.ReasonPhrase} (empty body)");

            // Return empty success response
            return ApiResponse<List<TeamRecordDto>>.SuccessResponse(new List<TeamRecordDto>());
        }

        // If the server returned HTML (e.g., SPA index.html) instead of JSON, return a helpful failure
        var mediaType = res.Content.Headers.ContentType?.MediaType;
        if (!string.IsNullOrEmpty(mediaType) && mediaType.Contains("html", StringComparison.OrdinalIgnoreCase))
        {
            // Try a common local API fallback (developer convenience) before failing, but only once
            var fallbackUrl = "https://localhost:7216/TeamRecords";
            var fallbackResp = await TryFallbackCallAsync(fallbackUrl, cancellationToken);
            if (fallbackResp != null)
                return fallbackResp;

            return ApiResponse<List<TeamRecordDto>>.Failure("unexpected_html",
                $"Expected JSON from API but received HTML (status {(int)res.StatusCode}). Is the API server running at the same origin? Response begins: {content?.Trim().Substring(0, Math.Min(200, content.Length)).Replace("\n", " ")}");
        }

        var trimmed = content?.TrimStart();
        if (!string.IsNullOrEmpty(trimmed) && trimmed.StartsWith("<"))
        {
            var fallbackUrl = "https://localhost:7216/TeamRecords";
            var fallbackResp = await TryFallbackCallAsync(fallbackUrl, cancellationToken);
            if (fallbackResp != null)
                return fallbackResp;

            return ApiResponse<List<TeamRecordDto>>.Failure("unexpected_html",
                $"Expected JSON from API but received HTML (status {(int)res.StatusCode}). Response begins: {trimmed.Substring(0, Math.Min(200, trimmed.Length)).Replace("\n", " ")}");
        }

        ApiResponse<List<TeamRecordDto>>? apiResp = null;
        try
        {
            var contentJson = content ?? string.Empty;
            apiResp = JsonSerializer.Deserialize<ApiResponse<List<TeamRecordDto>>>(contentJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException jex)
        {
            // Content was not valid JSON for expected shape
            return ApiResponse<List<TeamRecordDto>>.Failure("invalid_json", jex.Message + ": " + content);
        }

        if (apiResp == null) return ApiResponse<List<TeamRecordDto>>.Failure("invalid_response", content);

        if (res.IsSuccessStatusCode) return apiResp;
        // If server returned a non-success status but also included a structured ApiResponse, return that as failure
        if (!apiResp.Success)
            return apiResp;

        return ApiResponse<List<TeamRecordDto>>.Failure("http_error",
            $"Status {(int)res.StatusCode} {res.ReasonPhrase}");
    }

    private async Task<ApiResponse<List<TeamRecordDto>>?> TryFallbackCallAsync(string urlToTry, CancellationToken ct)
    {
        try
        {
            var r = await _http.GetAsync(urlToTry, ct);
            var body = await r.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(body)) return null;
            var mt = r.Content.Headers.ContentType?.MediaType;
            if (!string.IsNullOrEmpty(mt) && mt.Contains("html", StringComparison.OrdinalIgnoreCase)) return null;
            if (!string.IsNullOrWhiteSpace(body?.TrimStart()) && body.TrimStart().StartsWith("<")) return null;
            var bodyJson = body ?? string.Empty;
            var parsed = JsonSerializer.Deserialize<ApiResponse<List<TeamRecordDto>>>(bodyJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return parsed ?? ApiResponse<List<TeamRecordDto>>.Failure("invalid_response", body);
        }
        catch
        {
            return null;
        }
    }
}