namespace WhatsAppWebLib;

public enum WaState
{
    Conflict,
    Connected,
    DeprecatedVersion,
    Opening,
    Pairing,
    ProxyBlock,
    SmbTosBlock,
    Timeout,
    TosBlock,
    Unlaunched,
    Unpaired,
    UnpairedIdle
}

public enum DisconnectReason
{
    Logout,
    MaxQrRetries,
    Conflict,
    DeprecatedVersion,
    ProxyBlock,
    SmbTosBlock,
    Timeout,
    TosBlock
}

public class ClientOptions
{
    public string SessionPath { get; init; } = "./.wwebjs_auth/session";
    public string? PairWithPhoneNumber { get; init; }
    public bool ShowPairingNotification { get; set; } = true;
    public int PairingCodeIntervalMs { get; set; } = 180000; // 3 minutes

    /// <summary>
    /// Path to the ffmpeg binary used for media preprocessing (voice waveform, video stickers).
    /// When null, defaults to "ffmpeg" (expects ffmpeg on PATH).
    /// </summary>
    public string? FfmpegPath { get; init; }
}
