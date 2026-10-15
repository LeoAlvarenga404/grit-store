using NetArchTest.Rules;

namespace Grit.ArchitectureTests;

public class ModuleTests
{
    [Fact]
    public void ModuleProjects_AreAllReferencedByArchitectureTests()
    {
        var loaded = ModuleAssemblies.Load().Select(assembly => assembly.GetName().Name);

        ModuleAssemblies.ProjectNamesInSource().Except(loaded).ShouldBeEmpty();
    }

    [Fact]
    public void Modules_ReferenceOtherModulesOnlyThroughContracts()
    {
        var violations = ModuleAssemblies.Load()
            .SelectMany(assembly => assembly.GetReferencedAssemblies()
                .Where(ModuleAssemblies.IsModule)
                .Where(reference => !reference.Name!.EndsWith(".Contracts", StringComparison.Ordinal))
                .Select(reference => $"{assembly.GetName().Name} -> {reference.Name}"));

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void Domain_DependsOnlyOnSystemAndSharedKernel()
    {
        var violations = ModuleAssemblies.Load()
            .SelectMany(assembly =>
            {
                var domain = $"{assembly.GetName().Name}.Domain";
                return Types.InAssembly(assembly)
                    .That()
                    .ResideInNamespace(domain)
                    .Should()
                    .OnlyHaveDependencyOn("System", "Grit.SharedKernel", domain)
                    .GetResult()
                    .FailingTypes
                    .Select(type => type.FullName);
            });

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void ProbeModule_IsCheckedByTheModuleRules()
    {
        var names = ModuleAssemblies.Load().Select(assembly => assembly.GetName().Name).ToList();

        names.ShouldContain("Grit.Modules.Probe");
        names.ShouldContain("Grit.Modules.Probe.Contracts");
    }

    // Contracts cross module boundaries and the wire: primitives only, never Money or another kernel type.
    [Fact]
    public void Contracts_DependOnlyOnSystem()
    {
        var violations = ModuleAssemblies.Load()
            .Where(assembly => assembly.GetName().Name!.EndsWith(".Contracts", StringComparison.Ordinal))
            .SelectMany(assembly => Types.InAssembly(assembly)
                .Should()
                .OnlyHaveDependencyOn("System", assembly.GetName().Name!)
                .GetResult()
                .FailingTypes
                .Select(type => type.FullName));

        violations.ShouldBeEmpty();
    }
}
