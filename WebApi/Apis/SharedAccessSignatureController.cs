
using System.Net;
using System.Net.Http;
using System.Web.Http;
using AzureCloudRocks.CodeSamples.Album.DataAccess;

namespace AzureCloudRocks.CodeSamples.Album.WebApi.Apis
{
    public class SharedAccessSignatureController : ApiController
    {
        private readonly IBlobHelper _helper;

        public SharedAccessSignatureController(IBlobHelper _helper)
        {
            this._helper = _helper;
        }

        [HttpGet]
        public HttpResponseMessage Get(string blobName)
        {
            const string containerName = "images";
            const string containerPolicyName = "demopolicy";

            _helper.SetContainerPolicy(containerName, containerPolicyName);

            string sas = _helper.GenerateSharedAccessSignature(blobName, containerName, containerPolicyName);

            return new HttpResponseMessage
                {
                    Content = new StringContent(sas),
                    StatusCode = HttpStatusCode.OK
                };
        }
    }
}