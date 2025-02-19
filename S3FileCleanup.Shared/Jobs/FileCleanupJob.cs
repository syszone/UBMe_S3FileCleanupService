using Quartz;
using S3FileCleanup.Shared.Helpers;
using System;
using System.Threading.Tasks;

namespace S3FileCleanup.Shared.Jobs
{
    public class FileCleanupJob : IJob
    {
        private readonly FileDeletionService _fileDeletionService;

        public FileCleanupJob(FileDeletionService fileDeletionService)
        {
            _fileDeletionService = fileDeletionService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("File cleanup job started...");
            try
            {
                // Replace with the actual API URL
                await _fileDeletionService.DeleteOldFilesAsync();
                Console.WriteLine("File cleanup job completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during file cleanup job: {ex.Message}");
            }
        }
    }
}
