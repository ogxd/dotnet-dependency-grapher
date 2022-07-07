using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DotnetDependencyGrapher;

internal static class Extensions
{
    public static V GetOrAdd<K, V>(this Dictionary<K, V> dictionary, K key, Func<K, V> factory)
    {
        ref V? value = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, key, out bool exists);
        if (!exists)
            value = factory(key);

        return value;
    }

    public static string GetTargetFramework(this Assembly assembly)
    {
        TargetFrameworkAttribute targetFrameworkAttribute = assembly.GetCustomAttribute<TargetFrameworkAttribute>();
        return targetFrameworkAttribute?.FrameworkName ?? "unknown";
    }

    public static AssemblyName GetAssemblyName(string name, Version version)
    {
        var assemblyName = new AssemblyName();
        assemblyName.Name = name;
        assemblyName.Version = version;
        return assemblyName;
    }
}
