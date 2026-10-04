using Application.Common.Exceptions;
using FluentValidation;

namespace Api.Extensions;

public static class ExceptionHandlingExtensions
{
    public static void UseExceptionHandling(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exception = context.Features
                    .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()
                    ?.Error;

                switch (exception)
                {
                    case ValidationException validationException:
                        {
                            context.Response.StatusCode =
                                StatusCodes.Status400BadRequest;

                            context.Response.ContentType = "application/json";

                            var errors = validationException.Errors
                                .GroupBy(x => x.PropertyName)
                                .ToDictionary(
                                    x => x.Key,
                                    x => x.Select(e => e.ErrorMessage).ToArray());

                            await context.Response.WriteAsJsonAsync(new
                            {
                                errors
                            });

                            break;
                        }

                    case ConflictException conflictException:
                        {
                            context.Response.StatusCode =
                                StatusCodes.Status409Conflict;

                            context.Response.ContentType = "application/json";

                            await context.Response.WriteAsJsonAsync(new
                            {
                                error = conflictException.Message
                            });

                            break;
                        }

                    default:
                        {
                            context.Response.StatusCode =
                                StatusCodes.Status500InternalServerError;

                            context.Response.ContentType = "application/json";

                            await context.Response.WriteAsJsonAsync(new
                            {
                                error = "An unexpected error occurred."
                            });

                            break;
                        }
                }
            });
        });
    }
}