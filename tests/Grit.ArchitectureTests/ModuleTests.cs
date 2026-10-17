using Grit.BuildingBlocks;
using MediatR;
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

    // TransactionBehavior keys on ICommand: a command without the marker would write outside the transaction.
    [Fact]
    public void Commands_ImplementICommand_AndQueriesDoNot()
    {
        var types = ModuleAssemblies.Load().SelectMany(assembly => assembly.GetTypes()).ToList();

        var commandsWithoutMarker = types
            .Where(type => type.Name.EndsWith("Command", StringComparison.Ordinal) && !type.IsAssignableTo(typeof(ICommand)));
        var queriesWithMarker = types
            .Where(type => type.Name.EndsWith("Query", StringComparison.Ordinal) && type.IsAssignableTo(typeof(ICommand)));

        commandsWithoutMarker.Concat(queriesWithMarker).Select(type => type.FullName).ShouldBeEmpty();
    }

    // ADR 0005: a command runs on one connection inside the ambient transaction, so its handler never opens a Dapper connection.
    [Fact]
    public void CommandHandlers_DoNotUseDapper()
    {
        var violations = ModuleAssemblies.Load()
            .SelectMany(assembly => Types.InAssembly(assembly).That().HaveDependencyOnAny("Dapper").GetTypes())
            .Select(type => OutermostType(type.ReflectionType))
            .Where(HandlesACommand)
            .Select(type => type.FullName)
            .Distinct();

        violations.ShouldBeEmpty();
    }

    // An async Handle compiles to a nested state machine type, which is where the Dapper calls live.
    private static Type OutermostType(Type type) => type.DeclaringType is null ? type : OutermostType(type.DeclaringType);

    private static bool HandlesACommand(Type type) =>
        type.GetInterfaces().Any(contract => contract.IsGenericType
            && contract.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
            && contract.GetGenericArguments()[0].IsAssignableTo(typeof(ICommand)));
}
