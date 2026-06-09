using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

// For parsing dynamic JSON

namespace DeviceFX.Function
{
    public class GetScanCount
    {
        private readonly ILogger _logger;
        private static readonly HttpClient _httpClient = new HttpClient();

        // Default query counts all customEvents emitted by the MAUI app.
        // Override via APP_INSIGHTS_KQL_QUERY app setting if needed.

        public GetScanCount(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetScanCount>();
        }

        [Function("GetScanCount")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            _logger.LogInformation("Processing request for NFC scan count.");

            // Load from environment variables (app settings)
            string? appId = Environment.GetEnvironmentVariable("APP_INSIGHTS_APP_ID");
            string? apiKey = Environment.GetEnvironmentVariable("APP_INSIGHTS_API_KEY");

            string? kql = Environment.GetEnvironmentVariable("APP_INSIGHTS_KQL_QUERY");
            string query;
            if (string.IsNullOrWhiteSpace(kql))
            {
                _logger.LogInformation("APP_INSIGHTS_KQL_QUERY is null or whitespace. Defaulting to 'customEvents | count'.");
                query = "customEvents | count";
            }
            else
            {
                query = kql;
            }

            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Missing App ID or API Key in configuration.");
                return await CreateBadgeResponseAsync(req, "unconfigured", "lightgrey", isError: true);
            }

            try
            {
                // Build the REST API URL
                string apiUrl = $"https://api.applicationinsights.io/v1/apps/{appId}/query?query={Uri.EscapeDataString(query)}";
                // https://func-devicefx-nfc-badge-f2hwdqhrgzfjbqdc.eastus2-01.azurewebsites.net/api/GetScanCount

                // Set up the request
                using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                request.Headers.Add("x-api-key", apiKey);

                // Execute the request
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("App Insights query failed with {Status}: {Error}", response.StatusCode, errorBody);
                    return await CreateBadgeResponseAsync(req, "error", "red", isError: true);
                }

                // Parse the JSON response
                string jsonResponse = await response.Content.ReadAsStringAsync();
                JsonNode? root = JsonNode.Parse(jsonResponse);
                if (root == null)
                {
                    _logger.LogError("Failed to parse App Insights response JSON.");
                    return await CreateBadgeResponseAsync(req, "error", "red", isError: true);
                }

                JsonNode? tables = root["tables"]?[0];
                JsonNode? rows = tables?["rows"]?[0];
                if (rows == null || rows.AsArray().Count == 0)
                {
                    _logger.LogError("App Insights query returned no result rows. Query: {Query}", query);
                    return await CreateBadgeResponseAsync(req, "error", "red", isError: true);
                }

                long count;
                try
                {
                    count = (long)rows[0]!;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse count value from query result. Query: {Query}", query);
                    return await CreateBadgeResponseAsync(req, "error", "red", isError: true);
                }

                return await CreateBadgeResponseAsync(req, count.ToString(), "blue");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing scan count request. Query: {Query}", query);
                return await CreateBadgeResponseAsync(req, "error", "red", isError: true);
            }
        }

        private async Task<HttpResponseData> CreateBadgeResponseAsync(HttpRequestData req, string message, string color, bool isError = false)
        {
            var badgeData = new
            {
                schemaVersion = 1,
                label = "Total NFC Scans",
                message,
                color,
                isError
            };

            var httpResponse = req.CreateResponse(HttpStatusCode.OK);
            httpResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await httpResponse.WriteStringAsync(JsonSerializer.Serialize(badgeData));
            return httpResponse;
        }
    }
}