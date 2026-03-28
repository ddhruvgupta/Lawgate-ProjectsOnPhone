namespace LegalDocSystem.API.Middleware;

/// <summary>
/// Adds security-hardening HTTP response headers.
///
/// CSRF posture: This API uses stateless JWT Bearer tokens sent via the Authorization header.
/// Cross-origin requests cannot set custom headers (blocked by the browser's CORS preflight),
/// so CSRF via forged cross-origin requests is not possible without a valid Bearer token.
/// If cookies are ever introduced for token storage, switch to SameSite=Strict cookies and
/// add ASP.NET Core's built-in AntiForgery services.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] =
            "camera=(), microphone=(), geolocation=(), payment=()";
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: blob:; " +
            "connect-src 'self'; " +
            "font-src 'self'; " +
            "frame-ancestors 'none';";
        // Prevent MIME-type sniffing on downloads
        context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";

        await _next(context);
    }
}
