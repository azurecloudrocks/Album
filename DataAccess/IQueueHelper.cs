namespace AzureCloudRocks.CodeSamples.Album.DataAccess
{
    public interface IQueueHelper
    {
        void SendMessage(string queueName, string message);
    }
}