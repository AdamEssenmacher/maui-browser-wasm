using System.Collections;
using System.Reflection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using MauiLayout = Microsoft.Maui.ILayout;

namespace Mirage.Maui.HotReload;

internal static class MirageXamlHotReloadApplicator
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly MethodInfo? XamlLoaderLoadMethod = FindXamlLoaderLoadMethod();
    private static readonly MethodInfo? GetNameScopeMethod = FindGetNameScopeMethod();
    private static int s_resourceDictionaryHotReloadInitialized;

    public static bool TryApplyElementXaml(Element element, string xaml, out Exception? error, Window? hostWindow = null)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentException.ThrowIfNullOrWhiteSpace(xaml);

        try
        {
            ApplyElementXaml(element, xaml, hostWindow);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex;
            return false;
        }
    }

    public static bool TryApplyResourceDictionaryXaml(ResourceDictionary dictionary, string xaml, out Exception? error)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentException.ThrowIfNullOrWhiteSpace(xaml);

        try
        {
            ApplyResourceDictionaryXaml(dictionary, xaml);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex;
            return false;
        }
    }

    private static void ApplyElementXaml(Element element, string xaml, Window? hostWindow)
    {
        var bindingContext = element.BindingContext;
        var existingNameScope = GetNameScope(element);
        ClearNameScope(existingNameScope);

        if (!TryLoadElementXaml(element, xaml, bindingContext, hostWindow, out var applyError, out var updatedInPlace))
        {
            throw applyError ?? new InvalidOperationException("Failed to apply XAML hot reload update.");
        }

        if (!updatedInPlace)
        {
            return;
        }

        element.BindingContext = bindingContext;
        RebindXamlNamedFields(element);
        RefreshElementHandler(element);
    }

    private static void ApplyResourceDictionaryXaml(ResourceDictionary dictionary, string xaml)
    {
        EnsureResourceDictionaryHotReloadHelperInitialized();
        dictionary.Clear();
        LoadXaml(dictionary, xaml);
    }

    private static void LoadXaml(object target, string xaml)
    {
        if (XamlLoaderLoadMethod is null)
        {
            throw new InvalidOperationException("Unable to resolve Microsoft.Maui.Controls.Xaml.XamlLoader.Load(object, string).");
        }

        try
        {
            XamlLoaderLoadMethod.Invoke(null, [target, xaml]);
        }
        catch (TargetInvocationException invocationException) when (invocationException.InnerException is not null)
        {
            throw invocationException.InnerException;
        }
    }

    private static bool TryLoadElementXaml(
        Element liveElement,
        string xaml,
        object? bindingContext,
        Window? hostWindow,
        out Exception? error,
        out bool updatedInPlace)
    {
        updatedInPlace = true;

        if (HasMissingPlatformView(liveElement.Handler))
        {
            if (TryReplaceLivePageWithDetachedClone(liveElement, xaml, bindingContext, hostWindow, out var pageSwapError))
            {
                error = null;
                updatedInPlace = false;
                return true;
            }

            if (liveElement is Page)
            {
                error = pageSwapError is null
                    ? new InvalidOperationException("Page hot reload fallback failed with no reported error.")
                    : new InvalidOperationException(
                        "Page hot reload fallback failed while the live page handler had a null PlatformView.",
                        pageSwapError);
                return false;
            }

            if (TryApplyViaDetachedClone(liveElement, xaml, bindingContext, out var inPlaceFallbackError))
            {
                error = null;
                return true;
            }

            var fallbackError = CombineFallbackErrors(pageSwapError, inPlaceFallbackError);
            error = fallbackError is null
                ? new InvalidOperationException(
                    "Live element has a handler with a null PlatformView and no detached fallback path was available.")
                : new InvalidOperationException(
                    "Live element has a handler with a null PlatformView and detached fallback strategies failed.",
                    fallbackError);
            return false;
        }

        try
        {
            LoadXaml(liveElement, xaml);
            error = null;
            return true;
        }
        catch (Exception applyException)
        {
            var shouldAttemptFallback = ShouldTryDetachedFallback(applyException)
                || HasMissingPlatformView(liveElement.Handler);

            if (!shouldAttemptFallback)
            {
                error = applyException;
                return false;
            }

            if (TryReplaceLivePageWithDetachedClone(liveElement, xaml, bindingContext, hostWindow, out var pageSwapError))
            {
                error = null;
                updatedInPlace = false;
                return true;
            }

            if (liveElement is Page)
            {
                error = pageSwapError is null
                    ? new InvalidOperationException(
                        "Live XAML apply failed for a Page and no page-swap fallback error was reported.",
                        applyException)
                    : new InvalidOperationException(
                        "Live XAML apply failed for a Page and page-swap fallback also failed.",
                        pageSwapError);
                return false;
            }

            if (TryApplyViaDetachedClone(liveElement, xaml, bindingContext, out var inPlaceFallbackError))
            {
                error = null;
                return true;
            }

            var fallbackError = CombineFallbackErrors(pageSwapError, inPlaceFallbackError);
            error = fallbackError is null
                ? new InvalidOperationException(
                    "Live XAML apply failed and no detached fallback path was available.",
                    applyException)
                : new InvalidOperationException(
                    "Live XAML apply failed and detached fallback strategies also failed.",
                    fallbackError);
            return false;
        }
    }

    private static bool ShouldTryDetachedFallback(Exception error)
    {
        for (var cursor = error; cursor is not null; cursor = cursor.InnerException)
        {
            if (cursor is not InvalidOperationException invalidOperationException)
            {
                continue;
            }

            if (IsPlatformViewGetterFailure(invalidOperationException))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPlatformViewGetterFailure(InvalidOperationException exception)
    {
        if (string.Equals(exception.TargetSite?.Name, "get_PlatformView", StringComparison.Ordinal))
        {
            return true;
        }

        if (exception.StackTrace?.Contains("get_PlatformView", StringComparison.Ordinal) == true)
        {
            return true;
        }

        var message = exception.Message;
        return !string.IsNullOrWhiteSpace(message)
            && message.Contains("PlatformView", StringComparison.OrdinalIgnoreCase)
            && message.Contains("null", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryReplaceLivePageWithDetachedClone(
        Element liveElement,
        string xaml,
        object? bindingContext,
        Window? hostWindow,
        out Exception? error)
    {
        error = null;
        if (liveElement is not Page livePage)
        {
            error = new InvalidOperationException(
                "Page-swap hot reload fallback requires a live Page target.");
            return false;
        }

        var window = ResolveHostWindow(livePage, hostWindow);
        if (window is null)
        {
            error = new InvalidOperationException(
                "Page-swap hot reload fallback requires a live Page attached to a Window.");
            return false;
        }

        if (!TryCreateDetachedClone(liveElement, out var stagedElement, out error)
            || stagedElement is not Page stagedPage)
        {
            return false;
        }

        try
        {
            stagedPage.BindingContext = bindingContext;
            ClearNameScope(GetNameScope(stagedPage));
            LoadXaml(stagedPage, xaml);
            stagedPage.BindingContext = bindingContext;
            RebindXamlNamedFields(stagedPage);
            window.Page = stagedPage;
            return true;
        }
        catch (Exception ex)
        {
            error = ex;
            return false;
        }
    }

    private static Window? ResolveHostWindow(Page livePage, Window? hostWindow)
    {
        if (livePage.Window is not null)
        {
            return livePage.Window;
        }

        if (TryResolveWindowFromElementParents(livePage) is { } parentWindow)
        {
            return parentWindow;
        }

        if (hostWindow is not null)
        {
            if (ReferenceEquals(hostWindow.Page, livePage))
            {
                return hostWindow;
            }

            if (hostWindow.Page is not null && hostWindow.Page.GetType() == livePage.GetType())
            {
                return hostWindow;
            }
        }

        if (Application.Current is not Application app)
        {
            return hostWindow;
        }

        var windows = app.Windows.OfType<Window>().ToList();
        if (hostWindow is not null && !windows.Contains(hostWindow))
        {
            windows.Add(hostWindow);
        }

        if (windows.Count == 0)
        {
            return null;
        }

        foreach (var appWindow in windows)
        {
            if (ReferenceEquals(appWindow.Page, livePage))
            {
                return appWindow;
            }
        }

        if (windows.Count == 1)
        {
            return windows[0];
        }

        foreach (var appWindow in windows)
        {
            if (appWindow.Page is not null && appWindow.Page.GetType() == livePage.GetType())
            {
                return appWindow;
            }
        }

        return null;
    }

    private static Window? TryResolveWindowFromElementParents(Element element)
    {
        for (Element? cursor = element.Parent; cursor is not null; cursor = cursor.Parent)
        {
            if (cursor is Window window)
            {
                return window;
            }
        }

        return null;
    }

    private static bool TryApplyViaDetachedClone(
        Element liveElement,
        string xaml,
        object? bindingContext,
        out Exception? error)
    {
        error = null;
        if (!TryCreateDetachedClone(liveElement, out var stagedElement, out error))
        {
            error ??= new InvalidOperationException(
                $"Unable to create detached clone for '{liveElement.GetType().FullName}'.");
            return false;
        }

        try
        {
            stagedElement.BindingContext = bindingContext;
            ClearNameScope(GetNameScope(stagedElement));
            LoadXaml(stagedElement, xaml);
            stagedElement.BindingContext = bindingContext;

            if (!TryTransferStagedTree(liveElement, stagedElement))
            {
                error = new InvalidOperationException(
                    $"Detached hot reload fallback does not support '{liveElement.GetType().FullName}'.");
                return false;
            }

            CopyXamlNamedFields(stagedElement, liveElement);
            return true;
        }
        catch (Exception ex)
        {
            error = ex;
            return false;
        }
    }

    private static Exception? CombineFallbackErrors(Exception? primary, Exception? secondary)
    {
        if (primary is null)
        {
            return secondary;
        }

        if (secondary is null)
        {
            return primary;
        }

        return new AggregateException(primary, secondary);
    }

    private static bool TryCreateDetachedClone(Element liveElement, out Element stagedElement, out Exception? error)
    {
        error = null;
        stagedElement = null!;
        var elementType = liveElement.GetType();
        if (elementType.IsAbstract)
        {
            return false;
        }

        try
        {
            try
            {
                if (Activator.CreateInstance(elementType, nonPublic: true) is Element createdElement)
                {
                    stagedElement = createdElement;
                    return true;
                }
            }
            catch (MissingMethodException)
            {
                // Constructor fallback below handles pages without a parameterless constructor.
            }

            var services = ResolveServicesForClone(liveElement);
            foreach (var constructor in elementType
                         .GetConstructors(InstanceFlags)
                         .OrderByDescending(static ctor => ctor.GetParameters().Length))
            {
                if (!TryResolveConstructorArguments(constructor, liveElement, services, out var args))
                {
                    continue;
                }

                if (constructor.Invoke(args) is not Element constructedElement)
                {
                    continue;
                }

                stagedElement = constructedElement;
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            error = ex;
            return false;
        }
    }

    private static IServiceProvider? ResolveServicesForClone(Element liveElement)
    {
        if (liveElement.Handler?.MauiContext?.Services is { } liveServices)
        {
            return liveServices;
        }

        if (liveElement is Page livePage && livePage.Window?.Handler?.MauiContext?.Services is { } windowServices)
        {
            return windowServices;
        }

        if (TryResolveParentWindow(liveElement)?.Handler?.MauiContext?.Services is { } parentWindowServices)
        {
            return parentWindowServices;
        }

        if (Application.Current?.Handler?.MauiContext?.Services is { } appServices)
        {
            return appServices;
        }

        return null;
    }

    private static Window? TryResolveParentWindow(Element liveElement)
    {
        for (Element? cursor = liveElement; cursor is not null; cursor = cursor.Parent)
        {
            if (cursor is Page page && page.Window is not null)
            {
                return page.Window;
            }
        }

        return null;
    }

    private static bool TryResolveConstructorArguments(
        ConstructorInfo constructor,
        Element liveElement,
        IServiceProvider? services,
        out object?[] args)
    {
        var parameters = constructor.GetParameters();
        args = new object?[parameters.Length];
        for (var index = 0; index < parameters.Length; index++)
        {
            var parameter = parameters[index];
            if (services?.GetService(parameter.ParameterType) is { } service)
            {
                args[index] = service;
                continue;
            }

            if (TryResolveConstructorArgumentFromLiveElementFields(liveElement, parameter.ParameterType, out var existingValue))
            {
                args[index] = existingValue;
                continue;
            }

            if (parameter.HasDefaultValue)
            {
                args[index] = parameter.DefaultValue;
                continue;
            }

            return false;
        }

        return true;
    }

    private static bool TryResolveConstructorArgumentFromLiveElementFields(
        Element liveElement,
        Type parameterType,
        out object? value)
    {
        for (var typeCursor = liveElement.GetType(); typeCursor is not null && typeCursor != typeof(object); typeCursor = typeCursor.BaseType)
        {
            foreach (var field in typeCursor.GetFields(InstanceFlags | BindingFlags.DeclaredOnly))
            {
                if (field.IsStatic || !parameterType.IsAssignableFrom(field.FieldType))
                {
                    continue;
                }

                var candidate = field.GetValue(liveElement);
                if (candidate is null)
                {
                    continue;
                }

                value = candidate;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static bool TryTransferStagedTree(Element liveElement, Element stagedElement)
    {
        if (liveElement is ContentPage livePage && stagedElement is ContentPage stagedPage)
        {
            if (HasMissingPlatformView(liveElement.Handler))
            {
                return false;
            }

            livePage.Resources = stagedPage.Resources;
            var stagedContent = stagedPage.Content;
            stagedPage.Content = null;
            livePage.Content = stagedContent;
            livePage.Padding = stagedPage.Padding;
            livePage.Title = stagedPage.Title;
            livePage.Background = stagedPage.Background;
            return true;
        }

        if (liveElement is ContentView liveContentView && stagedElement is ContentView stagedContentView)
        {
            if (HasMissingPlatformView(liveElement.Handler))
            {
                return false;
            }

            liveContentView.Resources = stagedContentView.Resources;
            var stagedContent = stagedContentView.Content;
            stagedContentView.Content = null;
            liveContentView.Content = stagedContent;
            return true;
        }

        return false;
    }

    private static void CopyXamlNamedFields(object source, object target)
    {
        if (source is not Element sourceElement)
        {
            return;
        }

        for (var typeCursor = target.GetType(); typeCursor is not null && typeCursor != typeof(object); typeCursor = typeCursor.BaseType)
        {
            foreach (var field in typeCursor.GetFields(InstanceFlags | BindingFlags.DeclaredOnly))
            {
                if (field.IsStatic || string.IsNullOrWhiteSpace(field.Name))
                {
                    continue;
                }

                object? resolvedValue;
                try
                {
                    resolvedValue = sourceElement.FindByName(field.Name);
                }
                catch
                {
                    continue;
                }

                if (resolvedValue is null || !field.FieldType.IsInstanceOfType(resolvedValue))
                {
                    continue;
                }

                field.SetValue(target, resolvedValue);
            }
        }
    }

    private static void RebindXamlNamedFields(object target)
    {
        if (target is not BindableObject bindableObject)
        {
            return;
        }

        var nameScope = GetNameScope(bindableObject);
        MethodInfo? findByNameMethod = null;
        if (nameScope is not null)
        {
            findByNameMethod = nameScope.GetType().GetMethod(
                "FindByName",
                InstanceFlags,
                binder: null,
                [typeof(string)],
                modifiers: null);
        }

        for (var typeCursor = target.GetType(); typeCursor is not null && typeCursor != typeof(object); typeCursor = typeCursor.BaseType)
        {
            foreach (var field in typeCursor.GetFields(InstanceFlags | BindingFlags.DeclaredOnly))
            {
                if (field.IsStatic || string.IsNullOrWhiteSpace(field.Name))
                {
                    continue;
                }

                object? resolvedValue;
                try
                {
                    resolvedValue = target is Element elementTarget
                        ? elementTarget.FindByName(field.Name)
                        : findByNameMethod?.Invoke(nameScope, [field.Name]);
                }
                catch
                {
                    continue;
                }

                if (resolvedValue is null || !field.FieldType.IsInstanceOfType(resolvedValue))
                {
                    continue;
                }

                field.SetValue(target, resolvedValue);
            }
        }
    }

    private static void RefreshElementHandler(Element element)
    {
        var handler = element.Handler;
        if (handler is null)
        {
            return;
        }

        if (!TryGetPlatformView(handler, out _))
        {
            return;
        }

        if (element is ContentPage)
        {
            handler.UpdateValue(nameof(ContentPage.Content));
            handler.UpdateValue(nameof(ContentPage.Padding));
            return;
        }

        if (element is IContentView)
        {
            handler.UpdateValue(nameof(IContentView.Content));
            return;
        }

        if (element is MauiLayout)
        {
            handler.Invoke(nameof(ILayoutHandler.Update), null);
        }
    }

    private static bool HasMissingPlatformView(IElementHandler? handler)
    {
        if (handler is null)
        {
            return false;
        }

        return !TryGetPlatformView(handler, out _);
    }

    private static bool TryGetPlatformView(IElementHandler handler, out object? platformView)
    {
        try
        {
            platformView = handler.PlatformView;
            return platformView is not null;
        }
        catch (InvalidOperationException)
        {
            platformView = null;
            return false;
        }
    }

    private static object? GetNameScope(BindableObject bindableObject)
    {
        if (GetNameScopeMethod is null)
        {
            return null;
        }

        return GetNameScopeMethod.Invoke(null, [bindableObject]);
    }

    private static void ClearNameScope(object? nameScope)
    {
        if (nameScope is null)
        {
            return;
        }

        ClearCollectionField(nameScope, "_names");
        ClearCollectionField(nameScope, "_values");

        var clearMethod = nameScope.GetType().GetMethod(
            "Clear",
            InstanceFlags,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);
        clearMethod?.Invoke(nameScope, null);
    }

    private static void ClearCollectionField(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, InstanceFlags);
        if (field?.GetValue(target) is not { } value)
        {
            return;
        }

        if (value is IDictionary dictionary)
        {
            dictionary.Clear();
            return;
        }

        var clearMethod = value.GetType().GetMethod(
            "Clear",
            InstanceFlags,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);
        clearMethod?.Invoke(value, null);
    }

    private static void EnsureResourceDictionaryHotReloadHelperInitialized()
    {
        if (Interlocked.Exchange(ref s_resourceDictionaryHotReloadInitialized, 1) == 1)
        {
            return;
        }

        var helperType = FindType(
            "Microsoft.Maui.Controls.Xaml.Diagnostics.ResourceDictionaryHotReloadHelper",
            "Microsoft.Maui.Controls.HotReload.ResourceDictionaryHotReloadHelper",
            "ResourceDictionaryHotReloadHelper");

        var initMethod = helperType?.GetMethod(
            "Init",
            StaticFlags,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);
        initMethod?.Invoke(null, null);
    }

    private static MethodInfo? FindXamlLoaderLoadMethod()
    {
        var loaderType = FindType(
            "Microsoft.Maui.Controls.Xaml.XamlLoader",
            "XamlLoader");

        return loaderType?.GetMethod(
            "Load",
            StaticFlags,
            binder: null,
            [typeof(object), typeof(string)],
            modifiers: null);
    }

    private static MethodInfo? FindGetNameScopeMethod()
    {
        var nameScopeType = FindType(
            "Microsoft.Maui.Controls.NameScope",
            "Microsoft.Maui.Controls.Internals.NameScope",
            "NameScope");

        return nameScopeType?.GetMethod(
            "GetNameScope",
            StaticFlags,
            binder: null,
            [typeof(BindableObject)],
            modifiers: null);
    }

    private static Type? FindType(params string[] fullNamesOrNames)
    {
        foreach (var candidate in fullNamesOrNames)
        {
            var direct = Type.GetType(candidate, throwOnError: false);
            if (direct is not null)
            {
                return direct;
            }
        }

        TryLoadAssembly("Microsoft.Maui.Controls");
        TryLoadAssembly("Microsoft.Maui.Controls.Xaml");

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException reflectionLoadException)
            {
                types = reflectionLoadException.Types.Where(static type => type is not null).Cast<Type>().ToArray();
            }
            catch
            {
                continue;
            }

            foreach (var type in types)
            {
                foreach (var candidate in fullNamesOrNames)
                {
                    if (string.Equals(type.FullName, candidate, StringComparison.Ordinal)
                        || string.Equals(type.Name, candidate, StringComparison.Ordinal))
                    {
                        return type;
                    }
                }
            }
        }

        return null;
    }

    private static void TryLoadAssembly(string assemblyName)
    {
        if (AppDomain.CurrentDomain.GetAssemblies().Any(assembly =>
                string.Equals(assembly.GetName().Name, assemblyName, StringComparison.Ordinal)))
        {
            return;
        }

        try
        {
            _ = Assembly.Load(assemblyName);
        }
        catch
        {
            // Ignore optional load failures; type scan fallback still runs.
        }
    }
}
