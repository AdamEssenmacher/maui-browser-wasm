namespace ControlsSample.Shared.App;

public sealed class MirageControlsSampleVisibilityPolicy : IControlsSampleVisibilityPolicy
{
    public bool IsVisible(ControlsSampleEntry entry)
    {
        return entry.Status == ControlsSampleStatus.Runnable;
    }
}

public sealed class NativeControlsSampleVisibilityPolicy : IControlsSampleVisibilityPolicy
{
    public bool IsVisible(ControlsSampleEntry entry)
    {
        return entry.Status is ControlsSampleStatus.Runnable or ControlsSampleStatus.HiddenUnsupported;
    }
}
