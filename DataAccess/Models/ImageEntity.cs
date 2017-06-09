using Microsoft.WindowsAzure.StorageClient;

namespace AzureCloudRocks.CodeSamples.Album.DataAccess.Models
{
    public class ImageItemEntity : TableServiceEntity
    {
        public string BlobUri { get; set; }
        public string ThumbnailUri { get; set; }
        public string DziUri { get; set; }
    }

    
}
