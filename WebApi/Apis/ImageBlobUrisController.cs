using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Http.Tracing;
using AzureCloudRocks.CodeSamples.Album.DataAccess;
using AzureCloudRocks.CodeSamples.Album.DataAccess.Models;

namespace AzureCloudRocks.CodeSamples.Album.WebApi.Apis
{
    public class ImageBlobUrisController : ApiController
    {
        private readonly IEntityRepository<ImageItemEntity> _imageRepository;
        private readonly IQueueHelper _queueHelper;

        public ImageBlobUrisController(IEntityRepository<ImageItemEntity> vinRepository, IQueueHelper queueHelper)
        {
            _imageRepository = vinRepository;
            _queueHelper = queueHelper;
        }

        // GET api/imagewbloburis
        [HttpGet]
        public IEnumerable<ImageItemEntity> Get([FromBody]string vinNumber)
        {
            return _imageRepository.GetEntities(v => v.PartitionKey == vinNumber);
        }

        // POST api/imagebloburis
        [HttpPost]
        public HttpResponseMessage Post(ImageItemEntity imageItemEntity)
        {
           _imageRepository.InsertOrUpdate( imageItemEntity );
           
           _queueHelper.SendMessage( "images", string.Format( "{0}|{1}|{2}", imageItemEntity.PartitionKey, imageItemEntity.RowKey, imageItemEntity.BlobUri ) );
           Trace.TraceInformation( "sent message to the queue for processsing ",string.Format("{0}{1}{2}",imageItemEntity.PartitionKey, imageItemEntity.RowKey, imageItemEntity.BlobUri ));
           return new HttpResponseMessage { Content = new StringContent( "Success" ), StatusCode = HttpStatusCode.OK };
        }
    }
}
