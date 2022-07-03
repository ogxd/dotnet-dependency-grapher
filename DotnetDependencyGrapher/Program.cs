using CommandLine;

namespace DotnetDependencyGrapher;

public class Program
{
    public static void Main(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(new DependencyGrapher().Run);
    }
}