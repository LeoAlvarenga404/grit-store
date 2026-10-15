using System.Reflection;

namespace Grit.ArchitectureTests;

internal static class ModuleAssemblies
{
    private const string Prefix = "Grit.Modules.";

    public static IReadOnlyList<Assembly> Load() =>
        Directory.GetFiles(AppContext.BaseDirectory, Prefix + "*.dll")
            .Select(path => Assembly.Load(AssemblyName.GetAssemblyName(path)))
            .ToList();

    public static IReadOnlyList<string> ProjectNamesInSource()
    {
        var source = Path.Combine(FindRepositoryRoot(), "src");
        return Directory.GetDirectories(source, Prefix + "*")
            .Select(Path.GetFileName)
            .OfType<string>()
            .ToList();
    }

    public static bool IsModule(AssemblyName name) =>
        name.Name is not null && name.Name.StartsWith(Prefix, StringComparison.Ordinal);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Grit.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Grit.slnx not found above the test output.");
    }
}
