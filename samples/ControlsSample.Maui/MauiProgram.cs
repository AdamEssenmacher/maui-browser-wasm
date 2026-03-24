using ControlsSample.Shared.App;
using Microsoft.Maui.Controls.Compatibility.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;

namespace ControlsSample.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCompatibility()
            .UseMauiMaps()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Dokdo-Regular.ttf", "Dokdo");
                fonts.AddFont("LobsterTwo-Regular.ttf", "Lobster Two");
                fonts.AddFont("LobsterTwo-Bold.ttf", "Lobster Two Bold");
                fonts.AddFont("LobsterTwo-Italic.ttf", "Lobster Two Italic");
                fonts.AddFont("LobsterTwo-BoldItalic.ttf", "Lobster Two BoldItalic");
                fonts.AddFont("ionicons.ttf", "Ionicons");
                fonts.AddFont("SegoeUI.ttf", "Segoe UI");
                fonts.AddFont("SegoeUI-Bold.ttf", "Segoe UI Bold");
                fonts.AddFont("SegoeUI-Italic.ttf", "Segoe UI Italic");
                fonts.AddFont("SegoeUI-Bold-Italic.ttf", "Segoe UI Bold Italic");
            });

        builder.Services.AddSingleton<IControlsSampleVisibilityPolicy, NativeControlsSampleVisibilityPolicy>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        ControlsSampleApp.BootstrapServices = app.Services;
        return app;
    }
}
