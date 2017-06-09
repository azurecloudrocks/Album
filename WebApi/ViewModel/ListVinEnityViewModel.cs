using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AzureCloudRocks.CodeSamples.Album.DataAccess.Models;

namespace AzureCloudRocks.CodeSamples.Album.WebApi.ViewModel
{
   public class ListVinEnityViewModel
   {
      public List<SelectListItem> DropDownList { get; set; }
      public IEnumerable<DataAccess.Models.ImageItemEntity> VinEntity { get; set; }
   }
}