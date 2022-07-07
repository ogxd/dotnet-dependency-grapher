using CommandLine;
using System.Collections.Generic;

namespace DotnetDependencyGrapher;

public class Options
{
    [Option('f', "file", Required = false, HelpText = "DLL path")]
    public IEnumerable<string> File { get; set; }

    [Option('n', "name", Required = false, HelpText = "Package name")]
    public string Name { get; set; }

    [Option('v', "version", Required = false, HelpText = "Package version")]
    public string Version { get; set; }

    [Option('s', "source", Required = false, HelpText = "Nuget source (default is official Nuget)", Default = "nuget.org")]
    public string Source { get; set; }

    [Option('e', "export", Required = false, HelpText = "Export mode", Default = "plantuml")]
    public string Export { get; set; }
}