﻿using System.Net;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema;
using NJsonSchema.Generation.TypeMappers;
using NSwag;
using NSwag.Generation.Processors.Security;

namespace GrayMint.Common.Swagger;

public static class GrayMintSwaggerExtension
{
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    public static IServiceCollection AddGrayMintSwagger(this IServiceCollection services, string title, bool addVersioning)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerDocument(configure =>
        {
            configure.Title = title;

            configure.TypeMappers = new List<ITypeMapper>
            {
                new PrimitiveTypeMapper(typeof(IPAddress), s => { s.Type = JsonObjectType.String; }),
                new PrimitiveTypeMapper(typeof(IPEndPoint), s => { s.Type = JsonObjectType.String; }),
                new PrimitiveTypeMapper(typeof(Version), s => { s.Type = JsonObjectType.String; }),
            };

            configure.OperationProcessors.Add(new OperationSecurityScopeProcessor("Bearer"));
            configure.AddSecurity("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = OpenApiSecuritySchemeType.ApiKey,
                In = OpenApiSecurityApiKeyLocation.Header,
                Description = "Type into the text-box: Bearer YOUR_JWT"
            });
        });

        // Version
        if (addVersioning)
        {
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            });

            services.AddVersionedApiExplorer(options =>
            {
                // ReSharper disable once StringLiteralTypo
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });
        }

        return services;

    }

    public static void UseGrayMintSwagger(this WebApplication webApplication)
    {
        webApplication.UseOpenApi();
        webApplication.UseSwaggerUi3();
    }
}