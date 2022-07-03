using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DotnetDependencyGrapher;

public class AssemblyComparer : IEqualityComparer<AssemblyName>
{
    public bool Equals(AssemblyName? x, AssemblyName? y)
    {
        return x.Name == y.Name
            && x.Version == y.Version;
    }

    public int GetHashCode([DisallowNull] AssemblyName obj)
    {
        return HashCode.Combine(obj.Name, obj.Version);
    }
}
