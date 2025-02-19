using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiClient> _logger;
    public string BaseUrl { get; }
    public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger, IConfiguration configuration)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        // Retrieve BaseUrl from configuration
        BaseUrl = configuration["ApiSettings:BaseUrl"]
                  ?? throw new ArgumentNullException("BaseUrl configuration is missing.");
    }

    /// <summary>
    /// Fetches data from a specified API endpoint.
    /// </summary>
    /// <param name="endpoint">The API endpoint to call.</param>
    public async Task<List<string>> GetFilesToDeleteAsync(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            _logger.LogError("Endpoint is null or empty.");
            throw new ArgumentException("Endpoint must not be null or empty.", nameof(endpoint));
        }

        _logger.LogInformation("Calling API at endpoint: {Endpoint}", endpoint);

        try
        {
            // Construct the full URL using the provided endpoint
            var response = await _httpClient.GetAsync(endpoint);

            // Ensure the response status code is successful
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API call failed. Status Code: {StatusCode}, Reason: {ReasonPhrase}",
                    response.StatusCode, response.ReasonPhrase);
                return new List<string>();
            }

            // Read and log the response content
            var json = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("API Response Content: {Content}", json);

            // Deserialize the JSON into ApiResponseWrapper
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var apiResponse = JsonSerializer.Deserialize<ApiResponseWrapper>(json, options);

            if (apiResponse?.Results?.Data == null)
            {
                Console.WriteLine("Warning: No data returned from API.");
                return new List<string>();
            }

            // Return the list of files from the "Data" property
            return apiResponse.Results.Data;
        }
        catch (HttpRequestException httpEx)
        {
            // Handle HTTP request-specific errors
            _logger.LogError(httpEx, "HTTP Request Error occurred.");
        }
        catch (JsonException jsonEx)
        {
            // Handle JSON deserialization errors
            _logger.LogError(jsonEx, "JSON Deserialization Error occurred.");
        }
        catch (Exception ex)
        {
            // Handle any other generic exceptions
            _logger.LogError(ex, "An unexpected error occurred.");
        }

        // Return an empty list if an error occurred
        return new List<string>();
    }


    /// <summary>
    /// Calls the API to delete an S3 file.
    /// </summary>
    /// <param name="endpoint">The API endpoint for deleting S3 files.</param>
    /// <param name="bucketName">The name of the S3 bucket.</param>
    /// <param name="keyName">The key (path) of the file to be deleted.</param>
    /// <returns>Returns true if the file was deleted successfully, otherwise false.</returns>
    public async Task<bool> DeleteS3FileAsync(string endpoint, string bucketName, string keyName)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            _logger.LogError("DeleteS3File API endpoint is missing.");
            throw new ArgumentException("Endpoint must not be null or empty.", nameof(endpoint));
        }

        if (string.IsNullOrWhiteSpace(bucketName))
        {
            _logger.LogError("BucketName is missing.");
            throw new ArgumentException("BucketName must not be null or empty.", nameof(bucketName));
        }

        if (string.IsNullOrWhiteSpace(keyName))
        {
            _logger.LogError("KeyName is missing.");
            throw new ArgumentException("KeyName must not be null or empty.", nameof(keyName));
        }

        _logger.LogInformation("Calling API to delete S3 file. Endpoint: {Endpoint}, Bucket: {BucketName}, Key: {KeyName}",
            endpoint, bucketName, keyName);

        try
        {
            var payload = new { bucketName, keyName };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully deleted S3 file: {KeyName} from bucket: {BucketName}", keyName, bucketName);
                return true;
            }

            _logger.LogWarning("Failed to delete S3 file. Status Code: {StatusCode}, Reason: {ReasonPhrase}",
                response.StatusCode, response.ReasonPhrase);
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "HTTP Request Error occurred while deleting S3 file: {KeyName}", keyName);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON Serialization Error occurred while sending request to delete S3 file: {KeyName}", keyName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while deleting S3 file: {KeyName}", keyName);
        }

        return false;
    }

    /// <summary>
    /// Marks a file as deleted by calling the specified API endpoint.
    /// </summary>
    /// <param name="endpoint">The API endpoint to call.</param>
    /// <param name="fileToDelete">The name of the file to mark as deleted.</param>
    public async Task<bool> MarkFileAsDeletedAsync(string endpoint, string fileToDelete)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            _logger.LogError("Endpoint is null or empty.");
            throw new ArgumentException("Endpoint must not be null or empty.", nameof(endpoint));
        }

        if (string.IsNullOrWhiteSpace(fileToDelete))
        {
            _logger.LogError("FileName is null or empty.");
            throw new ArgumentException("FileName must not be null or empty.", nameof(fileToDelete));
        }

        _logger.LogInformation("Calling API to mark file as deleted. Endpoint: {Endpoint}, FileName: {FileName}", endpoint, fileToDelete);

        try
        {
            var payload = new { fileToDelete };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully marked file as deleted: {FileName}", fileToDelete);
                return true;
            }

            _logger.LogWarning("Failed to mark file as deleted. Status Code: {StatusCode}, Reason: {ReasonPhrase}",
                response.StatusCode, response.ReasonPhrase);
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "HTTP Request Error occurred while marking file as deleted: {FileName}", fileToDelete);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON Serialization Error occurred while marking file as deleted: {FileName}", fileToDelete);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while marking file as deleted: {FileName}", fileToDelete);
        }

        return false;
    }
}

public class ApiResponseWrapper
{
    [JsonPropertyName("results")]
    public Results Results { get; set; }
}

public class Results
{
    [JsonPropertyName("Data")]
    public List<string> Data { get; set; }

    [JsonPropertyName("Message")]
    public string Message { get; set; }

    [JsonPropertyName("ResponseCode")]
    public bool ResponseCode { get; set; }
}

