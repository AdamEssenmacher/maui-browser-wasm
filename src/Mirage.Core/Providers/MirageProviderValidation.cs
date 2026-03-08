namespace Mirage.Core.Providers;

public static class MirageProviderValidation
{
    public static IMirageComponentProvider GetRequiredProvider(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var provider = services.GetService(typeof(IMirageComponentProvider)) as IMirageComponentProvider;
        if (provider is null)
        {
            throw new InvalidOperationException("MirageRuntime requires IMirageComponentProvider. Register one with UseMirageComponentProvider<TProvider>().");
        }

        return provider;
    }
}
