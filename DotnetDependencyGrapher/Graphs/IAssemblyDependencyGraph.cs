using System;
using System.Collections.Generic;
using System.Reflection;

namespace DotnetDependencyGrapher.Graphs;

public interface IAssemblyDependencyGraph
{
    public IReadOnlyDictionary<AssemblyName, HashSet<AssemblyName>> Dependencies { get; }
    public IReadOnlyDictionary<AssemblyName, HashSet<AssemblyName>> References { get; }
    public IReadOnlyDictionary<string, HashSet<Version>> VersionsPerAssembly { get; }
}
