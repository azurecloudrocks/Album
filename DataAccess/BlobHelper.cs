using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System;

namespace AzureCloudRocks.CodeSamples.Album.DataAccess
{
    public class BlobHelper : IBlobHelper
    {
        private CloudBlobClient _blobStorage;

        public BlobHelper(CloudStorageAccount storageAccount)
        {
            _blobStorage = storageAccount.CreateCloudBlobClient();
            _blobStorage.RetryPolicy = RetryPolicies.NoRetry();
        }

        public string GenerateSharedAccessSignature(string blobName, string containerName, string policyName)
        {
            try
            {

                CloudBlobContainer blobContainer = _blobStorage.GetContainerReference(containerName);
                blobContainer.CreateIfNotExist();

                BlobContainerPermissions containerPermissions = new BlobContainerPermissions();
                containerPermissions.PublicAccess = BlobContainerPublicAccessType.Blob;

                containerPermissions.SharedAccessPolicies.Add(policyName, new SharedAccessPolicy()
                {
                    Permissions = SharedAccessPermissions.Write, // | SharedAccessPermissions.Read ,
                    SharedAccessExpiryTime = DateTime.Now.Add(TimeSpan.FromHours(4))
                });

                //Anonymous clients can write to the container with a sas
                blobContainer.SetPermissions(containerPermissions);

                string sas = blobContainer.GetSharedAccessSignature(new SharedAccessPolicy(), policyName);

                return string.Format("{0}/{1}{2}", blobContainer.Uri, blobName, sas);
            }
            catch (StorageClientException SCEx)
            {
                throw new ApplicationException(SCEx.Message, SCEx);
            }
            catch
            {
                throw;
            }
        }

        private CloudBlobContainer GetBlobContainer(string containerName)
        {
            return _blobStorage.GetContainerReference(containerName);
        }

        public void SetContainerPolicy(string container, string containerPolicyName)
        {
            BlobContainerPermissions blobPermissions = new BlobContainerPermissions();
            blobPermissions.SharedAccessPolicies.Add(containerPolicyName, new SharedAccessPolicy()
            {
                SharedAccessExpiryTime = DateTime.UtcNow + TimeSpan.FromHours(Convert.ToInt16(24)),
                Permissions = SharedAccessPermissions.Write
            });

            GetBlobContainer(container).SetPermissions(blobPermissions);
        }
    }
}