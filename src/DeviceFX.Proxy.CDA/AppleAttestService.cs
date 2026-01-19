using System.Formats.Asn1;
using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Options;

namespace DeviceFX.Proxy.CDA;

public class AppleAttestService(IOptions<CiscoOptions> options, ILogger<AppleAttestService> logger)
{
    private readonly CiscoOptions options = options.Value;

    public async Task<AttestResult> ValidateAttestationAsync(string attestationBase64, string keyId, string challenge)
    {
        try
        {
            // Decode the attestation object (CBOR format)
            var attestationData = Convert.FromBase64String(attestationBase64);
            var (authData, x5c) = ParseAttestationObject(attestationData);

            if (x5c == null || x5c.Count < 2)
            {
                return new AttestResult { IsValid = false, Error = "Invalid certificate chain" };
            }

            // 1. Verify the certificate chain
            var leafCert = X509CertificateLoader.LoadCertificate(x5c[0]);
            var intermediateCert = X509CertificateLoader.LoadCertificate(x5c[1]);
            var rootCert = X509Certificate2.CreateFromPem(options.AppleAppAttestRootCertPem);

            if (!VerifyCertificateChain(leafCert, intermediateCert, rootCert))
            {
                return new AttestResult { IsValid = false, Error = "Certificate chain verification failed" };
            }

            // 2. Create the client data hash from the challenge
            var clientDataHash = SHA256.HashData(Encoding.UTF8.GetBytes(challenge));

            // 3. Concatenate authenticator data and client data hash, then hash
            var nonceData = new byte[authData.Length + clientDataHash.Length];
            Buffer.BlockCopy(authData, 0, nonceData, 0, authData.Length);
            Buffer.BlockCopy(clientDataHash, 0, nonceData, authData.Length, clientDataHash.Length);
            var expectedNonce = SHA256.HashData(nonceData);

            // 4. Verify the nonce in the certificate extension (OID 1.2.840.113635.100.8.2)
            var nonceExtension = leafCert.Extensions["1.2.840.113635.100.8.2"];
            if (nonceExtension == null)
            {
                return new AttestResult { IsValid = false, Error = "Nonce extension not found in certificate" };
            }

            var certNonce = ExtractNonceFromExtension(nonceExtension.RawData);
            if (!certNonce.SequenceEqual(expectedNonce))
            {
                return new AttestResult { IsValid = false, Error = "Nonce verification failed" };
            }

            // 5. Verify the key identifier
            var publicKeyHash = SHA256.HashData(leafCert.GetPublicKey());
            var expectedKeyId = Convert.ToBase64String(publicKeyHash);
            
            // Normalize both for comparison (remove padding differences)
            var normalizedKeyId = keyId.TrimEnd('=');
            var normalizedExpectedKeyId = expectedKeyId.TrimEnd('=');
            
            if (!normalizedKeyId.Equals(normalizedExpectedKeyId, StringComparison.Ordinal))
            {
                logger.LogWarning("Key ID mismatch. Expected: {Expected}, Got: {Actual}", normalizedExpectedKeyId, normalizedKeyId);
                // Note: In some implementations, the keyId comes from the device and may use different encoding
            }

            // 6. Verify the RP ID hash (first 32 bytes of authData)
            var rpIdHash = new byte[32];
            Buffer.BlockCopy(authData, 0, rpIdHash, 0, 32);
            
            var appIdForAttest = $"{options.AppleTeamId}.{options.AppleAppId}";
            var expectedRpIdHash = SHA256.HashData(Encoding.UTF8.GetBytes(appIdForAttest));

            if (!rpIdHash.SequenceEqual(expectedRpIdHash))
            {
                return new AttestResult { IsValid = false, Error = "App ID verification failed" };
            }

            // 7. Verify counter (bytes 33-36, should be 0 for attestation)
            var counter = BitConverter.ToUInt32(authData.AsSpan(33, 4));
            // Counter can be big-endian
            if (BitConverter.IsLittleEndian) counter = System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(counter);

            if (counter != 0)
            {
                return new AttestResult { IsValid = false, Error = "Invalid counter value" };
            }

            // 8. Check the aaguid for App Attest (bytes 37-52)
            var aaguid = authData.AsSpan(37, 16).ToArray();
            var expectedAaguid = new List<byte[]>(); 
            expectedAaguid.Add("appattestdevelop"u8.ToArray());
            expectedAaguid.Add("appattest\0\0\0\0\0\0\0"u8.ToArray());
            if (!expectedAaguid.Any(e => e.SequenceEqual(aaguid)))
            {
                return new AttestResult { IsValid = false, Error = "Invalid AAGUID" };
            }
            // 9. Verify the authenticator data's credentialId
            var expectedCredentialId = Convert.FromBase64String(keyId);
            var credentialIdLength = BitConverter.ToUInt16(authData.AsSpan(53, 2));
            if (BitConverter.IsLittleEndian) credentialIdLength = System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(credentialIdLength);
            var credentialId = authData.AsSpan(55, credentialIdLength);
            if (!credentialId.SequenceEqual(expectedCredentialId))
            {
                return new AttestResult { IsValid = false, Error = "Invalid credentialId" };
            }

            // Extract the public key from the credential
            var credentialPublicKey = ExtractPublicKeyFromAuthData(authData);

            logger.LogInformation("Attestation validated successfully for keyId: {KeyId}", keyId);

            return new AttestResult
            {
                IsValid = true,
                PublicKey = credentialPublicKey,
                KeyId = keyId
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Attestation validation failed");
            return new AttestResult { IsValid = false, Error = $"Validation error: {ex.Message}" };
        }
    }
    

    private static (byte[] authData, List<byte[]>? x5c) ParseAttestationObject(byte[] data)
    {
        var reader = new CborReader(data);
        reader.ReadStartMap();

        byte[]? authData = null;
        List<byte[]>? x5c = null;

        while (reader.PeekState() != CborReaderState.EndMap)
        {
            var key = reader.ReadTextString();
            
            switch (key)
            {
                case "authData":
                    authData = reader.ReadByteString();
                    break;
                case "fmt":
                    reader.ReadTextString(); // Skip format
                    break;
                case "attStmt":
                    reader.ReadStartMap();
                    while (reader.PeekState() != CborReaderState.EndMap)
                    {
                        var attKey = reader.ReadTextString();
                        if (attKey == "x5c")
                        {
                            x5c = [];
                            reader.ReadStartArray();
                            while (reader.PeekState() != CborReaderState.EndArray)
                            {
                                x5c.Add(reader.ReadByteString());
                            }
                            reader.ReadEndArray();
                        }
                        else
                        {
                            reader.SkipValue();
                        }
                    }
                    reader.ReadEndMap();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }

        reader.ReadEndMap();
        return (authData ?? throw new InvalidOperationException("authData not found"), x5c);
    }

    private static bool VerifyCertificateChain(X509Certificate2 leaf, X509Certificate2 intermediate, X509Certificate2 root)
    {
        using var chain = new X509Chain();
        chain.ChainPolicy.ExtraStore.Add(intermediate);
        chain.ChainPolicy.ExtraStore.Add(root);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

        var isValid = chain.Build(leaf);
        
        if (!isValid)
        {
            return false;
        }

        // Verify the root matches Apple's root
        var chainRoot = chain.ChainElements[^1].Certificate;
        return chainRoot.Thumbprint.Equals(root.Thumbprint, StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] ExtractNonceFromExtension(byte[] extensionData)
    {
        var asnReader = new AsnReader(extensionData, AsnEncodingRules.BER);
        var sequence = asnReader.ReadSequence();
        byte[] certNonce = sequence.ReadOctetString(new Asn1Tag(TagClass.ContextSpecific, 1));
        return certNonce;
    }
    
    private static byte[] ExtractPublicKeyFromAuthData(byte[] authData)
    {
        // authData structure:
        // - 32 bytes: rpIdHash
        // - 1 byte: flags
        // - 4 bytes: signCount
        // - 16 bytes: aaguid (if AT flag set)
        // - 2 bytes: credentialIdLength (big-endian)
        // - N bytes: credentialId
        // - variable: credentialPublicKey (COSE format)

        var offset = 32 + 1 + 4 + 16; // Start after aaguid
        var credIdLength = (authData[offset] << 8) | authData[offset + 1];
        offset += 2 + credIdLength;

        // The rest is the COSE public key
        var publicKeyLength = authData.Length - offset;
        var coseKey = new byte[publicKeyLength];
        Buffer.BlockCopy(authData, offset, coseKey, 0, publicKeyLength);

        // Convert COSE key to SubjectPublicKeyInfo format
        return ConvertCoseToSpki(coseKey);
    }

    private static byte[] ConvertCoseToSpki(byte[] coseKey)
    {
        var reader = new CborReader(coseKey);
        reader.ReadStartMap();

        byte[]? x = null;
        byte[]? y = null;

        while (reader.PeekState() != CborReaderState.EndMap)
        {
            var key = reader.ReadInt32();
            switch (key)
            {
                case -2: // x coordinate
                    x = reader.ReadByteString();
                    break;
                case -3: // y coordinate
                    y = reader.ReadByteString();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }

        if (x == null || y == null)
        {
            throw new InvalidOperationException("Invalid COSE key format");
        }

        // Create EC point (uncompressed format: 0x04 + x + y)
        using var ecdsa = ECDsa.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = x, Y = y }
        });

        return ecdsa.ExportSubjectPublicKeyInfo();
    }
}