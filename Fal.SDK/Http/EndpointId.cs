using System.Text.RegularExpressions;

namespace Fal.SDK.Http;


public record EndpointId(
    string owner,
    string alias,
    string? path = null,
    string? ns = null
) {
    private static readonly string[] AppNamespaces = ["workflows", "comfy"];
    private static readonly Regex NumericIdPattern = new(@"^(\d+)-([a-zA-Z0-9-]+)$", RegexOptions.Compiled);


    public static string EnsureFormat(string id) {
        var parts = id.Split('/');
        if (parts.Length > 1) {
            return id;
        }

        var match = NumericIdPattern.Match(id);
        if (match.Success) {
            return $"{match.Groups[1].Value}/{match.Groups[2].Value}";
        }

        throw new ArgumentException($"Invalid endpoint id: {id}. Must be in the format owner/alias");
    }


    public static EndpointId Parse(string id) {
        string normalized = EnsureFormat(id);
        var parts = normalized.Split('/');

        if (parts.Length >= 3 && AppNamespaces.Contains(parts[0])) {
            return new EndpointId(
                owner: parts[1],
                alias: parts[2],
                path: parts.Length > 3 ? string.Join("/", parts[3..]) : null,
                ns: parts[0]
            );
        }

        return new EndpointId(
            owner: parts[0],
            alias: parts[1],
            path: parts.Length > 2 ? string.Join("/", parts[2..]) : null
        );
    }


    public string FormatPath() {
        string prefix = ns is not null ? $"{ns}/" : "";
        string suffix = path is not null ? $"/{path}" : "";
        return $"{prefix}{owner}/{alias}{suffix}";
    }
}
