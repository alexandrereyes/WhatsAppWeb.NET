namespace WhatsAppWebLib.Message;

/// <summary>
/// Represents media (image, audio, video, document, sticker) to be sent or received via WhatsApp.
/// Port of the upstream MessageMedia structure.
/// </summary>
/// <seealso href="https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/MessageMedia.js"/>
public class MessageMedia
{
    /// <summary>MIME type of the media (e.g. "image/png", "audio/ogg").</summary>
    public required string MimeType { get; init; }

    /// <summary>Base64-encoded media data.</summary>
    public required string Data { get; init; }

    /// <summary>Optional file name.</summary>
    public string? FileName { get; init; }

    /// <summary>Optional file size in bytes.</summary>
    public long? FileSize { get; init; }

    // @wwebjs-source MessageMedia.fromFilePath -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/MessageMedia.js#L36-L50
    /// <summary>
    /// Creates a MessageMedia from a local file path.
    /// </summary>
    public static MessageMedia FromFilePath(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Media file not found.", filePath);

        var bytes = File.ReadAllBytes(filePath);
        var mimeType = MimeTypeMap.GetMimeType(filePath);
        var fileName = Path.GetFileName(filePath);

        return new MessageMedia
        {
            MimeType = mimeType,
            Data = Convert.ToBase64String(bytes),
            FileName = fileName,
            FileSize = bytes.Length
        };
    }

    // @wwebjs-source MessageMedia.fromUrl -> https://github.com/pedroslopez/whatsapp-web.js/blob/main/src/structures/MessageMedia.js#L52-L119
    /// <summary>
    /// Creates a MessageMedia by downloading from a URL.
    /// </summary>
    /// <param name="url">The URL to download the media from.</param>
    /// <param name="fileName">Optional file name override.</param>
    /// <param name="httpClient">Optional HttpClient to use. A temporary one is created if not provided.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task<MessageMedia> FromUrlAsync(
        string url,
        string? fileName = null,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        var ownsClient = httpClient is null;
        httpClient ??= new HttpClient();

        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

            // Try to extract filename from Content-Disposition header
            fileName ??= response.Content.Headers.ContentDisposition?.FileNameStar
                         ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                         ?? ExtractFileNameFromUrl(url);

            var fileSize = response.Content.Headers.ContentLength ?? bytes.Length;

            return new MessageMedia
            {
                MimeType = contentType,
                Data = Convert.ToBase64String(bytes),
                FileName = fileName,
                FileSize = fileSize
            };
        }
        finally
        {
            if (ownsClient)
                httpClient.Dispose();
        }
    }

    /// <summary>
    /// Creates a MessageMedia from raw bytes.
    /// </summary>
    public static MessageMedia FromBytes(byte[] bytes, string mimeType, string? fileName = null)
    {
        return new MessageMedia
        {
            MimeType = mimeType,
            Data = Convert.ToBase64String(bytes),
            FileName = fileName,
            FileSize = bytes.Length
        };
    }

    /// <summary>
    /// Creates a MessageMedia from a Stream.
    /// </summary>
    public static async Task<MessageMedia> FromStreamAsync(
        Stream stream,
        string mimeType,
        string? fileName = null,
        CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();

        return new MessageMedia
        {
            MimeType = mimeType,
            Data = Convert.ToBase64String(bytes),
            FileName = fileName,
            FileSize = bytes.Length
        };
    }

    private static string? ExtractFileNameFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;
            var lastSegment = path.Split('/').LastOrDefault();
            return string.IsNullOrWhiteSpace(lastSegment) ? null : Uri.UnescapeDataString(lastSegment);
        }
        catch
        {
            return null;
        }
    }
}
