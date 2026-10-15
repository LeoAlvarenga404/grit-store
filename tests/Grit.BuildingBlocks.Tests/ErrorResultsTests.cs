using Grit.SharedKernel;
using Microsoft.AspNetCore.Http;

namespace Grit.BuildingBlocks.Tests;

public class ErrorResultsTests
{
    public static TheoryData<ErrorType, int> StatusCodesByErrorType => new()
    {
        { ErrorType.Validation, StatusCodes.Status400BadRequest },
        { ErrorType.Unauthorized, StatusCodes.Status401Unauthorized },
        { ErrorType.Forbidden, StatusCodes.Status403Forbidden },
        { ErrorType.NotFound, StatusCodes.Status404NotFound },
        { ErrorType.Conflict, StatusCodes.Status409Conflict },
        { ErrorType.Failure, StatusCodes.Status500InternalServerError },
    };

    [Theory]
    [MemberData(nameof(StatusCodesByErrorType))]
    public void ToProblem_ErrorType_MapsToStatusCode(ErrorType type, int expectedStatusCode)
    {
        var problem = new Error("some.code", "Some message.", type).ToProblem();

        problem.StatusCode.ShouldBe(expectedStatusCode);
        problem.ProblemDetails.Status.ShouldBe(expectedStatusCode);
    }

    [Fact]
    public void ToProblem_Error_PutsMessageInDetailAndCodeInExtensions()
    {
        var problem = new Error("note.not_found", "The note does not exist.", ErrorType.NotFound).ToProblem();

        problem.ProblemDetails.Detail.ShouldBe("The note does not exist.");
        problem.ProblemDetails.Extensions["code"].ShouldBe("note.not_found");
    }

    [Fact]
    public void ToProblem_WithFieldErrors_PutsThemInErrorsExtension()
    {
        var fieldErrors = new Dictionary<string, string[]> { ["text"] = ["'Text' must not be empty."] };
        var error = new Error("validation.failed", "Invalid.", ErrorType.Validation) { FieldErrors = fieldErrors };

        var problem = error.ToProblem();

        problem.ProblemDetails.Extensions["errors"].ShouldBeSameAs(fieldErrors);
    }

    [Fact]
    public void ToProblem_WithoutFieldErrors_HasNoErrorsExtension()
    {
        var problem = new Error("some.code", "Some message.", ErrorType.Conflict).ToProblem();

        problem.ProblemDetails.Extensions.ContainsKey("errors").ShouldBeFalse();
    }

    [Fact]
    public void ToProblem_Error_IsProblemJson()
    {
        var problem = new Error("some.code", "Some message.", ErrorType.Conflict).ToProblem();

        problem.ContentType.ShouldBe("application/problem+json");
    }
}
