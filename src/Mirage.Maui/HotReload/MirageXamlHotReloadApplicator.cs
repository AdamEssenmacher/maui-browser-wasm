using System.Collections;
using System.Reflection;
using Microsoft.Maui.Controls;

namespace Mirage.Maui.HotReload;

internal static class MirageXamlHotReloadApplicator
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly MethodInfo? XamlLoaderLoadMethod = FindXamlLoaderLoadMethod();
    private static readonly MethodInfo? GetNameScopeMethod = FindGetNameScopeMethod();
    private static int s_resourceDictionaryHotReloadInitialized;

    public static bool TryApplyElementXaml(Element element, string xaml, out Exception? error)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentException.ThrowIfNullOrWhiteSpace(xaml);

        try
        {
            ApplyElementXaml(element, xaml);
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

    private static void ApplyElementXaml(Element element, string xaml)
    {
        var bindingContext = element.BindingContext;
        var existingNameScope = GetNameScope(element);
        ClearNameScope(existingNameScope);

        LoadXaml(element, xaml);

        element.BindingContext = bindingContext;
        RebindXamlNamedFields(element);
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
