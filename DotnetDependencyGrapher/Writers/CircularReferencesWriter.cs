using DotnetDependencyGrapher.Graphs;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;

namespace DotnetDependencyGrapher.Writers;

public class CircularReferencesWriter : IOutputWriter
{
    private readonly ILogger<PlantUmlWriter> _logger;

    public CircularReferencesWriter(ILogger<PlantUmlWriter> logger)
    {
        ArgumentNullException.ThrowIfNull(_logger = logger);
    }

    public void Write(IAssemblyDependencyGraph graph)
    {
        _logger.LogInformation("Start writing CSV...");

        string path = Path.Combine(Directory.GetCurrentDirectory(), "circular.csv");

        using (var fs = new FileStream(path, FileMode.Create))
        using (var sr = new StreamWriter(fs))
        {
            foreach (var assembly in graph.Dependencies.Keys)
            {
                if (HasDependencyRecursive(graph, assembly, assembly.Name))
                {
                    sr.WriteLine(assembly);
                }
            }
        }

        _logger.LogInformation("CSV written to {FilePath}", path);
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