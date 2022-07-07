using DotnetDependencyGrapher.Graphs;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;

namespace DotnetDependencyGrapher.Writers;

public class PlantUmlWriter : IOutputWriter
{
    private readonly ILogger<PlantUmlWriter> _logger;

    public PlantUmlWriter(ILogger<PlantUmlWriter> logger)
    {
        ArgumentNullException.ThrowIfNull(_logger = logger);
    }

    public void Write(IAssemblyDependencyGraph graph)
    {
        _logger.LogInformation("Start writing diagram...");

        string path = Path.Combine(Directory.GetCurrentDirectory(), "diagram.plantuml");

        using (var fs = new FileStream(path, FileMode.Create))
        using (var sr = new StreamWriter(fs))
        {
            sr.WriteLine("@startuml");
            sr.WriteLine("set namespaceSeparator none");
            sr.WriteLine("hide empty members");
            sr.WriteLine("hide circle");

            sr.WriteLine(string.Empty);

            foreach (var pair in graph.VersionsPerAssembly.OrderBy(x => x.Key))
            {
                string style = string.Empty;

                var firstVersion = Extensions.GetAssemblyName(pair.Key, pair.Value.First());

                // If single version + no referencers => Entry point
                if (pair.Value.Count == 1
                 && (!graph.Referencers.ContainsKey(firstVersion) || graph.Referencers[firstVersion].Count == 0))
                {
                    style = "#lightblue ##[bold]blue";
                }
                else
                {
                    Version minVersion = pair.Value.Min();
                    Version maxVersion = pair.Value.Max();

                    // We want to highlight this as a warning because referencing two major versions can produce exceptions at runtime
                    // (missing methods, signature changed, namespace change, etc...)
                    bool multipleMajor = minVersion.Major != maxVersion.Major;

                    if (multipleMajor)
                    {
                        style = "#lightyellow ##[bold]orange";
                    }
                }

                sr.WriteLine($"class \"{pair.Key}\" {style} {{");
                sr.WriteLine(string.Join("\n__\n", pair.Value.OrderBy(x => x).Select(x => $"{x}")));
                sr.WriteLine("}");

                sr.WriteLine(string.Empty);
            }

            foreach (var pair in graph.Dependencies)
            {
                foreach (var dep in pair.Value)
                {
                    sr.WriteLine($"\"{pair.Key.Name}::{pair.Key.Version}\" ---> \"{dep.Name}::{dep.Version}\"");
                }
            }

            sr.WriteLine("@enduml");
        }

        _logger.LogInformation("Diagram written to {FilePath}", path);
    }
}