using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace DotnetDependencyGrapher;

public class Program
{
    public static void Main(string[] args)
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var dependencyGrapher = serviceProvider.GetService<DependencyGrapher>();

        Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(dependencyGrapher.Run);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(configure => configure.AddConsole(c =>
        {
            c.TimestampFormat = "[HH:mm:ss] ";
            c.FormatterName = ConsoleFormatterNames.Simple;
        }));

        services.AddTransient<DependencyGrapher>();
    }
}