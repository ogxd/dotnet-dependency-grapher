using DotnetDependencyGrapher.Graphs;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DotnetDependencyGrapher.Writers;

public class OverviewWriter : IOutputWriter
{
    private readonly ILogger<PlantUmlWriter> _logger;

    public OverviewWriter(ILogger<PlantUmlWriter> logger)
    {
        ArgumentNullException.ThrowIfNull(_logger = logger);
    }

    public void Write(IAssemblyDependencyGraph graph)
    {
        _logger.LogInformation("Start writing Overview...");

        string path = Path.Combine(Directory.GetCurrentDirectory(), "overview.md");

        using (var fs = new FileStream(path, FileMode.Create))
        using (var sr = new StreamWriter(fs))
        {
            sr.WriteLine($"# Overview");
            sr.WriteLine($"- {graph.VersionsPerAssembly.Count} dependencies");
            sr.WriteLine($"- {graph.Dependencies.Count} dependencies (including version)");

            var referencesMergedByName = graph.References
                .GroupBy(x => x.Key.Name)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .SelectMany(y => y.Value)
                        .GroupBy(y => y.Name)
                        .SelectMany(z => z
                            .Select(w => w.Name)));

            sr.WriteLine($"## Unique References");
            sr.WriteLine($"| Assembly | Unique Reference |");
            sr.WriteLine($"|---|---|");
            foreach (var singleReferences in referencesMergedByName.Where(x => x.Value.Count() == 1))
            {
                sr.WriteLine($"| {singleReferences.Key} | {singleReferences.Value.First()} |");
            }
        }

        _logger.LogInformation("Overview written to {FilePath}", path);
    }

    private bool HasDependencyRecursive(IAssemblyDependencyGraph graph, AssemblyName assemblyName, string dependencyNameToLookFor)
    {
        if (!graph.Dependencies.ContainsKey(assemblyName))
            return false;

        foreach (var dependency in graph.Dependencies[assemblyName])
        {
            if (dependency.Name == dependencyNameToLookFor)
                return true;

            if (HasDependencyRecursive(graph, dependency, dependencyNameToLookFor))
                return true;
        }

        return false;
    }
}