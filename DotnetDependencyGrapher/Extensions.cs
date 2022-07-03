using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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
}
