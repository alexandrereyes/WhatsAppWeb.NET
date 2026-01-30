namespace WhatsAppWebLib;

/// <summary>
/// Simple MIME type resolver based on file extension.
/// </summary>
internal static class MimeTypeMap
{
    private static readonly Dictionary<string, string> MimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".bmp"] = "image/bmp",
        [".svg"] = "image/svg+xml",
        [".ico"] = "image/x-icon",
        [".tiff"] = "image/tiff",
        [".tif"] = "image/tiff",
        [".avif"] = "image/avif",
        [".heic"] = "image/heic",
        [".heif"] = "image/heif",

        // Audio
        [".mp3"] = "audio/mpeg",
        [".wav"] = "audio/wav",
        [".ogg"] = "audio/ogg",
        [".oga"] = "audio/ogg",
        [".opus"] = "audio/opus",
        [".m4a"] = "audio/mp4",
        [".aac"] = "audio/aac",
        [".flac"] = "audio/flac",
        [".wma"] = "audio/x-ms-wma",

        // Video
        [".mp4"] = "video/mp4",
        [".webm"] = "video/webm",
        [".avi"] = "video/x-msvideo",
        [".mov"] = "video/quicktime",
        [".mkv"] = "video/x-matroska",
        [".3gp"] = "video/3gpp",
        [".wmv"] = "video/x-ms-wmv",
        [".flv"] = "video/x-flv",

        // Documents
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".ppt"] = "application/vnd.ms-powerpoint",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".txt"] = "text/plain",
        [".csv"] = "text/csv",
        [".rtf"] = "application/rtf",
        [".zip"] = "application/zip",
        [".rar"] = "application/vnd.rar",
        [".7z"] = "application/x-7z-compressed",
        [".gz"] = "application/gzip",
        [".tar"] = "application/x-tar",
        [".json"] = "application/json",
        [".xml"] = "application/xml",

        // Other
        [".vcf"] = "text/x-vcard",
        [".apk"] = "application/vnd.android.package-archive"
    };

    public static string GetMimeType(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return MimeTypes.GetValueOrDefault(ext, "application/octet-stream");
    }
}
