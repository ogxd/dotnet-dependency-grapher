using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DotnetDependencyGrapher.Graphs;

internal class NugetDependencyGraph : IAssemblyDependencyGraph
{
    private readonly Dictionary<AssemblyName, HashSet<AssemblyName>> _dependencies = new Dictionary<AssemblyName, HashSet<AssemblyName>>(new AssemblyComparer());
    private readonly Dictionary<AssemblyName, HashSet<AssemblyName>> _referencers = new Dictionary<AssemblyName, HashSet<AssemblyName>>(new AssemblyComparer());
    private readonly Dictionary<string, HashSet<Version>> _versionsPerAssembly = new Dictionary<string, HashSet<Version>>();
    private readonly Dictionary<AssemblyName, Assembly> _loadedAssemblies = new Dictionary<AssemblyName, Assembly>();

    private readonly ILogger<NugetDependencyGraph> _logger;

    private readonly string _currentDirectory;

    private readonly Options _options;

    public IReadOnlyDictionary<AssemblyName, HashSet<AssemblyName>> Dependencies => _dependencies;
    public IReadOnlyDictionary<AssemblyName, HashSet<AssemblyName>> Referencers => _referencers;
    public IReadOnlyDictionary<string, HashSet<Version>> VersionsPerAssembly => _versionsPerAssembly;

    public NugetDependencyGraph(Options options, ILogger<NugetDependencyGraph> logger)
    {
        ArgumentNullException.ThrowIfNull(_options = options);
        ArgumentNullException.ThrowIfNull(_logger = logger);

        Directory.CreateDirectory("tmp");

        _logger.LogInformation("Start!");

        if (options.File.Any())
        {
            foreach (var file in options.File)
            {
                // Load from file (DLL)
                Assembly rootAssembly = Assembly.LoadFile(file);
                AssemblyName rootAssemblyName = rootAssembly.GetName();
                _loadedAssemblies.Add(rootAssemblyName, rootAssembly);
                _currentDirectory = Path.GetDirectoryName(file);

                TryCollect(rootAssemblyName);
            }
        }
        else
        {
            // Load from NuGet using name and version
            AssemblyName rootAssemblyName = Extensions.GetAssemblyName(options.Name, new Version(options.Version));
            _currentDirectory = string.Empty;

            TryCollect(rootAssemblyName);
        }

        _logger.LogInformation("Finished!");
    }

    private bool TryCollect(AssemblyName assemblyName)
    {
        // Collect dependencies of a given assembly not more than once
        if (_dependencies.ContainsKey(assemblyName))
            return false;

        // Get (or try to download from Nuget) assembly
        if (!TryGetAssembly(assemblyName, out Assembly assembly))
            return false;

        _logger.LogInformation("Checking dependencies for {AssemblyName}...", assemblyName);

        // Add this version to the version per assembly dictionary, in order to highlight eventual conflicts
        var versionsPerAssembly = _versionsPerAssembly.GetOrAdd(assemblyName.Name, static (_) => new HashSet<Version>());
        versionsPerAssembly.Add(assemblyName.Version);

        var dependencies = _dependencies.GetOrAdd(assemblyName, static (_) => new HashSet<AssemblyName>(new AssemblyComparer()));

        foreach (AssemblyName dependency in assembly.GetReferencedAssemblies())
        {
            if (ShouldIgnore(dependency))
                continue;

            TryCollect(dependency);

            dependencies.Add(dependency);

            var referencers = _referencers.GetOrAdd(dependency, static (_) => new HashSet<AssemblyName>(new AssemblyComparer()));
            referencers.Add(assemblyName);
        }

        return true;
    }

    private bool TryGetAssembly(AssemblyName assemblyName, out Assembly assembly)
    {
        assembly = null;

        // Check if already loaded
        if (_loadedAssemblies.TryGetValue(assemblyName, out assembly))
            return true;

        // Check if not in current directory
        string localDll = Path.Combine(_currentDirectory, assemblyName.Name + ".dll");
        if (File.Exists(localDll))
        {
            var assemblyTmp = Assembly.LoadFile(localDll);
            var assemblyNameTmp = assemblyTmp.GetName();

            _loadedAssemblies.TryAdd(assemblyNameTmp, assemblyTmp);

            if (assemblyName.Name == assemblyNameTmp.Name
             && assemblyName.Version == assemblyNameTmp.Version)
            {
                assembly = assemblyTmp;
                return true;
            }
        }

        // Search in NuGet local cache
        string searchPath = Path.Combine(Directory.GetCurrentDirectory(), "tmp", $"{assemblyName.Name}.{assemblyName.Version.Major}.{assemblyName.Version.Minor}.{assemblyName.Version.Build}", "lib");

        if (!Directory.Exists(searchPath))
        {
            // Try download from NuGet
            Process process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = false;
            process.StartInfo.RedirectStandardInput = false;
            process.StartInfo.RedirectStandardOutput = false;
            process.StartInfo.FileName = "nuget";
            // https://docs.microsoft.com/en-us/nuget/reference/cli-reference/cli-ref-install
            process.StartInfo.Arguments = $"install {assemblyName.Name} -NonInteractive -Source {_options.Source} -FallbackSource nuget.org -OutputDirectory tmp -Version {assemblyName.Version} -Verbosity quiet -DependencyVersion Ignore";
            // -FallbackSource nuget.org
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

        _loadedAssemblies.Add(assemblyName, assembly);

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