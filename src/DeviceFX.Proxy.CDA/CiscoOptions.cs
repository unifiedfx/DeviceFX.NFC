using System.ComponentModel.DataAnnotations;

namespace DeviceFX.Proxy.CDA;

public class CiscoOptions
{
    [Required]
    public required string ClientId { get; set; }
    [Required]
    public required string ClientSecret { get; set; }
}