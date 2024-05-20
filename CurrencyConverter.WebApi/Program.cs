using CurrencyConverter.DTO.CurrencyConverter.Request;
using CurrencyConverter.DTO.CurrencyConverter.Validator;
using CurrencyConverter.Services.CurrencyConverter.Interface;
using CurrencyConverter.Services.CurrencyConverter;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.Configuration;
using CurrencyConverter.DTO.Shared;
using System.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using AspNetCoreRateLimit;
using CurrencyConverter.WebApi.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverter.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // =======================Load configuration============================
            var configuration = builder.Configuration;
            configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);

            // Add services to the container
            FluentValidation(builder);

            // =======================Set HTTP Client===============================
            AddHttpClient(builder);

            // =======================Adding Services============================
            RegisterServices(builder, configuration);

            var app = builder.Build();

            // Configure the HTTP request pipeline
            ConfigurePipeline(app);

            ConfigureMiddleware(app);

            app.Run();
        }

        private static void ConfigureMiddleware(WebApplication app)
        {
            // Open it when u have the ssl certificate and change the application url in profile under properties folder
            //app.UseHttpsRedirection();
            app.UseRouting();

            // Add throttling middleware
            app.UseMiddleware<ThrottlingMiddleware>();

            // Use Rate Limiting Middleware
            app.UseIpRateLimiting();

            // Generic Error Global Handling
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.Map("/error", async context =>
                {
                    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                    var errorResponse = new
                    {
                        message = "An unexpected error occurred. Please try again later.",
                        detail = exceptionHandlerPathFeature?.Error.Message // Remove in production for security
                    };

                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    await context.Response.WriteAsJsonAsync(errorResponse);
                });
            });
        }

        private static void ConfigurePipeline(WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                // Use the built-in exception handling middleware
                app.UseExceptionHandler("/error");
                app.UseHsts();
            }
        }

        private static void FluentValidation(WebApplicationBuilder builder)
        {
            _ = builder.Services.AddControllers()
                       .AddFluentValidation(fv =>
                       {
                           _ = fv.RegisterValidatorsFromAssemblyContaining<ConversionRequestValidator>();
                       });
        }

        private static void AddHttpClient(WebApplicationBuilder builder)
        {
            builder.Services.AddHttpClient("Frankfurter", (provider, client) =>
            {
                var settings = provider.GetRequiredService<SettingDto>();
                client.BaseAddress = new Uri(settings.ExchangeApiSettings.FrankfurterBaseUrl);
            });
        }

        private static void RegisterServices(WebApplicationBuilder builder, Microsoft.Extensions.Configuration.ConfigurationManager configuration)
        {
            var settings = (configuration.Get<SettingDto>());
            builder.Services.AddMemoryCache();

            // Register api services and validator
            builder.Services.AddScoped<ICurrencyService, CurrencyService>();
            builder.Services.AddSingleton<SettingDto>(settings);
            builder.Services.AddTransient<IValidator<ConversionRequestDto>, ConversionRequestValidator>();
            builder.Services.AddTransient<IValidator<HistoricalRatesRequestDto>, HistoricalRatesRequestValidator>();

            // Register the required services for rate limiting
            builder.Services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
            builder.Services.Configure<IpRateLimitPolicies>(configuration.GetSection("IpRateLimitPolicies"));
            builder.Services.AddInMemoryRateLimiting();
            builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            // Add Swagger services
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
        }
    }
}