using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using DeviceFX.NfcApp.Abstractions;
using Microsoft.Extensions.Logging;

namespace DeviceFX.NfcApp.Services;

public class HttpClientFactory(ILogger<HttpClientFactory> logger) : IHttpClientFactory
{
    public async Task<HttpClient> Create()
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = ServerCertificateValidationCallback;
        var client = new HttpClient(handler);
        return client;
        bool ServerCertificateValidationCallback(HttpRequestMessage message, X509Certificate2? cert, X509Chain? chain, SslPolicyErrors errors)
        {
            //TODO: validate the HTTPS Certificate from the identity server to ensure the identity server is trusted
            // cert.GetCertHashString()
            return true;
            // return errors == SslPolicyErrors.None;
        }
    }
}