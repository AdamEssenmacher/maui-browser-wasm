using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace Mirage.Maui.Essentials;

internal static class MirageEssentialsStaticPatcher
{
    private static readonly object Sync = new();
    private static readonly MirageEssentialsPatchDescriptor[] DefaultDescriptors =
    [
        new(typeof(Preferences), typeof(IPreferences), "defaultImplementation"),
        new(typeof(AppInfo), typeof(IAppInfo), "currentImplementation")
    ];

    public static IDisposable Apply(IServiceProvider services) => Apply(services, DefaultDescriptors);

    internal static IDisposable Apply(IServiceProvider services, params MirageEssentialsPatchDescriptor[] descriptors)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(descriptors);

        lock (Sync)
        {
            var snapshots = new MirageEssentialsFieldSnapshot[descriptors.Length];
            for (var index = 0; index < descriptors.Length; index++)
            {
                var descriptor = descriptors[index];
                var field = descriptor.WrapperType.GetField(descriptor.FieldName, BindingFlags.Static | BindingFlags.NonPublic);
                if (field is null)
                {
                    throw new InvalidOperationException(
                        $"Mirage Essentials patcher could not find private field '{descriptor.WrapperType.FullName}.{descriptor.FieldName}'. " +
                        "Upstream MAUI likely changed and the static patch descriptor must be updated.");
                }

                var implementation = services.GetService(descriptor.ServiceType);
                if (implementation is null)
                {
                    throw new InvalidOperationException(
                        $"Mirage Essentials patcher could not resolve service '{descriptor.ServiceType.FullName}' for '{descriptor.WrapperType.FullName}'.");
                }

                if (!field.FieldType.IsInstanceOfType(implementation))
                {
                    throw new InvalidOperationException(
                        $"Mirage Essentials patcher resolved service '{descriptor.ServiceType.FullName}', " +
                        $"but '{descriptor.WrapperType.FullName}.{descriptor.FieldName}' expects '{field.FieldType.FullName}'.");
                }

                snapshots[index] = new MirageEssentialsFieldSnapshot(field, field.GetValue(null));
                field.SetValue(null, implementation);
            }

            return new RestoreScope(snapshots);
        }
    }

    internal readonly record struct MirageEssentialsPatchDescriptor(Type WrapperType, Type ServiceType, string FieldName);

    private readonly record struct MirageEssentialsFieldSnapshot(FieldInfo Field, object? PreviousValue);

    private sealed class RestoreScope : IDisposable
    {
        private readonly MirageEssentialsFieldSnapshot[] _snapshots;
        private bool _disposed;

        public RestoreScope(MirageEssentialsFieldSnapshot[] snapshots)
        {
            _snapshots = snapshots;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            lock (Sync)
            {
                for (var index = _snapshots.Length - 1; index >= 0; index--)
                {
                    var snapshot = _snapshots[index];
                    snapshot.Field.SetValue(null, snapshot.PreviousValue);
                }

                _disposed = true;
            }
        }
    }
}
