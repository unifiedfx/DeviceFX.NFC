using System.Net.Http.Json;
using System.Text.Json;

namespace DeviceFX.NfcApp.Helpers;

internal static class CdaSignHttp
{
    public static async Task<byte[]> ReadSignedDataAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(await ReadErrorAsync(response, cancellationToken));

        var result = await response.Content.ReadFromJsonAsync<CdaSignResponse>(cancellationToken: cancellationToken);
        if (result?.IsSuccess == true)
            return result.SignedDataBytes;

        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(result?.ErrorMessage) ? "CDA signing failed" : result.ErrorMessage);
    }

    internal static async Task<string> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(text))
            {
                var trimmed = text.Trim();
                if (trimmed.StartsWith('"'))
                {
                    var parsed = JsonSerializer.Deserialize<string>(trimmed);
                    if (!string.IsNullOrWhiteSpace(parsed))
                        return parsed;
                }

                return trimmed;
            }
        }
        catch (Exception)
        {
            // fall through
        }

        return string.IsNullOrWhiteSpace(response.ReasonPhrase)
            ? $"HTTP {(int)response.StatusCode}"
            : response.ReasonPhrase;
    }

    internal sealed record CdaSignResponse(string Status, string StatusMessage, string SignedData)
    {
        public bool IsSuccess => Status == "SUCCESS";
        public string ErrorMessage => $"{Status}: {StatusMessage}";
        public byte[] SignedDataBytes => Convert.FromBase64String(SignedData);
    }
}
