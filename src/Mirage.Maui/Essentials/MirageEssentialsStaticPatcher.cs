using System.Reflection;
using System.Diagnostics.CodeAnalysis;
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

    [DynamicDependency("defaultImplementation", typeof(Preferences))]
    [DynamicDependency("currentImplementation", typeof(AppInfo))]
    public static IDisposable Apply(IServiceProvider services) => Apply(services, DefaultDescriptors);

    internal static IDisposable Apply(IServiceProvider services, params MirageEssentialsPatchDescriptor[] descriptors)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(descriptors);

        lock (Sync)
        {
            var snapshots = new MirageEssentialsFieldSnapshot[descriptors.Length];
            var appliedCount = 0;

            try
            {
                for (var index = 0; index < descriptors.Length; index++)
                {
                    var descriptor = descriptors[index];
                    var field = descriptor.WrapperType.GetField(descriptor.FieldName, BindingFlags.Static | BindingFlags.NonPublic);
                    if (field is null)
                    {
                        throw new InvalidOperationException(
                            $"Mirage Essentials patcher could not find private field '{descriptor.WrapperType.FullName}.{descriptor.FieldName}'. " +
                            "Upstream MAUI likely changed or publish trimming removed required reflection metadata. " +
                            "Update the patch descriptor or preserve the field for trimming.");
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

                    snapshots[appliedCount] = new MirageEssentialsFieldSnapshot(field, field.GetValue(null));
                    appliedCount++;
                    field.SetValue(null, implementation);
                }
            }
            catch
            {
                RestoreSnapshots(snapshots, appliedCount);
                throw;
            }

            return new RestoreScope(snapshots, appliedCount);
        }
    }

    internal readonly record struct MirageEssentialsPatchDescriptor(Type WrapperType, Type ServiceType, string FieldName);

    private readonly record struct MirageEssentialsFieldSnapshot(FieldInfo Field, object? PreviousValue);

    private sealed class RestoreScope : IDisposable
    {
        private readonly MirageEssentialsFieldSnapshot[] _snapshots;
        private readonly int _count;
        private bool _disposed;

        public RestoreScope(MirageEssentialsFieldSnapshot[] snapshots, int count)
        {
            _snapshots = snapshots;
            _count = count;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            lock (Sync)
            {
                RestoreSnapshots(_snapshots, _count);
                _disposed = true;
            }
        }
    }

    private static void RestoreSnapshots(MirageEssentialsFieldSnapshot[] snapshots, int count)
    {
        for (var index = count - 1; index >= 0; index--)
        {
            var snapshot = snapshots[index];
            snapshot.Field.SetValue(null, snapshot.PreviousValue);
        }
    }
}
