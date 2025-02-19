using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace S3FileCleanup.Shared.Helpers
{
    public class S3ClientHelper
    {
        private readonly IAmazonS3 _s3Client;
        private readonly ILogger<S3ClientHelper> _logger;
        private readonly HttpClient _httpClient;

        public S3ClientHelper(IAmazonS3 s3Client, ILogger<S3ClientHelper> logger, HttpClient httpClient)
        {
            _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Deletes a file from S3.
        /// </summary>
        /// <param name="bucketName">The S3 bucket name.</param>
        /// <param name="keyName">The S3 object key (path within the bucket).</param>
        public async Task DeleteFileAsync(string bucketName, string keyName)
        {
            try
            {
                _logger.LogInformation("Attempting to delete file: {KeyName} from bucket: {BucketName}", keyName, bucketName);

                var deleteObjectRequest = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = keyName
                };

                // Perform the delete operation
                await _s3Client.DeleteObjectAsync(deleteObjectRequest);

                
                _logger.LogInformation("Successfully deleted file: {KeyName} from bucket: {BucketName}", keyName, bucketName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {KeyName} from bucket: {BucketName}", keyName, bucketName);
            }
        }

        /// <summary>
        /// Lists all files in the specified S3 bucket.
        /// </summary>
        /// <param name="bucketName">The name of the S3 bucket.</param>
        public async Task ListFilesAsync(string bucketName)
        {
            try
            {
                _logger.LogInformation("Listing files in bucket: {BucketName}", bucketName);

                var request = new ListObjectsV2Request
                {
                    BucketName = bucketName
                };

                var response = await _s3Client.ListObjectsV2Async(request);

                foreach (var obj in response.S3Objects)
                {
                    _logger.LogInformation("File: {Key}, Size: {Size} bytes", obj.Key, obj.Size);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files in bucket: {BucketName}", bucketName);
            }
        }

        /// <summary>
        /// Calls an API to mark a file as deleted in the database.
        /// </summary>
        /// <param name="fileName">The name of the file to mark as deleted.</param>
        public async Task MarkFileAsDeletedAsync(string fileName)
        {
            try
            {
                _logger.LogInformation("Calling API to mark file as deleted: {FileName}", fileName);

                var payload = new { FileName = fileName };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/markAsDeleted", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully marked file as deleted: {FileName}", fileName);
                }
                else
                {
                    _logger.LogWarning("Failed to mark file as deleted: {FileName}. Response: {StatusCode} - {ReasonPhrase}",
                        fileName, response.StatusCode, response.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while marking file as deleted: {FileName}", fileName);
            }
        }
    }
}
