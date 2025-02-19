using Microsoft.Extensions.Configuration;
using System.IO;

namespace S3FileCleanup.Shared.Helpers
{
    public class ConfigurationHelper
    {
        private static IConfiguration _configuration;

        static ConfigurationHelper()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        public static string GetApiBaseUrl() => _configuration["ApiSettings:BaseUrl"];
        public static string GetJwtToken() => _configuration["ApiSettings:JwtToken"];
    }
}
