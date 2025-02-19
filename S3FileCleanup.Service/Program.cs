using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using S3FileCleanup.Shared.Helpers;
using S3FileCleanup.Shared.Jobs;
using Amazon.S3;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using S3FileCleanup.Service;
using Serilog;
using Microsoft.Extensions.Configuration;

class Program
{
    static async Task Main(string[] args)
    {
        // Load configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Configure Serilog for logging
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.Console() // Console logging
            .WriteTo.File("Logs/s3cleanup-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7) // File logging
            .CreateLogger();

        try
        {
            await Host.CreateDefaultBuilder(args)
                .UseWindowsService() // Ensure the service runs as a Windows Service
                .UseSerilog() // Use Serilog for logging
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IConfiguration>(configuration);

                    // Configure HttpClient with JWT Token
                    services.AddHttpClient<ApiClient>((provider, client) =>
                    {
                        var config = provider.GetRequiredService<IConfiguration>();
                        var baseUrl = config["ApiSettings:BaseUrl"];
                        var jwtToken = config["ApiSettings:JwtToken"];

                        if (string.IsNullOrWhiteSpace(baseUrl))
                            throw new ArgumentNullException("ApiSettings:BaseUrl is missing in appsettings.");

                        client.BaseAddress = new Uri(baseUrl);
                        client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", jwtToken);
                    });

                    // Quartz Configuration
                    services.AddSingleton<IJobFactory, SingletonJobFactory>();
                    services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();

                    // Register the job
                    services.AddSingleton<FileCleanupJob>();
                    services.AddSingleton(new JobSchedule(
                        jobType: typeof(FileCleanupJob),
                        cronExpression: "0 0 0 * * ?")); // Run daily at midnight

                    services.AddHostedService<QuartzHostedService>();

                    // Register shared services
                    services.AddSingleton<IAmazonS3>(new AmazonS3Client(Amazon.RegionEndpoint.USWest2));
                    services.AddSingleton<S3ClientHelper>();
                    services.AddSingleton<FileDeletionService>();

                    // Register the Worker service
                    services.AddHostedService<Worker>();
                })
                .Build()
                .RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush(); // Ensure logs are flushed before exiting
        }
    }
}
