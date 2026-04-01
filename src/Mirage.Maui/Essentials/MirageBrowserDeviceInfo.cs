using System.Threading;
using Mirage.Core.Diagnostics;
using Microsoft.Maui.Devices;

namespace Mirage.Maui.Essentials;

internal sealed class MirageBrowserDeviceInfo : IDeviceInfo
{
    private static readonly DevicePlatform BrowserPlatform = DevicePlatform.Create("Browser");
    private static readonly Version BrowserVersion = new(0, 0);
    private static int s_loggedViewportInteropFailure;

    private readonly BrowserDeviceSnapshot _snapshot;

    public MirageBrowserDeviceInfo()
        : this(CreateCurrentSnapshot())
    {
    }

    internal MirageBrowserDeviceInfo(double viewportWidth, double viewportHeight)
        : this(new BrowserDeviceSnapshot(viewportWidth, viewportHeight))
    {
    }

    internal MirageBrowserDeviceInfo(BrowserDeviceSnapshot snapshot)
    {
        _snapshot = snapshot;
    }

    public string Model => "Browser";

    public string Manufacturer => "Browser";

    public string Name => "Browser";

    public string VersionString => "0.0";

    public Version Version => BrowserVersion;

    public DevicePlatform Platform => BrowserPlatform;

    public DeviceIdiom Idiom => ClassifyIdiom(_snapshot.ViewportWidth, _snapshot.ViewportHeight);

    public DeviceType DeviceType => DeviceType.Physical;

    internal static DeviceIdiom ClassifyIdiom(double viewportWidth, double viewportHeight)
    {
        var normalizedWidth = NormalizeDimension(viewportWidth, fallback: 1024d);
        var normalizedHeight = NormalizeDimension(viewportHeight, fallback: 768d);
        var shortestEdge = Math.Min(normalizedWidth, normalizedHeight);

        if (shortestEdge < 600d)
        {
            return DeviceIdiom.Phone;
        }

        if (shortestEdge < 900d)
        {
            return DeviceIdiom.Tablet;
        }

        return DeviceIdiom.Desktop;
    }

    private static BrowserDeviceSnapshot CreateCurrentSnapshot()
    {
        var viewport = TryGetViewportSize();
        return new BrowserDeviceSnapshot(viewport.Width, viewport.Height);
    }

    private static (double Width, double Height) TryGetViewportSize()
    {
        try
        {
            var domElementType = Type.GetType("Mirage.Web.Dom.DomElement, Mirage.Web", throwOnError: false);
            var getViewportSizeMethod = domElementType?.GetMethod(
                "GetViewportSize",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            if (getViewportSizeMethod is null)
            {
                return HandleViewportInteropFailure("missing-dom-viewport-helper");
            }

            if (getViewportSizeMethod.Invoke(null, null) is ValueTuple<double, double> viewport)
            {
                return viewport;
            }

            return HandleViewportInteropFailure("unexpected-dom-viewport-result");
        }
        catch (Exception error) when (error is not InvalidOperationException)
        {
            return HandleViewportInteropFailure("dom-viewport-helper-threw", error);
        }
    }

    private static double NormalizeDimension(double value, double fallback)
    {
        return double.IsFinite(value) && value > 0d ? value : fallback;
    }

    private static (double Width, double Height) HandleViewportInteropFailure(string reason, Exception? error = null)
    {
        LogViewportInteropFailure(reason, error);

        if (OperatingSystem.IsBrowser())
        {
            throw new InvalidOperationException(
                $"Mirage browser DeviceInfo could not capture viewport snapshot: {reason}.",
                error);
        }

        return (1024d, 768d);
    }

    private static void LogViewportInteropFailure(string reason, Exception? error)
    {
        if (Interlocked.Exchange(ref s_loggedViewportInteropFailure, 1) != 0)
        {
            return;
        }

        var message = error is null
            ? $"viewportSnapshot unavailable reason={reason}"
            : $"viewportSnapshot unavailable reason={reason} message={error.Message}";
        MirageDiagnostics.Log("essentials.deviceinfo", message);
    }

    internal readonly record struct BrowserDeviceSnapshot(double ViewportWidth, double ViewportHeight);
}
