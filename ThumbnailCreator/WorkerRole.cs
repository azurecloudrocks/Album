using AzureCloudRocks.CodeSamples.Album.DataAccess;
using AzureCloudRocks.CodeSamples.Album.DataAccess.Models;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AzureCloudRocks.CodeSamples.Album.ThumbnailCreator
{
    public class WorkerRole : RoleEntryPoint
    {
        private CloudStorageAccount storageAccount;
        private string _imageContainer;
        private string _thumbnailContainer;
        private string _queueName;
        int _sleepTime;
        int _poisonDequeueLimit;
        string _dziBlobUri;
        string _thumbBlobUri;

        public WorkerRole()
        {
            CloudStorageAccount.SetConfigurationSettingPublisher((configName, configSetter) =>
            {
                configSetter(RoleEnvironment.GetConfigurationSettingValue(configName));

                RoleEnvironment.Changed += (sender, arg) =>
                {
                    if (arg.Changes.OfType<RoleEnvironmentConfigurationSettingChange>()
                        .Any((change) => (change.ConfigurationSettingName == configName)))
                    {
                        if (!configSetter(RoleEnvironment.GetConfigurationSettingValue(configName)))
                        {
                            RoleEnvironment.RequestRecycle();
                        }
                    }
                };
            });

            this.storageAccount = CloudStorageAccount.FromConfigurationSetting("DataConnectionString");
        }

        public override bool OnStart()
        {
            Trace.TraceInformation("Worker role started");
            ServicePointManager.DefaultConnectionLimit = 12;
            RoleEnvironment.Changing += this.RoleEnvironmentChanging;
            return base.OnStart();
        }

        public override void Run()
        {
            Trace.TraceInformation("Inside worker role method");
            this._imageContainer = RoleEnvironment.GetConfigurationSettingValue("ImageBlobContainer");
            this._thumbnailContainer = RoleEnvironment.GetConfigurationSettingValue("ThumbnailBlobContainer");
            this._queueName = RoleEnvironment.GetConfigurationSettingValue("ImageQueue");
            this._poisonDequeueLimit = Convert.ToInt32(RoleEnvironment.GetConfigurationSettingValue("PoisonDequeueLimit"));
            this._sleepTime = GetSleepTimeFromConfig();
            PollQueue();
        }


        private void PollQueue()
        {
            Trace.TraceInformation("Inside Poll queue method");
            while (true)
            {
                Thread.Sleep(_sleepTime);
                Trace.TraceInformation("Poll queue method called after " + _sleepTime);
                try
                {
                    var queueClient = this.storageAccount.CreateCloudQueueClient();
                    var q = queueClient.GetQueueReference(_queueName);
                    Trace.TraceInformation("Message count in the queue");
                    var msg = q.GetMessage();

                    var success = false;

                    if (msg != null)
                    {
                        if (msg.DequeueCount > _poisonDequeueLimit)
                        {
                            q.DeleteMessage(msg);// Alternative: Move to another queue for investigation - DeadLetter Queue
                            continue;
                        }

                        var id = msg.Id;
                        success = this.ProcessMessage(msg);

                        if (success)
                        {
                            q.DeleteMessage(msg);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            }
        }

        private static int GetSleepTimeFromConfig()
        {
            int sleepTime;

            if (!int.TryParse(RoleEnvironment.GetConfigurationSettingValue("WorkerSleepTime"), out sleepTime))
            {
                sleepTime = 0;
            }

            if (sleepTime < 1000)
            {
                sleepTime = 2000;
            }

            return sleepTime;
        }

        private void RoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
            if (e.Changes.Any(change => change is RoleEnvironmentConfigurationSettingChange))
            {
                e.Cancel = true;
            }
        }

        private bool ProcessMessage(CloudQueueMessage msg)
        {
            Trace.TraceInformation("Processing message: {0}", msg.AsString);
            var parts = msg.AsString.Split('|');

            if (parts.Length != 3)
            {
                Trace.TraceError("Invalid input ", msg.AsString);
                return false;
            }

            var albumName = parts[0];
            var type = parts[1];
            var imageUri = parts[2];

            IEntityRepository<ImageItemEntity> imageRepository = new EntityRepository<ImageItemEntity>(this.storageAccount, "images");

            try
            {
                var photos = imageRepository.GetEntities(item => item.PartitionKey == albumName && item.RowKey == type);

                foreach (var photo in photos)
                {
                    Trace.TraceInformation("Processing {0} in photos", photo.RowKey);

                    var client = this.storageAccount.CreateCloudBlobClient();
                    var container = client.GetContainerReference(_imageContainer);

                    var dziTempDir = RoleEnvironment.GetLocalResource("DZITemp").RootPath;
                    // clean up from previous iterations
                    foreach (var dirname in Directory.GetDirectories(dziTempDir))
                    {
                        Directory.Delete(dirname, true);
                    }

                    //Dir to store downloaded original image.
                    Directory.CreateDirectory(string.Format(@"{0}\images", dziTempDir));
                    //Output dir to dump DZI content.
                    Directory.CreateDirectory(string.Format(@"{0}\output", dziTempDir));
                    var webclient = new WebClient();
                    var imagePath = string.Format(@"{0}\images\{1}_original{2}", dziTempDir, Path.GetFileNameWithoutExtension(imageUri), Path.GetExtension(imageUri));
                    webclient.DownloadFile(imageUri, imagePath);
                    //Correct color palette - It's throwing error when color palette information is corrupted.
                    var correctImagePath = string.Format(@"{0}\images\{1}{2}", dziTempDir, Path.GetFileNameWithoutExtension(imageUri), Path.GetExtension(imageUri));

                    using (var bitmap = Bitmap.FromFile(imagePath, false))
                    {
                        bitmap.Save(correctImagePath);
                    }

                    CreateThumb(correctImagePath);
                    Trace.TraceInformation("Created thumbnail for {0}.", imageUri);
                    CreateDziContent(correctImagePath, dziTempDir + @"\output\" + Path.GetFileNameWithoutExtension(correctImagePath));
                    Trace.TraceInformation("Created DZI for {0}.", imageUri);
                    UploadDirectoryRecursive(dziTempDir + @"\output", container);
                    Trace.TraceInformation("Uploaded DZI content for {0}.", imageUri);
                    photo.ThumbnailUri = _thumbBlobUri;
                    photo.DziUri = _dziBlobUri;
                    imageRepository.InsertOrUpdate(photo);
                    return true;
                }

            }
            catch (Exception ex)
            {
                Trace.TraceError("Error for Album:{0} Url:{1}. Exception:{2}", albumName, imageUri, ex.ToString());
                return false;
            }

            return false;
        }

        private void CreateThumb(string sourcefile)
        {
            int thumbnailWidth = 150;
            int thumbnailHeight = 150;
            System.Drawing.Image thumb = null;

            using (var image = System.Drawing.Image.FromFile(sourcefile))
            {

                int width;
                int height;
                if (image.Width > image.Height)
                {
                    width = thumbnailWidth;
                    height = thumbnailHeight * image.Height / image.Width;
                }
                else
                {
                    height = thumbnailHeight;
                    width = thumbnailWidth * image.Width / image.Height;
                }

                thumb = image.GetThumbnailImage(width, height, () => false, IntPtr.Zero);
            }

            string fileName = Path.GetDirectoryName(sourcefile) + @"\thumbnail-" + Path.GetFileName(sourcefile);
            thumb.Save(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);
            var client = this.storageAccount.CreateCloudBlobClient();
            var thumbContainer = client.GetContainerReference(_thumbnailContainer);
            var thumbBlob = thumbContainer.GetBlobReference("thumbnail-" + Path.GetFileName(sourcefile));
            UploadFile(fileName, thumbBlob);
            _thumbBlobUri = thumbBlob.Uri.ToString();

        }

        private void CreateDziContent(string sourceImagePath, string tempOutputDirectory)
        {
            Microsoft.DeepZoomTools.ImageCreator collectionCreator = new Microsoft.DeepZoomTools.ImageCreator();
            collectionCreator.TileSize = 256;
            collectionCreator.TileFormat = Microsoft.DeepZoomTools.ImageFormat.Jpg;
            collectionCreator.ImageQuality = 1.0;
            collectionCreator.TileOverlap = 0;
            collectionCreator.Create(sourceImagePath, tempOutputDirectory);
        }

        private void UploadDirectoryRecursive(string path, CloudBlobContainer container)
        {
            string xmlPath = null;

            // use 16 threads to upload
            Parallel.ForEach(EnumerateDirectoryRecursive(path),
                new ParallelOptions { MaxDegreeOfParallelism = 16 },
                (file) =>
                {
                    if (Path.GetExtension(file) == ".xml")
                    {
                        xmlPath = file;
                    }
                    else
                    {
                        // upload each file, using the relative path as a blob name
                        UploadFile(file, container.GetBlobReference(Path.GetFullPath(file).Substring(path.Length)));
                    }
                });

            // finish up with the xml itself
            if (xmlPath != null)
            {
                CloudBlob dziblob = container.GetBlobReference(Path.GetFileName(xmlPath));
                UploadFile(xmlPath, dziblob);
                _dziBlobUri = dziblob.Uri.ToString();
            }
        }

        private IEnumerable<string> EnumerateDirectoryRecursive(string root)
        {
            foreach (var file in Directory.GetFiles(root))
                yield return file;
            foreach (var subdir in Directory.GetDirectories(root))
                foreach (var file in EnumerateDirectoryRecursive(subdir))
                    yield return file;
        }

        private void UploadFile(string filename, CloudBlob blob)
        {
            var extension = Path.GetExtension(filename).ToLower();
            if (extension == ".xml")
            {
                // cache XML for 30 minutes
                blob.Properties.CacheControl = "max-age=1800";
            }
            else
            {
                // cache everything else (images) for 2 hours
                blob.Properties.CacheControl = "max-age=7200";
            }
            switch (extension)
            {
                case ".xml":
                case ".dzi":
                    blob.Properties.ContentType = "application/xml";
                    break;
                case ".jpg":
                    blob.Properties.ContentType = "image/jpeg";
                    break;
            }
            blob.UploadFile(filename);
        }
    }
}

