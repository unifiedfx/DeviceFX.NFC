namespace DeviceFX.NfcApp.Abstractions;

public record ActivationResult(string? Code = null, DateTime? Expiry = null, string? Error = null);
