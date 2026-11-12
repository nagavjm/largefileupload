using FluentValidation;
using LargeFileUpload.Application.Uploads.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LargeFileUpload.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IDirectUploadService, DirectUploadService>();
        services.AddScoped<IChunkedUploadService, ChunkedUploadService>();
        services.AddScoped<IUploadSessionService, UploadSessionService>();

        return services;
    }
}
