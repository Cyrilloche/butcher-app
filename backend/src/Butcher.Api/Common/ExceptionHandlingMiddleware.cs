using Butcher.Api.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Butcher.Api.Common;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var (statusCode, title) = exception switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Ressource introuvable"),
                ConflictException => (StatusCodes.Status409Conflict, "Conflit"),
                BadRequestException => (StatusCodes.Status400BadRequest, "Requête invalide"),
                _ => (StatusCodes.Status500InternalServerError, "Erreur interne"),
            };

            var isUnexpected = statusCode == StatusCodes.Status500InternalServerError;

            if (isUnexpected)
            {
                logger.LogError(exception, "Erreur non gérée pendant le traitement de la requête");
            }

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                // Le détail des erreurs inattendues (500) n'est jamais renvoyé au client : il est seulement loggué,
                // pour ne pas exposer de détails internes (message d'exception, stack, etc.).
                Detail = isUnexpected ? "Une erreur inattendue est survenue." : exception.Message,
            };

            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}
