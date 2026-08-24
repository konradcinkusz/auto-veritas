using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AutoVeritas.ServiceDefaults;

/// <summary>
/// Kernel-owned DataAnnotations validation for minimal APIs: one generic endpoint
/// filter returning <see cref="Results.ValidationProblem"/> grouped by member, so
/// every service reports the same 400 shape.
/// </summary>
public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
        {
            return Results.BadRequest(new { error = $"A request body of type {typeof(T).Name} is required." });
        }

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(argument);
        if (!Validator.TryValidateObject(argument, validationContext, validationResults, validateAllProperties: true))
        {
            var errors = validationResults
                .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty),
                    (result, member) => (member, message: result.ErrorMessage ?? "Invalid value."))
                .GroupBy(entry => entry.member, entry => entry.message)
                .ToDictionary(group => group.Key, group => group.ToArray());
            return Results.ValidationProblem(errors);
        }

        return await next(context);
    }
}
