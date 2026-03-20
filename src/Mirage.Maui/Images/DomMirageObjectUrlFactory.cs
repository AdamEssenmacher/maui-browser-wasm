using Mirage.Web.Dom;

namespace Mirage.Maui.Images;

internal sealed class DomMirageObjectUrlFactory : IMirageObjectUrlFactory
{
    public string? CreateObjectUrl(ReadOnlySpan<byte> content, string contentType)
    {
        if (content.Length == 0)
        {
            return null;
        }

        var normalizedContentType = string.IsNullOrWhiteSpace(contentType)
            ? "application/octet-stream"
            : contentType.Trim();
        var base64 = Convert.ToBase64String(content);
        return DomElement.CreateObjectUrlFromBase64(base64, normalizedContentType);
    }

    public void RevokeObjectUrl(string objectUrl)
    {
        if (string.IsNullOrWhiteSpace(objectUrl))
        {
            return;
        }

        DomElement.RevokeObjectUrl(objectUrl.Trim());
    }
}
