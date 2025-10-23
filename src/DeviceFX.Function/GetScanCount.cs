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

            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(apiKey))
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Missing App ID or API Key in configuration.");
                return errorResponse;
            }

            // KQL query to count all "NFCScan" events (adjust time range or filters as needed, e.g., | where timestamp > ago(30d))
            // string query = "customEvents | where name == 'NFCScan' | count";
            string query = "customEvents | count";

            // Build the REST API URL
            string apiUrl = $"https://api.applicationinsights.io/v1/apps/{appId}/query?query={Uri.EscapeDataString(query)}";
            // https://func-devicefx-nfc-badge-f2hwdqhrgzfjbqdc.eastus2-01.azurewebsites.net/api/GetScanCount

            // Set up the request
            using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Add("x-api-key", apiKey);

            // Execute the request
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            // Parse the JSON response
            string jsonResponse = await response.Content.ReadAsStringAsync();
            JsonNode root = JsonNode.Parse(jsonResponse)!;
            JsonNode tables = root["tables"]![0]!;
            JsonNode rows = tables["rows"]![0]!;
            long totalScans = (long)rows[0]!;

            // Format JSON for Shields.io
            var badgeData = new
            {
                schemaVersion = 1,
                label = "Total NFC Scans",
                message = totalScans.ToString(),
                color = "blue" // Customize color based on value if desired, e.g., totalScans > 1000 ? "green" : "orange"
            };

            // Return JSON response using System.Text.Json
            var httpResponse = req.CreateResponse(HttpStatusCode.OK);
            httpResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await httpResponse.WriteStringAsync(JsonSerializer.Serialize(badgeData));
            return httpResponse;
        }
    }
}