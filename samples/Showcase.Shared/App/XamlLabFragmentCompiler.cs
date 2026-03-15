using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace Showcase.Shared.App;

public interface IXamlLabFragmentCompiler
{
    XamlLabCompileResult Compile(string fragmentText);
}

public sealed record XamlLabCompileResult(bool IsSuccess, IView? Preview, IReadOnlyList<string> Errors)
{
    public static XamlLabCompileResult Success(IView preview) => new(true, preview, Array.Empty<string>());

    public static XamlLabCompileResult Failure(params string[] errors) => new(false, null, errors);

    public static XamlLabCompileResult Failure(IReadOnlyList<string> errors) => new(false, null, errors);
}

public sealed class XamlLabFragmentCompiler : IXamlLabFragmentCompiler
{
    private const string MauiNamespace = "http://schemas.microsoft.com/dotnet/2021/maui";
    private const string XamlNamespace = "http://schemas.microsoft.com/winfx/2009/xaml";
    private static readonly IReadOnlyDictionary<string, Type> AllowedTypes = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        [nameof(ContentView)] = typeof(ContentView),
        [nameof(Label)] = typeof(Label),
        [nameof(Button)] = typeof(Button),
        [nameof(Entry)] = typeof(Entry),
        [nameof(VerticalStackLayout)] = typeof(VerticalStackLayout),
        [nameof(HorizontalStackLayout)] = typeof(HorizontalStackLayout),
        [nameof(Grid)] = typeof(Grid),
        [nameof(ScrollView)] = typeof(ScrollView)
    };

    private static readonly HashSet<string> DisallowedRoots = new(StringComparer.Ordinal)
    {
        nameof(Application),
        nameof(Window),
        nameof(NavigationPage),
        nameof(Shell)
    };

    private static readonly HashSet<string> DisallowedXamlDirectives = new(StringComparer.Ordinal)
    {
        "Class",
        "Code",
        "FactoryMethod",
        "Arguments",
        "TypeArguments"
    };

    private static readonly Dictionary<Type, HashSet<string>> EventNamesByType = new();

    public XamlLabCompileResult Compile(string fragmentText)
    {
        if (string.IsNullOrWhiteSpace(fragmentText))
        {
            return XamlLabCompileResult.Failure("Fragment is empty. Enter a XAML fragment with a single root control.");
        }

        var parseResult = TryParseFragment(fragmentText, out var rootElement, out var parseErrors);
        if (!parseResult || rootElement is null)
        {
            return XamlLabCompileResult.Failure(parseErrors);
        }

        var validationErrors = ValidateFragment(rootElement).ToArray();
        if (validationErrors.Length > 0)
        {
            return XamlLabCompileResult.Failure(validationErrors);
        }

        try
        {
            var preview = BuildPreviewFromFragment(fragmentText);
            return XamlLabCompileResult.Success(preview);
        }
        catch (Exception ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;
            return XamlLabCompileResult.Failure($"XAML compilation failed: {message}");
        }
    }

    private static bool TryParseFragment(
        string fragmentText,
        out XElement? rootElement,
        out IReadOnlyList<string> errors)
    {
        rootElement = null;
        try
        {
            var wrapped =
                $"<__mirage_xaml_lab_fragment__ xmlns=\"{MauiNamespace}\">{fragmentText}</__mirage_xaml_lab_fragment__>";
            var document = XDocument.Parse(wrapped, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            var wrapper = document.Root;
            if (wrapper is null)
            {
                errors = ["Fragment wrapper parse failed."];
                return false;
            }

            var hasUnexpectedText = wrapper
                .Nodes()
                .OfType<XText>()
                .Any(node => !string.IsNullOrWhiteSpace(node.Value));
            if (hasUnexpectedText)
            {
                errors = ["Fragment must contain a single root element and no top-level text nodes."];
                return false;
            }

            var topLevelElements = wrapper.Elements().ToArray();
            if (topLevelElements.Length != 1)
            {
                errors = ["Fragment must contain exactly one root element."];
                return false;
            }

            rootElement = topLevelElements[0];
            errors = Array.Empty<string>();
            return true;
        }
        catch (XmlException xmlException)
        {
            errors =
            [
                $"XML parse error at line {xmlException.LineNumber}, position {xmlException.LinePosition}: {xmlException.Message}"
            ];
            return false;
        }
        catch (Exception ex)
        {
            errors = [$"Fragment parse failed: {ex.Message}"];
            return false;
        }
    }

    private static IEnumerable<string> ValidateFragment(XElement rootElement)
    {
        var errors = new List<string>();
        ValidateElementRecursive(rootElement, errors, isRoot: true);
        return errors;
    }

    private static void ValidateElementRecursive(XElement element, ICollection<string> errors, bool isRoot)
    {
        if (!string.Equals(element.Name.NamespaceName, MauiNamespace, StringComparison.Ordinal))
        {
            errors.Add($"Unsupported XML namespace on element '{element.Name}'. Use the MAUI namespace only.");
            return;
        }

        var localName = element.Name.LocalName;
        if (!AllowedTypes.ContainsKey(localName))
        {
            errors.Add($"Control '{localName}' is not allowed in XAML Lab v1.");
            return;
        }

        if (isRoot && DisallowedRoots.Contains(localName))
        {
            errors.Add($"Root '{localName}' is not supported. Author a fragment root from the allowed control set.");
            return;
        }

        var controlType = AllowedTypes[localName];
        foreach (var attribute in element.Attributes())
        {
            if (attribute.IsNamespaceDeclaration)
            {
                continue;
            }

            var attributeNamespace = attribute.Name.NamespaceName;
            var attributeLocalName = attribute.Name.LocalName;
            if (string.Equals(attributeNamespace, XamlNamespace, StringComparison.Ordinal)
                && DisallowedXamlDirectives.Contains(attributeLocalName))
            {
                errors.Add($"XAML directive 'x:{attributeLocalName}' is not allowed in XAML Lab.");
                continue;
            }

            if (IsEventAttribute(controlType, attributeLocalName))
            {
                errors.Add($"Event handler attribute '{attributeLocalName}' is not allowed in XAML Lab.");
                continue;
            }

            if (ContainsMarkupExpression(attribute.Value))
            {
                errors.Add($"Markup expression in attribute '{attributeLocalName}' is not supported in XAML Lab v1.");
            }
        }

        foreach (var child in element.Elements())
        {
            ValidateElementRecursive(child, errors, isRoot: false);
        }
    }

    private static bool ContainsMarkupExpression(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Contains('{', StringComparison.Ordinal) && value.Contains('}', StringComparison.Ordinal);
    }

    private static bool IsEventAttribute(Type controlType, string attributeLocalName)
    {
        if (string.IsNullOrWhiteSpace(attributeLocalName))
        {
            return false;
        }

        if (!EventNamesByType.TryGetValue(controlType, out var eventNames))
        {
            eventNames = controlType
                .GetEvents(BindingFlags.Instance | BindingFlags.Public)
                .Select(info => info.Name)
                .ToHashSet(StringComparer.Ordinal);
            EventNamesByType[controlType] = eventNames;
        }

        return eventNames.Contains(attributeLocalName);
    }

    private static IView BuildPreviewFromFragment(string fragmentText)
    {
        var host = new ContentView();
        var hostXaml = $"""
                        <ContentView xmlns="{MauiNamespace}" xmlns:x="{XamlNamespace}">
                            {fragmentText}
                        </ContentView>
                        """;
        host.LoadFromXaml(hostXaml);

        if (host.Content is not IView preview)
        {
            throw new InvalidOperationException("Fragment did not produce a valid preview root.");
        }

        return preview;
    }
}
