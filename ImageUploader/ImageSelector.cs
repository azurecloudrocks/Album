using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Windows.Forms;
namespace AzureCloudRocks.CodeSamples.Album.ImageUploader
{
    public partial class ImageSelector : Form
    {
        public ImageSelector()
        {
            InitializeComponent();
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            var result = openFileDialogForImage.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                FileNameTextBox.Text = openFileDialogForImage.FileName;
                FileNameTextBox.ReadOnly = true;
                BrowseButton.Enabled = false;
                UploadProgressBar.Minimum = 0;
                UploadProgressBar.Maximum = 100;
                int count = 0;
                var uri = UploadMediaItemAndGetBlobUriResponse(openFileDialogForImage.FileName);
                StoreTableItem("GSE-", Path.GetFileNameWithoutExtension(openFileDialogForImage.FileName).ToUpperInvariant(), uri);
                while (count < 100)
                {
                    UploadProgressBar.Value = count;
                    count++;
                    System.Threading.Thread.Sleep(600);
                }

                FileNameTextBox.ReadOnly = false;
                BrowseButton.Enabled = true;
            }
        }

        static string ServiceUrl = ConfigurationManager.AppSettings["ServiceURL"];
        public static string GetSharedAccessSignature(string uri)
        {
            using (var client = new WebClient())
            {
                var response = client.DownloadString(uri);
                return response;
            }
        }

        public static string UploadMediaItemAndGetBlobUriResponse(string fileName)
        {
            string uri = string.Format("{0}sharedaccesssignature?blobName={1}", ServiceUrl, System.IO.Path.GetFileName(fileName));
            // get the SAS for the blob item
            var sas = GetSharedAccessSignature(uri);
            // save the item and get a blobUri response
            var blobUri = SaveMediaItem(fileName, sas);
            return blobUri;
        }

        public static string SaveMediaItem(string file, string sasUrl)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(sasUrl);
            request.Method = "PUT";
            request.Headers.Add("x-ms-blob-type", "BlockBlob");
            using (Stream requestStream = request.GetRequestStream())
            {
                var content = System.IO.File.ReadAllBytes(file);
                requestStream.Write(content, 0, content.Length);
            }
            using (HttpWebResponse resp = (HttpWebResponse)request.GetResponse())
            {
                var uri = new Uri(sasUrl);
                return uri.OriginalString.Replace(uri.Query, string.Empty);
            }
        }

        public static string GetContentTypeFromFileName(string file)
        {
            var extension = System.IO.Path.GetExtension(file).ToLowerInvariant();
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".gif":
                    return "image/gif";
                case ".bmp":
                    return "image/bmp";
                default:
                    return "application/octet-stream";
            }
        }

        public static string StoreTableItem(string albumName, string pictureType, string blobUri)
        {
            using (var client = new WebClient())
            {
                string json =
                    "{\"PartitionKey\":\"" + albumName + DateTime.Now.Date.Ticks + "\"," +
                     "\"RowKey\":\"" + pictureType + "\"," +
                     "\"BlobUri\":\"" + blobUri + "\"}";
                client.Headers.Add("Content-Type", "application/json");
                client.Headers.Add("Accept", "application/json");
                string url = ServiceUrl + "imagebloburis";
                var response = client.UploadString(url, json);
                return response;
            }
        }
    }
}
