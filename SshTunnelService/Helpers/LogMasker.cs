namespace SshTunnelService.Helpers;

public static class LogMasker
{
    /// <summary>
    /// Masks a hostname, showing only the first 3 characters.
    /// e.g. "myserver.example.com" -> "mys***.***"
    /// </summary>
    public static string MaskHost(string host)
    {
        if (string.IsNullOrEmpty(host)) return "***";
        var visible = Math.Min(3, host.Length);
        return $"{host[..visible]}***.***";
    }

    /// <summary>
    /// Masks a port number, showing only the first digit.
    /// e.g. 1957 -> "1***"
    /// </summary>
    public static string MaskPort(int port)
    {
        var portStr = port.ToString();
        return portStr.Length <= 1 ? "***" : $"{portStr[0]}***";
    }

    /// <summary>
    /// Returns a masked "host:port" string for safe logging.
    /// </summary>
    public static string MaskEndpoint(string host, int port)
        => $"{MaskHost(host)}:{MaskPort(port)}";
}
