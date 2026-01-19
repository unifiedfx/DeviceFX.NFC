namespace DeviceFX.Proxy.CDA;

public class AttestResult
{
    public bool IsValid { get; set; }
    public string? Error { get; set; }
    public byte[]? PublicKey { get; set; }
    public string? KeyId { get; set; }
}