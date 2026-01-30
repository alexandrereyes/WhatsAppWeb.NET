using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace WhatsAppWebLib.Message;

/// <summary>
/// Handles server-side media preprocessing before sending to WhatsApp Web.
/// Replicates the Node.js-side preprocessing from the upstream whatsapp-web.js:
/// - Waveform generation for voice messages (PTT) via ffmpeg
/// - Video-to-WebP conversion for animated stickers via ffmpeg
/// - EXIF metadata injection for sticker packs
/// </summary>
/// <seealso href="https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Util.js"/>
internal class MediaPreprocessor(ILogger logger, string ffmpegPath = "ffmpeg")
{
    // @wwebjs-source WWebJS.generateWaveform -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Injected/Utils.js#L1145-L1182
    /// <summary>
    /// Generates a 64-sample waveform from an audio file using ffmpeg.
    /// Replicates the browser-side generateWaveform that uses Web Audio API,
    /// which fails in headless Chromium environments.
    /// </summary>
    /// <returns>Base64-encoded 64-byte waveform array (values 0-100), or null on failure.</returns>
    public async Task<byte[]?> GenerateWaveformAsync(byte[] audioData, CancellationToken ct = default)
    {
        var tempInput = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.audio");
        var tempOutput = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.raw");

        try
        {
            await File.WriteAllBytesAsync(tempInput, audioData, ct);

            // Extract raw PCM (mono, float32, 16kHz) using ffmpeg
            var result = await RunFfmpegAsync(
                $"-i \"{tempInput}\" -ac 1 -ar 16000 -f f32le -acodec pcm_f32le \"{tempOutput}\"",
                ct);

            if (!result.Success)
            {
                logger.LogWarning("Waveform generation failed: {Error}", result.Error);
                return null;
            }

            if (!File.Exists(tempOutput)) return null;

            var pcmBytes = await File.ReadAllBytesAsync(tempOutput, ct);
            return ComputeWaveform(pcmBytes);
        }
        finally
        {
            TryDelete(tempInput);
            TryDelete(tempOutput);
        }
    }

    /// <summary>
    /// Converts any audio format to OGG/Opus, which is required by WhatsApp for PTT (voice) messages.
    /// WhatsApp Web rejects non-Opus audio when sent as voice messages (InvalidMediaCheckRepairFailedType).
    /// Output: OGG container, Opus codec, 48kHz, mono.
    /// </summary>
    public async Task<MessageMedia?> ConvertToOggOpusAsync(MessageMedia media, CancellationToken ct = default)
    {
        var audioBytes = Convert.FromBase64String(media.Data);
        var ext = media.MimeType.Split('/').LastOrDefault()?.Split(';')[0].Trim() ?? "audio";
        var tempInput = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.{ext}");
        var tempOutput = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ogg");

        try
        {
            await File.WriteAllBytesAsync(tempInput, audioBytes, ct);

            var result = await RunFfmpegAsync(
                $"-i \"{tempInput}\" -ac 1 -ar 48000 -c:a libopus -b:a 128k \"{tempOutput}\"",
                ct);

            if (!result.Success)
            {
                logger.LogWarning("Audio to OGG/Opus conversion failed: {Error}", result.Error);
                return null;
            }

            if (!File.Exists(tempOutput)) return null;

            var oggBytes = await File.ReadAllBytesAsync(tempOutput, ct);

            return new MessageMedia
            {
                MimeType = "audio/ogg; codecs=opus",
                Data = Convert.ToBase64String(oggBytes),
                FileName = Path.ChangeExtension(media.FileName ?? "audio.ogg", ".ogg"),
                FileSize = oggBytes.Length
            };
        }
        finally
        {
            TryDelete(tempInput);
            TryDelete(tempOutput);
        }
    }

    // @wwebjs-source Util.formatVideoToWebpSticker -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Util.js#L137-L190
    /// <summary>
    /// Converts a video to an animated WebP sticker using ffmpeg.
    /// Output: 512x512, max 5 seconds, 10fps, no audio, libwebp codec.
    /// </summary>
    public async Task<MessageMedia?> ConvertVideoToWebpStickerAsync(
        MessageMedia media,
        CancellationToken ct = default)
    {
        if (!media.MimeType.Contains("video", StringComparison.OrdinalIgnoreCase))
            return null;

        var videoBytes = Convert.FromBase64String(media.Data);
        var ext = media.MimeType.Split('/').LastOrDefault()?.Split(';')[0].Trim() ?? "mp4";
        var tempInput = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.{ext}");
        var tempOutput = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.webp");

        try
        {
            await File.WriteAllBytesAsync(tempInput, videoBytes, ct);

            var vf = "scale='iw*min(300/iw,300/ih)':'ih*min(300/iw,300/ih)',"
                     + "format=rgba,"
                     + "pad=300:300:'(300-iw)/2':'(300-ih)/2':'#00000000',"
                     + "setsar=1,fps=10";

            var result = await RunFfmpegAsync(
                $"-i \"{tempInput}\" "
                + $"-vcodec libwebp "
                + $"-vf \"{vf}\" "
                + "-loop 0 "
                + "-ss 00:00:00.0 -t 00:00:05.0 "
                + "-preset default -an -vsync 0 "
                + $"-s 512:512 \"{tempOutput}\"",
                ct);

            if (!result.Success)
            {
                logger.LogWarning("Video to WebP sticker conversion failed: {Error}", result.Error);
                return null;
            }

            if (!File.Exists(tempOutput)) return null;

            var webpBytes = await File.ReadAllBytesAsync(tempOutput, ct);

            return new MessageMedia
            {
                MimeType = "image/webp",
                Data = Convert.ToBase64String(webpBytes),
                FileName = media.FileName,
                FileSize = webpBytes.Length
            };
        }
        finally
        {
            TryDelete(tempInput);
            TryDelete(tempOutput);
        }
    }

    // @wwebjs-source Util.formatToWebpSticker EXIF injection -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/util/Util.js#L80-L135
    /// <summary>
    /// Injects sticker pack EXIF metadata into a WebP file.
    /// The EXIF format uses TIFF Little-Endian with a custom tag 0x5741 ("AW" = Android WhatsApp)
    /// containing JSON with sticker pack info.
    /// </summary>
    public static byte[] InjectStickerExif(byte[] webpData, string? name, string? author, List<string>? categories)
    {
        if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(author))
            return webpData;

        var jsonObj = new Dictionary<string, object?>
        {
            ["sticker-pack-id"] = GenerateRandomHash(32),
            ["sticker-pack-name"] = name ?? "",
            ["sticker-pack-publisher"] = author ?? "",
            ["emojis"] = categories ?? new List<string> { "" }
        };
        var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(jsonObj));

        // TIFF Little-Endian EXIF header with tag 0x5741 (type UNDEFINED)
        var exifHeader = new byte[]
        {
            0x49, 0x49,             // "II" = Little-Endian
            0x2A, 0x00,             // TIFF magic (42)
            0x08, 0x00, 0x00, 0x00, // Offset to first IFD (8)
            0x01, 0x00,             // Number of IFD entries (1)
            0x41, 0x57,             // Tag 0x5741 ("AW" reversed LE)
            0x07, 0x00,             // Data type 7 (UNDEFINED)
            0x00, 0x00, 0x00, 0x00, // Data length (will be overwritten)
            0x16, 0x00, 0x00, 0x00  // Data offset (22)
        };

        // Write JSON length at offset 14 (4 bytes LE)
        BitConverter.GetBytes(jsonBytes.Length).CopyTo(exifHeader, 14);

        var exifChunk = new byte[exifHeader.Length + jsonBytes.Length];
        exifHeader.CopyTo(exifChunk, 0);
        jsonBytes.CopyTo(exifChunk, exifHeader.Length);

        return InjectExifIntoWebp(webpData, exifChunk);
    }

    /// <summary>
    /// Injects an EXIF chunk into a WebP file by inserting an EXIF RIFF chunk.
    /// </summary>
    private static byte[] InjectExifIntoWebp(byte[] webpData, byte[] exifData)
    {
        // WebP format: RIFF....WEBP[chunks...]
        // We need to insert an EXIF chunk
        var exifChunkId = Encoding.ASCII.GetBytes("EXIF");
        var exifChunkSize = BitConverter.GetBytes(exifData.Length);

        // Pad to even size if needed
        var padding = exifData.Length % 2 != 0 ? 1 : 0;

        using var output = new MemoryStream();
        // Write original RIFF header (first 12 bytes: "RIFF" + size + "WEBP")
        output.Write(webpData, 0, 12);

        // Write remaining original chunks
        output.Write(webpData, 12, webpData.Length - 12);

        // Append EXIF chunk
        output.Write(exifChunkId);
        output.Write(exifChunkSize);
        output.Write(exifData);
        if (padding > 0) output.WriteByte(0);

        var result = output.ToArray();

        // Update RIFF file size (bytes 4-7, LE, = total - 8)
        BitConverter.GetBytes(result.Length - 8).CopyTo(result, 4);

        return result;
    }

    /// <summary>
    /// Computes a 64-sample waveform from raw PCM float32 data.
    /// Replicates the upstream algorithm from WWebJS.generateWaveform.
    /// </summary>
    private static byte[] ComputeWaveform(byte[] pcmBytes)
    {
        const int samples = 64;
        var sampleCount = pcmBytes.Length / 4; // float32 = 4 bytes

        if (sampleCount == 0) return new byte[samples];

        var blockSize = sampleCount / samples;
        if (blockSize == 0) blockSize = 1;

        var filteredData = new float[samples];

        for (var i = 0; i < samples; i++)
        {
            var blockStart = blockSize * i;
            var sum = 0f;
            var count = 0;

            for (var j = 0; j < blockSize && (blockStart + j) < sampleCount; j++)
            {
                var offset = (blockStart + j) * 4;
                if (offset + 4 > pcmBytes.Length) break;
                var value = BitConverter.ToSingle(pcmBytes, offset);
                sum += Math.Abs(value);
                count++;
            }

            filteredData[i] = count > 0 ? sum / count : 0;
        }

        // Normalize
        var max = filteredData.Max();
        var multiplier = max > 0 ? 1f / max : 0;

        var waveform = new byte[samples];
        for (var i = 0; i < samples; i++)
            waveform[i] = (byte)Math.Min(100, (int)Math.Floor(100 * filteredData[i] * multiplier));

        return waveform;
    }

    private async Task<FfmpegResult> RunFfmpegAsync(string arguments, CancellationToken ct)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-y -hide_banner -loglevel error {arguments}",
                RedirectStandardOutput = false,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            var stderr = await process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            return new FfmpegResult(process.ExitCode == 0, stderr);
        }
        catch (Exception ex)
        {
            return new FfmpegResult(false, ex.Message);
        }
    }

    private static string GenerateRandomHash(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var bytes = RandomNumberGenerator.GetBytes(length);
        var sb = new StringBuilder(length);
        foreach (var b in bytes)
            sb.Append(chars[b % chars.Length]);
        return sb.ToString();
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* ignore cleanup errors */ }
    }

    private record FfmpegResult(bool Success, string Error);
}
