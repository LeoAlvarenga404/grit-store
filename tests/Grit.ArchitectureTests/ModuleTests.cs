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
}
