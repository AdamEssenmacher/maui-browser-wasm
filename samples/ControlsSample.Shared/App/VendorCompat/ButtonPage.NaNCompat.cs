using System;

namespace Maui.Controls.Sample.Pages;

public partial class ButtonPage
{
    // XAML source generation emits an unqualified `NaN` token for Thickness NaN.
    // Keep this compat constant local to the wrapper instead of patching vendored upstream XAML.
    private const double NaN = double.NaN;
}
