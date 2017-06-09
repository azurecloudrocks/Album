using AzureCloudRocks.CodeSamples.Album.DataAccess;
using AzureCloudRocks.CodeSamples.Album.DataAccess.Models;
using Ionic.Zip;
using Microsoft.Azure;
using Microsoft.WindowsAzure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
namespace AzureCloudRocks.CodeSamples.Album.WebApi.Controllers
{
    public class PhotoController : Controller
    {
        private readonly IEntityRepository<ImageItemEntity> _imageRepository;

        public static CloudStorageAccount Account
        {
            get
            {
                return CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));
            }
        }

        public PhotoController(IEntityRepository<ImageItemEntity> vinRepository)
        {
            _imageRepository = vinRepository;
        }

        public ActionResult Index()
        {
            var photoItems = _imageRepository.GetEntities(null).ToList();
            if (photoItems.Count > 0)
            {
                var albumList = photoItems.ToList().Select(vin => vin.PartitionKey).Distinct().ToList();
                return View(albumList);
            }
            else
            {
                return View(new List<string>());
            }
        }

        public ActionResult GetDziContent(string url)
        {
            WebClient client = new WebClient();
            string content = new StreamReader(client.OpenRead(url)).ReadToEnd();
            return Json(new { XmlContent = content }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult List(string id)
        {
            var photoItems = _imageRepository.GetEntities(v => v.PartitionKey == id).ToList();
            ViewData["photoset"] = id;
            return View(photoItems);
        }

        public ActionResult Search(string id)
        {
            var allVinEntities = _imageRepository.GetEntities(v => v.PartitionKey == id).ToList();
            return View("List", allVinEntities);
        }

        [HttpPost]
        public ActionResult Download(FormCollection form)
        {
            string[] photoUrls = form["photo"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (photoUrls.Length > 0)
            {
                string downloadFileName = string.Format("attachment;filename=Photoset_{0}_{1}.zip", form["photosetId"], DateTime.Now.ToString(CultureInfo.InvariantCulture));
                this.Response.Clear();
                this.Response.ContentType = "application/zip";
                this.Response.AddHeader("Content-Disposition", downloadFileName);
                //Using DotNetZip library for compressing.
                using (ZipFile zipFile = new ZipFile())
                {
                    foreach (var file in photoUrls)
                    {
                        string fileName = Path.GetFileName(file);
                        using (MemoryStream stream = new MemoryStream())
                        {
                            //Using web client to get images since blobs are public. 
                            //If we are using private blobs, Azure Storage Library needs to be used.
                            using (System.Net.WebClient client = new System.Net.WebClient())
                            {
                                byte[] imageData = client.DownloadData(file);
                                zipFile.AddEntry(fileName, imageData);
                            }
                        }
                    }

                    zipFile.Save(Response.OutputStream);
                }


            }
            return null;
        }
    }
}
