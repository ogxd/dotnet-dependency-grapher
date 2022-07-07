# Dotnet Dependency Grapher

Uses nuget to fetch packages and create a two way dependency graph. Useful for many things, such as:
- Have a clear overview of architecture
- Identify circular references
- Identify major version conflicts (potential runtime crashes)
- Identify out of date packages
- Identify single references (potential mergable)

> ⚠️ Nuget must be installed

## Usage

From a local DLL:    
`DotnetDependencyWalker.exe --file path/to/myassembly.dll`

From several local DLLs (it will be displayed on the same graph):    
`DotnetDependencyWalker.exe --file path/to/myassemblyA.dll path/to/myassemblyB.dll`

From a nuget package:    
`DotnetDependencyWalker.exe --name myassembly --version 1.2.3`

Using a different Nuget source than nuget.org:    
`DotnetDependencyWalker.exe --file path/to/myassembly.dll --source GitLab`

Exporting as plantuml (it's actually the default export, so you don't need to explicitely type it)    
`DotnetDependencyWalker.exe --file path/to/myassembly.dll --export plantuml`

Exporting referencers as csv (one line = one link, "A with version X is referenced by B with version Y")    
`DotnetDependencyWalker.exe --file path/to/myassembly.dll --export csvreferencers`