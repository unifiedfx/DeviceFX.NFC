using System.Net.Sockets;
using System.Text;
using SkiaSharp;

namespace DeviceFX.NfcApp.Services;

public class ZebraPrinterClient(string host, int port = 9100, TimeSpan? timeout = null)
{
    public async Task<string?> SendCommandAsync(string command, bool expectResponse,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,
                new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(10)).Token);
            using var client = new TcpClient();
            client.ReceiveTimeout = 5000;
            await client.ConnectAsync(host, port, cts.Token);
            await using var stream = client.GetStream();

            // Send the command
            var commandBytes = Encoding.ASCII.GetBytes(command);
            await stream.WriteAsync(commandBytes, cts.Token);
            await stream.FlushAsync(cts.Token);

            if (!expectResponse) return null;

            // Read the response
            var buffer = new byte[1024];
            var responseBytes = new List<byte>();
            int bytesRead;

            try
            {
                while (true)
                {
                    bytesRead = await stream.ReadAsync(buffer, cts.Token);
                    Console.WriteLine($"bytesRead: {bytesRead}");
                    if (bytesRead == 0) break;
                    responseBytes.AddRange(buffer.AsSpan(0, bytesRead).ToArray());
                    if (buffer.AsSpan(0, bytesRead).Contains((byte) 0x03)) break;
                }
            }
            catch (IOException ex) when (ex.InnerException is SocketException {SocketErrorCode: SocketError.TimedOut})
            {
                Console.WriteLine($"IOException: {ex}");
                // Expected timeout when no more data is available
            }

            if (responseBytes.Count == 0) return null;

            // Parse the response to extract content between STX (0x02) and ETX (0x03)
            var responses = new List<string>();
            int index = 0;
            while (index < responseBytes.Count)
            {
                // Find STX
                while (index < responseBytes.Count && responseBytes[index] != 0x02) index++;
                if (index >= responseBytes.Count) break;
                int start = index + 1;
                index = start;

                // Find ETX
                while (index < responseBytes.Count && responseBytes[index] != 0x03) index++;
                if (index >= responseBytes.Count) break;

                int length = index - start;
                var data = responseBytes.GetRange(start, length);
                responses.Add(Encoding.ASCII.GetString(data.ToArray()));

                index++; // Skip ETX

                // Skip CR (0x0D) and LF (0x0A) if present
                if (index < responseBytes.Count && responseBytes[index] == 0x0D) index++;
                if (index < responseBytes.Count && responseBytes[index] == 0x0A) index++;
            }

            if (responses.Count == 0) return null;

            // Join the extracted responses with newlines
            return string.Join("\n", responses);
        }
        catch
        {
            return null;
        }
    }

    public static class ImageHelper
    {
        public static int GetImageHeight(Stream pngStream,
            int targetWidth = 300,
            int? targetHeight = null)
        {
            using var original = SKBitmap.Decode(pngStream)
                                 ?? throw new Exception("Failed to decode PNG");

            int width = targetWidth;
            int height = targetHeight ?? (int) (original.Height * (width / (double) original.Width));

            using var resized = original.Resize(new SKImageInfo(width, height), SKFilterQuality.High);
            return resized.Height;
        }

        /// <summary>
        /// Loads PNG → resizes → converts to 1bpp monochrome → returns ^GFA ZPL string with Z64 compression and CRC
        /// Position: ^FO x,y (in dots)
        /// Works natively on macOS Apple Silicon (M3 etc.)
        /// </summary>
        public static string GetZplGfaFromPng(
            Stream pngStream,
            int targetWidth = 300, // desired width in dots (~1.5" at 203 dpi)
            int? targetHeight = null, // null = preserve aspect
            float threshold = 0.5f) // 0..1 brightness cutoff (lower = darker/black)
        {
            try
            {
                using var original = SKBitmap.Decode(pngStream)
                                     ?? throw new Exception("Failed to decode PNG");

                int width = targetWidth;
                int height = targetHeight ?? (int) (original.Height * (width / (double) original.Width));

                using var resized = original.Resize(new SKImageInfo(width, height), SKFilterQuality.High);

                var bytesPerRow = (width + 7) / 8;
                var totalBytes = bytesPerRow * height;
                byte[] monoBytes = new byte[totalBytes];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var pixel = resized.GetPixel(x, y);
                        float brightness = (0.299f * pixel.Red + 0.587f * pixel.Green + 0.114f * pixel.Blue) / 3f;

                        if (brightness < threshold && pixel.Alpha > 128)
                        {
                            int byteIndex = y * bytesPerRow + (x / 8);
                            int bitIndex = 7 - (x % 8);
                            monoBytes[byteIndex] |= (byte) (1 << bitIndex);
                        }
                    }
                }

                var data = ToAsciiHexZ64(monoBytes);
                string gfa = $"^GFA,{monoBytes.Length},{monoBytes.Length},{bytesPerRow},{data}^FS";
                return gfa;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Image processing failed: {ex.Message}");
                return string.Empty;
            }
        }

        private static string ToAsciiHexZ64(byte[] data)
        {
            var sb = new StringBuilder();
            foreach (var b in data)
            {
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }
    }
}