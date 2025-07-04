using System.Reflection;

namespace MetaExchange.Shared.Helper;

public class EmbeddedResourceHelper
{
    //public static string GetEmbeddedResource(Assembly assembly, string resourceName)
    //{
    //    Stream resourceStream = assembly.GetManifestResourceStream(resourceName)
    //        ?? throw new ArgumentException($"Resource '{resourceName}' not found in assembly '{assembly.FullName}'.");
    //    using StreamReader reader = new(resourceStream);
    //    return reader.ReadToEnd();
    //}

    public static string GetEmbeddedResource(string resourceName)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        Stream resourceStream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new ArgumentException($"Resource '{resourceName}' not found in assembly '{assembly.FullName}'.");
        using StreamReader reader = new(resourceStream);
        return reader.ReadToEnd();
    }
}
