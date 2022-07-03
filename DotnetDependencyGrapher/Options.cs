using CommandLine;

namespace DotnetDependencyGrapher;

public class Options
{
    [Option('n', "name", Required = false, HelpText = "Package name")]
    public string Name { get; set; }

    [Option('v', "version", Required = false, HelpText = "Package version")]
    public string Version { get; set; }

}