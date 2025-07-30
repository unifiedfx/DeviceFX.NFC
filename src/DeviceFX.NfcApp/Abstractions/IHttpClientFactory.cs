namespace DeviceFX.NfcApp.Abstractions;

public interface IHttpClientFactory
{
    Task<HttpClient> Create();
}