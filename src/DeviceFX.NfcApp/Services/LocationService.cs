using System.Net.Http.Headers;
using System.Net.Http.Json;
using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Model;

namespace DeviceFX.NfcApp.Services;

// TODO: Need to attribute 'OpenStreetMap' for the use of their API
// https://osmfoundation.org/wiki/Licence/Attribution_Guidelines#Attribution_text
public class LocationService(IHttpClientFactory clientFactory) : ILocationService
{
    public async Task<GeocodeResponse?> GetLocationAsync(CancellationToken cancellationToken = default)
    {
        PermissionStatus status = PermissionStatus.Unknown;
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }
        });
        if (status != PermissionStatus.Granted) return null;
        var location = await Geolocation.GetLocationAsync();
        if (location == null) return null;
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(10_000);
        var client = await clientFactory.Create();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15");
        client.DefaultRequestHeaders.Referrer = new("https://unifiedfx.com");
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var url = $"https://nominatim.openstreetmap.org/reverse?lat={location.Latitude}&lon={location.Longitude}&format=json";
        
        var response = new GeocodeResponse(lat: location.Latitude.ToString(), lon: location.Longitude.ToString());
        try
        {
            var result = await client.GetAsync(url, cts.Token);
            if(result.IsSuccessStatusCode) response = await result.Content.ReadFromJsonAsync<GeocodeResponse>(cancellationToken: cts.Token);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return response;
    }
}