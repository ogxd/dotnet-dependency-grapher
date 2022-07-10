using CommandLine;
using DotnetDependencyGrapher.Graphs;
using DotnetDependencyGrapher.Writers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace DotnetDependencyGrapher;

public class Program
{
    public static void Main(string[] args)
    {
        var options = Parser.Default.ParseArguments<Options>(args).Value;

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection, options);

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var graph = serviceProvider.GetService<IAssemblyDependencyGraph>();
        var writer = serviceProvider.GetService<IOutputWriter>();

        writer.Write(graph);
    }

    private static void ConfigureServices(IServiceCollection services, Options options)
    {
        services.AddLogging(configure => configure
            .SetMinimumLevel(options.Quiet ? LogLevel.Critical : LogLevel.Information)
            .AddConsole(c => {
                c.TimestampFormat = "[HH:mm:ss] ";
                c.FormatterName = ConsoleFormatterNames.Simple;
                c.FormatterName = "Pretty";
            })
            .AddConsoleFormatter<PrettyConsoleFormatter, PrettyConsoleOptions>());

        services.AddTransient(x => options);
        services.AddTransient<IAssemblyDependencyGraph, NugetDependencyGraph>();

        switch (options.Export)
        {
            case "csv":
            case "csvreferencers":
                services.AddTransient<IOutputWriter, CsvReferencersWriter>();
                break;
            case "circular":
                services.AddTransient<IOutputWriter, CircularReferencesWriter>();
                break;
            case "plantuml":
            default:
                services.AddTransient<IOutputWriter, PlantUmlWriter>();
                break;
        }
    }
}