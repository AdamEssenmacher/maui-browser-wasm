namespace Showcase.Shared.App;

public interface IShowcaseSectionNavigator
{
    bool ScrollToSection(string sectionId, bool smooth = true);
}
