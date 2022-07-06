using Microsoft.Extensions.Logging;
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
    private readonly Dictionary<AssemblyName, HashSet<AssemblyName>> _dependencies = new Dictionary<AssemblyName, HashSet<AssemblyName>>(new AssemblyComparer());
    private readonly Dictionary<AssemblyName, HashSet<AssemblyName>> _referencers = new Dictionary<AssemblyName, HashSet<AssemblyName>>(new AssemblyComparer());
    private readonly Dictionary<AssemblyName, string> _targetFrameworks = new Dictionary<AssemblyName, string>(new AssemblyComparer());
    private readonly Dictionary<string, HashSet<Version>> _versionsPerAssembly = new Dictionary<string, HashSet<Version>>();
    private readonly Dictionary<AssemblyName, Assembly> _loadedAssemblies = new Dictionary<AssemblyName, Assembly>();

    private readonly ILogger<DependencyGrapher> _logger;

    private string _currentDirectory;

    private Options _options;

    public DependencyGrapher(ILogger<DependencyGrapher> logger)
    {
        ArgumentNullException.ThrowIfNull(_logger = logger);
    }

    private static AssemblyName GetAssemblyName(string name, Version version)
    {
        var assemblyName = new AssemblyName();
        assemblyName.Name = name;
        assemblyName.Version = version;
        return assemblyName;
    }

    public void Run(Options options)
    {
        _options = options;

        Directory.CreateDirectory("tmp");

        AssemblyName rootAssemblyName;

        _logger.LogInformation("Start!");

        if (string.IsNullOrEmpty(options.File))
        {
            // Load from NuGet using name and version
            rootAssemblyName = GetAssemblyName(options.Name, new Version(options.Version));
            _currentDirectory = string.Empty;
        }
        else
        {
            // Load from file (DLL)
            Assembly rootAssembly = Assembly.LoadFile(options.File);
            rootAssemblyName = rootAssembly.GetName();
            _loadedAssemblies.Add(rootAssemblyName, rootAssembly);
            _currentDirectory = Path.GetDirectoryName(options.File);
        }

        Collect(rootAssemblyName);

        foreach (var pair in _versionsPerAssembly)
        {
            // Check if conflict
            if (pair.Value.Count <= 1)
                continue;

            Version minVersion = pair.Value.Min();
            Version maxVersion = pair.Value.Max();

            // Check conflict severity
            if (minVersion.MajorRevision == maxVersion.Major
             && minVersion.MinorRevision == maxVersion.MinorRevision)
                continue;

            var frameworks = new HashSet<string>(pair.Value.Select(x => _targetFrameworks[GetAssemblyName(pair.Key, x)]));
            if (frameworks.Count > 1)
                _logger.LogError($"Framework conflict for '{pair.Key}'");

            _logger.LogError($"Version conflict for '{pair.Key}'");

            foreach (var version in pair.Value)
            {
                var conflictingAssembly = GetAssemblyName(pair.Key, version);

                _logger.LogError($"- '{version}' ({_targetFrameworks[conflictingAssembly]})");

                foreach (var referencer in _referencers[conflictingAssembly])
                {
                    _logger.LogError($"  - '{referencer.Name} {referencer.Version}' ({_targetFrameworks[referencer]})");
                }
            }
        }

        _logger.LogInformation("Finished!");
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

        _logger.LogInformation("Checking dependencies for {AssemblyName}...", assemblyName);

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
            process.StartInfo.Arguments = $"install {assemblyName.Name} -NonInteractive -Source {_options.Source} -OutputDirectory tmp -Version {assemblyName.Version} -Verbosity quiet -DependencyVersion Ignore";
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

    private void WritePlantUML()
    {

    }
}