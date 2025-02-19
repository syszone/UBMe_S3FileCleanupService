using S3FileCleanup.Shared.Helpers;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

public class FileDeletionService
{
    private readonly ApiClient _apiClient;
    private readonly S3ClientHelper _s3ClientHelper;
    private readonly ILogger<FileDeletionService> _logger;
    private readonly string _bucketName;
    private readonly string _basePath;
    private readonly string _rootPath;
    private readonly string _getFilesToDeleteEndpoint;
    private readonly string _markFileAsDeletedEndpoint;
    private readonly string _deleteS3FileEndpoint;

    public FileDeletionService(ApiClient apiClient, S3ClientHelper s3ClientHelper, ILogger<FileDeletionService> logger, IConfiguration configuration)
    {
        _apiClient = apiClient;
        _s3ClientHelper = s3ClientHelper;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        // Retrieve settings from configuration
        // Retrieve settings from configuration
        var apiSettings = configuration.GetSection("ApiSettings");
        _bucketName = configuration["S3Settings:BucketName"] ?? throw new ArgumentNullException("BucketName configuration is missing.");
        _basePath = configuration["S3Settings:BasePath"] ?? throw new ArgumentNullException("BasePath configuration is missing.");
        _rootPath = configuration["S3Settings:RootPath"] ?? throw new ArgumentNullException("RootPath configuration is missing.");
        _getFilesToDeleteEndpoint = apiSettings["Endpoints:GetFilesToDelete"] ?? throw new ArgumentNullException("GetFilesToDelete endpoint is missing.");
        _markFileAsDeletedEndpoint = apiSettings["Endpoints:MarkFileAsDeleted"] ?? throw new ArgumentNullException("MarkFileAsDeleted endpoint is missing.");
        _deleteS3FileEndpoint = apiSettings["Endpoints:DeleteS3File"] ?? throw new ArgumentNullException("DeleteS3File endpoint is missing.");
    }
        

    public async Task DeleteOldFilesAsync()
    {
        _logger.LogInformation("Fetching files to delete...");

        try
        {
            // Construct the full API URL for fetching files to delete
            var getFilesToDeleteUrl = $"{_apiClient.BaseUrl}{_getFilesToDeleteEndpoint}";
            // Construct the API endpoint URL
            var deleteS3FileEndpoint = $"{_apiClient.BaseUrl}{_deleteS3FileEndpoint}";

            // Fetch files to delete from the API
            List<string> files = await _apiClient.GetFilesToDeleteAsync(getFilesToDeleteUrl);

            if (files == null || files.Count == 0)
            {
                _logger.LogInformation("No files to delete.");
                return;
            }

            foreach (var file in files)
            {
                try
                {
                    // Construct the key name (path within the bucket)
                    var keyName = _basePath.Replace("//" + file, "").Replace(_rootPath, "").Replace("\\", "/") + file;
                   
                    _logger.LogInformation("Deleting file: {KeyName} from bucket: {BucketName}", keyName, _bucketName);

                    // Perform the deletion

                    // Call API to delete the file
                    bool success = await _apiClient.DeleteS3FileAsync(deleteS3FileEndpoint, _bucketName, keyName);

                    //await _s3ClientHelper.DeleteFileAsync(_bucketName, keyName);

                    // Mark the file as deleted in the database
                    await MarkFileAsDeletedAsync(file);

                    _logger.LogInformation("Successfully deleted file: {FileName}", file);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while deleting file: {FileName}", file);
                }
            }

            _logger.LogInformation("File cleanup completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during the file cleanup process.");
        }
    }

    private async Task MarkFileAsDeletedAsync(string fileName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                _logger.LogWarning("FileName is null or empty. Skipping the mark-as-deleted operation.");
                return;
            }

            _logger.LogInformation("Marking file as deleted in the database via API: {FileName}", fileName);

            // Construct the API endpoint URL
            var markFileAsDeletedEndpoint = $"{_apiClient.BaseUrl}{_markFileAsDeletedEndpoint}";


            // Call the API to mark the file as deleted
            var success = await _apiClient.MarkFileAsDeletedAsync(markFileAsDeletedEndpoint, fileName);

            if (success)
            {
                _logger.LogInformation("Successfully marked file as deleted: {FileName}", fileName);
            }
            else
            {
                _logger.LogWarning("Failed to mark file as deleted via API: {FileName}", fileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while marking file as deleted: {FileName}", fileName);
        }
    }

}
