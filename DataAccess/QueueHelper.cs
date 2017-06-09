using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System;

namespace AzureCloudRocks.CodeSamples.Album.DataAccess
{
    public class QueueHelper : IQueueHelper
    {
        private CloudQueueClient _queueStorage;

        public QueueHelper(CloudStorageAccount storageAccount)
        {
            _queueStorage = storageAccount.CreateCloudQueueClient();
        }

        public void SendMessage(string queueName, string message)
        {
           _queueStorage.RetryPolicy = RetryPolicies.Retry( 3, TimeSpan.FromSeconds( 5 ) ); 
           _queueStorage.GetQueueReference(queueName).AddMessage(new CloudQueueMessage(message));
        }
    }
}