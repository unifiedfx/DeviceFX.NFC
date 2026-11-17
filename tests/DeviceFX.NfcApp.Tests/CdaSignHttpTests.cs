using System.Net;
using System.Net.Http;
using System.Text;
using DeviceFX.NfcApp.Helpers;
using Xunit;

namespace DeviceFX.NfcApp.Tests;

public class CdaSignHttpTests
{
    [Fact]
    public async Task ReadSignedDataAsync_ThrowsWithApiMessage_On502JsonString()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("\"Failed to sign data with Cisco CDA\"", Encoding.UTF8, "application/json")
        };

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            CdaSignHttp.ReadSignedDataAsync(response));

        Assert.Equal("Failed to sign data with Cisco CDA", ex.Message);
    }

    [Fact]
    public async Task ReadSignedDataAsync_ReturnsBytes_OnSuccess()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"status":"SUCCESS","statusMessage":"ok","signedData":"YWI="}""",
                Encoding.UTF8,
                "application/json")
        };

        var bytes = await CdaSignHttp.ReadSignedDataAsync(response);

        Assert.Equal("ab"u8.ToArray(), bytes);
    }

    [Fact]
    public async Task ReadSignedDataAsync_Throws_WhenStatusIsNotSuccess()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"status":"FAILURE","statusMessage":"denied","signedData":""}""",
                Encoding.UTF8,
                "application/json")
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CdaSignHttp.ReadSignedDataAsync(response));

        Assert.Equal("FAILURE: denied", ex.Message);
    }
}
