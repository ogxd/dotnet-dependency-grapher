using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using System;
using System.IO;

namespace DotnetDependencyGrapher;

internal class PrettyConsoleFormatter : ConsoleFormatter
{
    public PrettyConsoleFormatter() : base("Pretty")
    {

    }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
    {
        var color = logEntry.LogLevel switch
        {
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Warning => ConsoleColor.Yellow,
            _ => ConsoleColor.White
        };

        Console.ForegroundColor = color;

        textWriter.WriteLine($"{logEntry.Formatter(logEntry.State, logEntry.Exception)}");
    }
}

public class PrettyConsoleOptions : ConsoleFormatterOptions
{
    
}
