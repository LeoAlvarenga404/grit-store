using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Grit.Api;

internal static class OpenApiDocumentation
{
    private const string BearerScheme = "Bearer";

    public static void Configure(OpenApiOptions options)
    {
        options.AddSchemaTransformer(AddErrorExtensions);
        options.AddDocumentTransformer(AddBearerScheme);
        options.AddOperationTransformer(RequireBearerWhenAuthorized);
    }

    // ErrorResults writes `code` and `errors` as ProblemDetails extensions, which the type itself does not declare (ADR 0006).
    // Optional: framework errors (malformed JSON, 404/405, 429) carry neither.
    private static Task AddErrorExtensions(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type != typeof(ProblemDetails))
        {
            return Task.CompletedTask;
        }

        schema.Properties ??= new Dictionary<string, IOpenApiSchema>();
        schema.Properties["code"] = new OpenApiSchema { Type = JsonSchemaType.String };
        schema.Properties["errors"] = new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            AdditionalProperties = new OpenApiSchema { Type = JsonSchemaType.Array, Items = new OpenApiSchema { Type = JsonSchemaType.String } },
        };
        return Task.CompletedTask;
    }

    private static Task AddBearerScheme(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[BearerScheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "The access token from POST /api/v1/identity/login.",
        };
        return Task.CompletedTask;
    }

    // RequireAuthorization() adds IAuthorizeData; RequireAuthorization(policy => …) adds the built AuthorizationPolicy.
    private static Task RequireBearerWhenAuthorized(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        if (!metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<AuthorizationPolicy>().Any())
        {
            return Task.CompletedTask;
        }

        operation.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(BearerScheme, context.Document)] = [] }];
        return Task.CompletedTask;
    }
}
