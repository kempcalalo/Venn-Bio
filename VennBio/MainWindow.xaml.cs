using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Loader;
using System.Diagnostics;
using System.Configuration;
using SharedUtilities;
using Google.Apis.Storage.v1;
using Google.Apis.Upload;
using System.Windows.Threading;

namespace VennBio
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private string ProteoWizardApp = ConfigurationManager.AppSettings["ProteoWizardFilePath"];
        private string CompressedFile;
        private string ConvertedFile;

        public MainWindow()
        {
            InitializeComponent();

        }

        private void Compress_Click(object sender, EventArgs e)
        {
            try
            {
                if (!String.IsNullOrEmpty(FolderToZip.DirectoryPath))
                {
                    using (var myDialog = new SaveFileDialog())
                    {
                        myDialog.Filter = "Zip Files|*.zip";
                        DialogResult result = myDialog.ShowDialog();
                        if (result == System.Windows.Forms.DialogResult.OK)
                        {
                            ZipFile.CreateFromDirectory(FolderToZip.DirectoryPath, myDialog.FileName);
                            System.Windows.Forms.MessageBox.Show("ZIP file created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            FolderToConvert.IsEnabled = true;
                            ConvertButton.IsEnabled = true;
                            CompressedFile = myDialog.FileName;
                        }
                    }
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Choose a folder to zip first", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch(UnauthorizedAccessException ex)
            {
                System.Windows.Forms.MessageBox.Show("Write access to target folder is denied!", "Error Saving", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, "Error Saving", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void Convert_Click(object sender, EventArgs e)
        {
            var outPutDir = "";
            try
            {
                if (!String.IsNullOrEmpty(FolderToConvert.DirectoryPath))
                {
                    using (var myDialog = new FolderBrowserDialog())
                    {
                        myDialog.ShowDialog();
                        outPutDir = myDialog.SelectedPath;
                        ConvertedFile = GetConvertedFileName(FolderToConvert.DirectoryPath, outPutDir);
                        EnableUI(false);

                        BackgroundWorker bgw = new BackgroundWorker();
                        bgw.DoWork += bgw_DoWork;
                        bgw.RunWorkerCompleted += bgw_Completed;
                        var args = SetArguments(ProteoWizardApp, FolderToConvert.DirectoryPath, outPutDir);
                        bgw.RunWorkerAsync(args);
                        
                    }
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Choose a folder to convert first", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Windows.Forms.MessageBox.Show("Write access to target folder is denied!", "Error Saving", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, "Error Saving", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private string GetConvertedFileName(string directoryPath, string outPutDir)
        {
            string fileName = System.IO.Path.GetFileName(directoryPath.TrimEnd(System.IO.Path.DirectorySeparatorChar)).Replace(".d","");
            return $"{outPutDir}\\{fileName}.mzXML";
        }

        private void Upload_Click(object sender, EventArgs e)
        {
            var scopes = new[] { @"https://www.googleapis.com/auth/devstorage.full_control" };
            var bucketName = "kemp-hiring-vennbio";
            var token = GoogleCloudHelper.GetAccessTokenFromJSONKey(scopes);

            //ProgressBar.IsEnabled = true;

            //Upload Zip
            Upload(bucketName, token, CompressedFile, "application/zip");
            //Upload XML fd
            Upload(bucketName, token, ConvertedFile, "application/xml");

        }

        private List<string> SetArguments(string filePath, string inputFolderPath,string outPutFolder)
        {
            List<string> arguments = new List<string>();
                    arguments.Add(filePath);
                    arguments.Add(inputFolderPath);
                    arguments.Add("--srmAsSpectra");
                    arguments.Add("--mzXML");
                    arguments.Add("-o");
                    arguments.Add(outPutFolder);
            return arguments;
        }

        void bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            Process p = new Process();
            var args = e.Argument as List<string>;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.StartInfo.FileName = args[0];
            p.StartInfo.Arguments = $"{args[1]} {args[2]} {args[3]} {args[4]} {args[5]}";
            p.Start();
            p.WaitForExit(int.MaxValue);
        }

        void bgw_Completed(object sender, RunWorkerCompletedEventArgs e)
        {            
            ((BackgroundWorker)sender).Dispose();
            EnableUI(true);
            UploadButton.IsEnabled = true;
        }

        private void EnableUI(bool isEnabled)
        {
            FolderToConvert.IsEnabled = isEnabled;
            FolderToZip.IsEnabled = isEnabled;
            ConvertButton.IsEnabled = isEnabled;
            CompressButton.IsEnabled = isEnabled;  

            if (isEnabled)
                Throbber1.Visibility = Visibility.Collapsed;
            else
                Throbber1.Visibility = Visibility.Visible;

        }

        public async Task<bool> UploadToGoogleCloudStorage(string bucketName, string token, string filePath, string contentType)
        {
            var newObject = new Google.Apis.Storage.v1.Data.Object()
            {
                Bucket = bucketName,
                Name = System.IO.Path.GetFileNameWithoutExtension(filePath)
            };
            var service = new Google.Apis.Storage.v1.StorageService();

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open))
                {

                    var uploadRequest = new ObjectsResource.InsertMediaUpload(service, newObject, bucketName, fileStream, contentType);
                    uploadRequest.OauthToken = token;
                    uploadRequest.ProgressChanged += UploadProgress;
                    uploadRequest.ChunkSize = (256 * 1024);
                    await uploadRequest.UploadAsync().ConfigureAwait(false);
                    service.Dispose();

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
            return true;
        }
        public bool Upload(string bucketName, string token, string filePath, string contentType)
        {
            return UploadToGoogleCloudStorage(bucketName, token, filePath, contentType).Result;
        }

        private void UploadProgress(IUploadProgress progress)
        {
            switch (progress.Status)
            {
                case UploadStatus.Starting:
                    //ProgressBar.Minimum = 0;
                    //ProgressBar.Value = 0;
                    break;
                case UploadStatus.Completed:
                    System.Windows.MessageBox.Show("Upload completed!");
                    break;
                case UploadStatus.Uploading:
                    Console.WriteLine(progress.BytesSent);
                    break;
                case UploadStatus.Failed:
                    Console.WriteLine("Upload failed "
                                + Environment.NewLine
                                + progress.Exception.Message
                                + Environment.NewLine
                                + progress.Exception.StackTrace
                                + Environment.NewLine
                                + progress.Exception.Source
                                + Environment.NewLine
                                + progress.Exception.InnerException
                                + Environment.NewLine
                                + "HR-Result" + progress.Exception.HResult);
                    break;
            }
        }

        //private void UpdateProgressBar(long value)
        //{
        //    Dispatcher.Invoke(() => { this.ProgressBar.Value = value; });
        //}
    }
}
