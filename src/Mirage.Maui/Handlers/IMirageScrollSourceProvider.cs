namespace Mirage.Maui.Handlers;

internal interface IMirageScrollSourceProvider
{
    bool TryGetScrollSourceNodeId(out string nodeId);
}
