namespace AzureCloudRocks.CodeSamples.Album.DataAccess
{
    public interface IBlobHelper
    {
        string GenerateSharedAccessSignature(string blobName, string containerName, string policyName);
        
        void SetContainerPolicy(string containerName, string containerPolicyName);
    }
}