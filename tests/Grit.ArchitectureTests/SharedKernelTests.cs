using Grit.SharedKernel;
using NetArchTest.Rules;

namespace Grit.ArchitectureTests;

public class SharedKernelTests
{
    [Fact]
    public void SharedKernel_DependsOnlyOnSystem()
    {
        var result = Types.InAssembly(typeof(Result).Assembly)
            .Should()
            .OnlyHaveDependencyOn("System", "Grit.SharedKernel")
            .GetResult();

        result.SelectedTypesForTesting.ShouldNotBeEmpty();
        result.FailingTypes.Select(type => type.FullName).ShouldBeEmpty();
    }
}
