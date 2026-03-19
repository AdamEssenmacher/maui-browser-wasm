namespace Mirage.Maui.Images;

internal interface IMirageObjectUrlFactory
{
    string? CreateObjectUrl(ReadOnlySpan<byte> content, string contentType);

    void RevokeObjectUrl(string objectUrl);
}
