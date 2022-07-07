using DotnetDependencyGrapher.Graphs;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;

namespace DotnetDependencyGrapher.Writers;

public class CsvReferencersWriter : IOutputWriter
{
    private readonly ILogger<PlantUmlWriter> _logger;

    public CsvReferencersWriter(ILogger<PlantUmlWriter> logger)
    {
        ArgumentNullException.ThrowIfNull(_logger = logger);
    }

    public void Write(IAssemblyDependencyGraph graph)
    {
        _logger.LogInformation("Start writing CSV...");

        string path = Path.Combine(Directory.GetCurrentDirectory(), "referencers.csv");

        using (var fs = new FileStream(path, FileMode.Create))
        using (var sr = new StreamWriter(fs))
        {
            foreach (var pair in graph.Referencers.OrderBy(x => x.Key.Name))
            {
                // One line per package
                //sr.Write(pair.Key.Name);
                //sr.Write(';');
                //sr.Write(pair.Key.Version);
                //sr.Write(';');
                //sr.Write(string.Join(";", pair.Value.OrderBy(x => x.Version).Select(x => $"{x.Name};{x.Version}")));
                //sr.Write('\n');

                foreach (var referencer in pair.Value.OrderBy(x => x.Version))
                {
                    // One line per link
                    sr.Write(pair.Key.Name);
                    sr.Write(';');
                    sr.Write(pair.Key.Version);
                    sr.Write(';');
                    sr.Write(referencer.Name);
                    sr.Write(';');
                    sr.Write(referencer.Version);
                    sr.Write('\n');
                }
            }
        }

        _logger.LogInformation("CSV written to {FilePath}", path);
    }
}