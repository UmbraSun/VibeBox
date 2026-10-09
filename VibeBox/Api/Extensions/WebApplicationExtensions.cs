using Api.Hubs;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigurePipeline(
        this WebApplication app)
    {
        app.UseExceptionHandling();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseCors("ReactClient");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHub<CallSignalingHub>("/hubs/call-signaling");

        return app;
    }

    public static async Task ApplyDatabaseMigrationsAsync(
        this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}