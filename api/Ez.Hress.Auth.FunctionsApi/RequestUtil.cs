using Microsoft.AspNetCore.Http;
using System.Net;

namespace Ez.Hress.Auth.FunctionsApi;

internal static class RequestUtil
{
    /// <summary>
    /// Client IP as an IPv4 string (as Hress.Org stored it). Behind Static Web Apps the connection IP
    /// is the front end, so the first X-Forwarded-For entry is used when present.
    /// </summary>
    public static string GetClientIp(HttpRequest req)
    {
        var forwarded = req.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var first = forwarded.Split(',')[0].Trim();
            if (IPEndPoint.TryParse(first, out var endPoint))
                return endPoint.Address.MapToIPv4().ToString();
            if (IPAddress.TryParse(first, out var address))
                return address.MapToIPv4().ToString();
        }

        return req.HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "0.0.0.0";
    }

    /// <summary>
    /// True when the request comes from a page on this same site (or from a non-browser client with no Origin).
    /// Used to stop other sites from posting to endpoints that only the auth app's own pages should call.
    /// </summary>
    public static bool IsSameOriginOrNoOrigin(HttpRequest req)
    {
        var origin = req.Headers.Origin.ToString();
        if (string.IsNullOrEmpty(origin))
            return req.Headers["Sec-Fetch-Site"].ToString() is "" or "same-origin" or "none";

        var forwardedHost = req.Headers["X-Forwarded-Host"].ToString();
        var host = string.IsNullOrEmpty(forwardedHost) ? req.Host.Value : forwardedHost.Split(',')[0].Trim();
        return Uri.TryCreate(origin, UriKind.Absolute, out var originUri)
            && string.Equals(originUri.Authority, host, StringComparison.OrdinalIgnoreCase);
    }
}
