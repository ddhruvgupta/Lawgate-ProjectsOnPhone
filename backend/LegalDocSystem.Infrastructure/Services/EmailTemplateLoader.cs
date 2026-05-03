using System.Reflection;

namespace LegalDocSystem.Infrastructure.Services;

/// <summary>
/// Loads email HTML templates embedded in the Infrastructure assembly and fills
/// named {{placeholder}} tokens with caller-supplied values.
/// Callers are responsible for HTML-encoding any user-supplied values before
/// passing them into the dictionary.
/// </summary>
internal static class EmailTemplateLoader
{
    private static readonly Assembly _assembly = typeof(EmailTemplateLoader).Assembly;
    private const string ResourcePrefix = "LegalDocSystem.Infrastructure.EmailTemplates.";

    /// <summary>
    /// Loads the named template and replaces all <c>{{key}}</c> tokens with the
    /// corresponding values from <paramref name="values"/>.
    /// </summary>
    /// <param name="templateFileName">
    ///   Filename of the embedded template, e.g. <c>email-verification.html</c>.
    /// </param>
    /// <param name="values">
    ///   Token replacements. Keys must match the placeholder names (without braces).
    ///   Values must be safe to embed directly in HTML (encode user input before passing).
    /// </param>
    public static string Load(string templateFileName, Dictionary<string, string> values)
    {
        var resourceName = ResourcePrefix + templateFileName;

        using var stream = _assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Email template '{templateFileName}' not found as embedded resource '{resourceName}'. " +
                $"Available resources: {string.Join(", ", _assembly.GetManifestResourceNames())}");

        using var reader = new StreamReader(stream);
        var html = reader.ReadToEnd();

        foreach (var (key, value) in values)
        {
            html = html.Replace("{{" + key + "}}", value, StringComparison.Ordinal);
        }

        return html;
    }
}
