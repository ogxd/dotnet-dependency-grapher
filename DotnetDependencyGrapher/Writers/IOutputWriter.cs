using DotnetDependencyGrapher.Graphs;

namespace DotnetDependencyGrapher.Writers;

public interface IOutputWriter
{
    public void Write(IAssemblyDependencyGraph graph);
}
