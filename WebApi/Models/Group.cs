using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AzureCloudRocks.CodeSamples.Album.DataAccess.Models;

namespace AzureCloudRocks.CodeSamples.Album.WebApi.Models
{
   public class Group
   {
      public string Key { get; set; }
      public IEnumerable<ImageItemEntity> Values { get; set; }
      public bool Selected { get; set; }
   }


}