using Microsoft.Extensions.DependencyInjection;
using Amazon.S3;
using S3FileCleanup.Shared.Helpers;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;

var services = ConfigureServices();
var serviceProvider = services.BuildServiceProvider();

// Resolve the FileDeletionService
var fileDeletionService = serviceProvider.GetRequiredService<FileDeletionService>();

Console.WriteLine("Starting manual cleanup...");

// Replace the URL with your actual API endpoint
//string apiUrl = "http://localhost:62476/MenumamaService.svc/EventMgmt/GetFilesToDelete";

try
{
    // Trigger the cleanup process
    //await fileDeletionService.DeleteOldFilesAsync(apiUrl);
    await fileDeletionService.DeleteOldFilesAsync();
    Console.WriteLine("Cleanup completed successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error during cleanup: {ex.Message}");
}

static IServiceCollection ConfigureServices()
{
    var services = new ServiceCollection();

    // Load configuration
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();

    services.AddSingleton<IConfiguration>(configuration);

    // Configure HttpClient with JWT Token
    services.AddHttpClient<ApiClient>((provider, client) =>
    {
        var configuration = provider.GetRequiredService<IConfiguration>();
        var baseUrl = configuration["ApiSettings:BaseUrl"];
        var jwtToken = configuration["ApiSettings:JwtToken"];

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentNullException("ApiSettings:BaseUrl is missing in appsettings.");

        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", jwtToken);
    });

    // Register shared services
    services.AddHttpClient<ApiClient>();
    services.AddSingleton<IAmazonS3>(new AmazonS3Client(Amazon.RegionEndpoint.USWest2));
    services.AddSingleton<S3ClientHelper>();
    services.AddSingleton<FileDeletionService>();

    return services;
}
