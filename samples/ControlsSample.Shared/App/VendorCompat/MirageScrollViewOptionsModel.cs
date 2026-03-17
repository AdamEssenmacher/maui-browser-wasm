using Maui.Controls.Sample.Models;
using Maui.Controls.Sample.Pages.ScrollViewPages;
using Maui.Controls.Sample.ViewModels;

namespace ControlsSample.Shared.App.VendorCompat;

// Keep unsupported nested pages imported while filtering them from browser navigation
// until Mirage ships the required control handlers.
public sealed class MirageScrollViewOptionsModel : ScrollViewOptionsModel
{
    protected override IEnumerable<SectionModel> CreateItems()
    {
        return base.CreateItems()
            .Where(item => item.Type != typeof(ScrollViewOrientationPage));
    }
}
