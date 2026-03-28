using System.Text;
using System.Text.Json;
using System.Web;

namespace LegalDocSystem.API.Middleware;

/// <summary>
/// Sanitizes string values in incoming JSON request bodies to prevent XSS.
/// Strips HTML tags and encodes dangerous characters on all string fields.
/// Binary endpoints (file uploads) are excluded.
/// </summary>
public class InputSanitizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InputSanitizationMiddleware> _logger;

    // These paths deal with raw binary streams — skip sanitization
    private static readonly HashSet<string> _skipPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/document/upload"
    };

    public InputSanitizationMiddleware(RequestDelegate next, ILogger<InputSanitizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method == HttpMethods.Post ||
            context.Request.Method == HttpMethods.Put ||
            context.Request.Method == HttpMethods.Patch)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            if (!_skipPaths.Contains(path) &&
                context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
            {
                context.Request.EnableBuffering();

                var body = await new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true)
                    .ReadToEndAsync();

                context.Request.Body.Position = 0;

                if (!string.IsNullOrWhiteSpace(body))
                {
                    try
                    {
                        var sanitized = SanitizeJson(body);

                        if (sanitized != body)
                        {
                            _logger.LogDebug("Input sanitization removed potentially unsafe content from {Path}", path);
                            var sanitizedBytes = Encoding.UTF8.GetBytes(sanitized);
                            context.Request.Body = new MemoryStream(sanitizedBytes);
                            context.Request.ContentLength = sanitizedBytes.Length;
                        }
                    }
                    catch (JsonException)
                    {
                        // Invalid JSON — let the controller's model binding reject it with 400
                    }
                }
            }
        }

        await _next(context);
    }

    private static string SanitizeJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        SanitizeElement(doc.RootElement, writer);
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void SanitizeElement(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var prop in element.EnumerateObject())
                {
                    writer.WritePropertyName(prop.Name);
                    SanitizeElement(prop.Value, writer);
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                    SanitizeElement(item, writer);
                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                var raw = element.GetString() ?? string.Empty;
                var clean = SanitizeString(raw);
                writer.WriteStringValue(clean);
                break;

            default:
                // Numbers, booleans, null — write as-is
                element.WriteTo(writer);
                break;
        }
    }

    /// <summary>
    /// Strips HTML tags and HTML-encodes angle brackets to neutralise script injection.
    /// Preserves all normal text content (does NOT strip quotes, spaces, etc.).
    /// </summary>
    private static string SanitizeString(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Remove script/style tags and their contents
        value = System.Text.RegularExpressions.Regex.Replace(
            value, @"<script[^>]*>.*?</script>", string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase |
            System.Text.RegularExpressions.RegexOptions.Singleline);

        value = System.Text.RegularExpressions.Regex.Replace(
            value, @"<style[^>]*>.*?</style>", string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase |
            System.Text.RegularExpressions.RegexOptions.Singleline);

        // Strip remaining HTML tags
        value = System.Text.RegularExpressions.Regex.Replace(value, @"<[^>]*>", string.Empty);

        // Encode residual < and > to prevent any tag reconstruction
        value = value.Replace("<", "&lt;").Replace(">", "&gt;");

        return value.Trim();
    }
}
