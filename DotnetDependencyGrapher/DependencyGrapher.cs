using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;

namespace DotnetDependencyGrapher;

internal class DependencyGrapher
{
    private Dictionary<AssemblyName, HashSet<AssemblyName>> _dependencies = new Dictionary<AssemblyName, HashSet<AssemblyName>>(new AssemblyComparer());
    private Dictionary<AssemblyName, HashSet<AssemblyName>> _referencers = new Dictionary<AssemblyName, HashSet<AssemblyName>>(new AssemblyComparer());
    private Dictionary<AssemblyName, string> _targetFrameworks = new Dictionary<AssemblyName, string>(new AssemblyComparer());
    private Dictionary<string, HashSet<Version>> _versionsPerAssembly = new Dictionary<string, HashSet<Version>>();

    public void Run(Options options)
    {
        Directory.CreateDirectory("tmp");

        AssemblyName rootAssemblyName = new AssemblyName();
        rootAssemblyName.Name = options.Name;
        rootAssemblyName.Version = new Version(options.Version);

        Collect(rootAssemblyName);

        Console.ForegroundColor = ConsoleColor.Red;

        foreach (var pair in _versionsPerAssembly.Where(x => x.Value.Count > 1))
        {
            Console.WriteLine($"Version conflict for '{pair.Key}' ({string.Join(", ", pair.Value)})");
        }

        Console.ForegroundColor = ConsoleColor.White;
    }

    private void Collect(AssemblyName assemblyName)
    {
        if (ShouldIgnore(assemblyName))
            return;

        // Collect dependencies of a given assembly not more than once
        if (_dependencies.ContainsKey(assemblyName))
            return;

        // Get (or try to download from Nuget) assembly
        if (!TryGetAssembly(assemblyName, out Assembly assembly))
            return;

        TargetFrameworkAttribute targetFrameworkAttribute = assembly.GetCustomAttribute<TargetFrameworkAttribute>();
        _targetFrameworks.Add(assemblyName, targetFrameworkAttribute?.FrameworkName);

        // Add this version to the version per assembly dictionary, in order to highlight eventual conflicts
        var versionsPerAssembly = _versionsPerAssembly.GetOrAdd(assemblyName.Name, static (_) => new HashSet<Version>());
        versionsPerAssembly.Add(assemblyName.Version);

        var dependencies = _dependencies.GetOrAdd(assemblyName, static (_) => new HashSet<AssemblyName>(new AssemblyComparer()));

        foreach (AssemblyName dependency in assembly.GetReferencedAssemblies())
        {
            dependencies.Add(dependency);

            var referencers = _referencers.GetOrAdd(dependency, static (_) => new HashSet<AssemblyName>(new AssemblyComparer()));
            referencers.Add(assemblyName);

            Collect(dependency);
        }
    }

    private bool TryGetAssembly(AssemblyName assemblyName, out Assembly assembly)
    {
        assembly = null;

        var searchPath = $@"tmp\{assemblyName.Name}.{assemblyName.Version.Major}.{assemblyName.Version.Minor}.{assemblyName.Version.Build}\lib";

        if (!Directory.Exists(searchPath))
        {
            Process process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = false;
            process.StartInfo.RedirectStandardInput = false;
            process.StartInfo.RedirectStandardOutput = false;
            process.StartInfo.FileName = "nuget";
            // https://docs.microsoft.com/en-us/nuget/reference/cli-reference/cli-ref-install
            process.StartInfo.Arguments = $"install {assemblyName.Name} -NonInteractive -Source GitLab -OutputDirectory tmp -Version {assemblyName.Version} -Verbosity quiet -DependencyVersion Ignore";
            // -FallbackSource NuGet
            // -DirectDownload
            process.Start();

            process.WaitForExit();

            if (!Directory.Exists(searchPath))
                return false;
        }

        var lib = Directory.EnumerateFiles(searchPath, "*.dll", SearchOption.AllDirectories).FirstOrDefault();

        if (lib == null)
            return false;

        lib = Path.GetFullPath(lib);

        assembly = Assembly.LoadFile(lib);

        return true;
    }

    private bool ShouldIgnore(AssemblyName assemblyName)
    {
        if (assemblyName.Name.StartsWith("System"))
            return true;

        if (assemblyName.Name.StartsWith("Microsoft"))
            return true;

        if (assemblyName.Name.StartsWith("netstandard"))
            return true;

        if (assemblyName.Name.StartsWith("mscorlib"))
            return true;

        return false;
    }
}