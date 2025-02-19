using System;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using S3FileCleanup.Shared.Helpers;
using S3FileCleanup.Shared.Jobs;
using Amazon.S3;
using Serilog;

namespace S3FileCleanup.Service
{
    public partial class S3FileCleanupWindowsService : ServiceBase
    {
        private IHost _host;

        public S3FileCleanupWindowsService()
        {
            this.ServiceName = "S3FileCleanupService";
        }

        protected override void OnStart(string[] args)
        {
            Log.Information("S3 File Cleanup Service is starting...");

            try
            {
                // Start the host in a background task
                Task.Run(() => StartHost(args));
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to start the service");
                throw;
            }
        }

        private void StartHost(string[] args)
        {
            _host = CreateHostBuilder(args).Build();
            _host.Run(); // Blocks and runs the host
        }

        protected override void OnStop()
        {
            Log.Information("S3 File Cleanup Service is stopping...");

            if (_host != null)
            {
                _host.StopAsync(TimeSpan.FromSeconds(10)).Wait(); // Gracefully stop the host
                _host.Dispose();
            }

            Log.Information("S3 File Cleanup Service has stopped.");
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Load configuration
                    var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                        .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!)
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .Build();

                    services.AddSingleton(configuration);

                    // Configure HttpClient
                    services.AddHttpClient<ApiClient>((provider, client) =>
                    {
                        var config = provider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
                        client.BaseAddress = new Uri(config["ApiSettings:BaseUrl"]);
                        client.DefaultRequestHeaders.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config["ApiSettings:JwtToken"]);
                    });

                    // Quartz Configuration
                    services.AddSingleton<IJobFactory, SingletonJobFactory>();
                    services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
                    services.AddSingleton<FileCleanupJob>();
                    services.AddSingleton(new JobSchedule(
                        jobType: typeof(FileCleanupJob),
                        cronExpression: "0 0 0 * * ?")); // Run daily at midnight
                    services.AddHostedService<QuartzHostedService>();

                    // Shared Services
                    services.AddSingleton<IAmazonS3>(new AmazonS3Client(Amazon.RegionEndpoint.USWest2));
                    services.AddSingleton<S3ClientHelper>();
                    services.AddSingleton<FileDeletionService>();

                    // Worker Service
                    services.AddHostedService<Worker>();
                })
                .UseSerilog();
        }
    }
}
