using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace WslSdk.Sample
{
    public partial class MainForm : Form
    {
        public MainForm()
            : base()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (backgroundWorker.IsBusy)
                return;

            backgroundWorker.RunWorkerAsync("ls /");
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var command = (e.Argument as string) ?? string.Empty;

            backgroundWorker.ReportProgress(30, "Creating WslSdk.WslService");
            var wslServiceType = Type.GetTypeFromProgID("WslSdk.WslService");
            dynamic wslService = Activator.CreateInstance(wslServiceType);

            backgroundWorker.ReportProgress(50, "Generating Random Name");
            var randomName = wslService.GenerateRandomName(true);
            var busyboxRootfsFile = Path.GetFullPath("busybox.tgz");
            var tempDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WslSdkTest", randomName);

            if (!Directory.Exists(tempDirectory))
            {
                backgroundWorker.ReportProgress(60, "Creating Temporary Directory");
                Directory.CreateDirectory(tempDirectory);
            }

            backgroundWorker.ReportProgress(70, $"Registering Busybox Distro: {randomName}");
            wslService.RegisterDistro(randomName, busyboxRootfsFile, tempDirectory);
            try
            {
                backgroundWorker.ReportProgress(80, $"Running Command: {command}");
                var res = (string)wslService.RunWslCommand(randomName, command);
                e.Result = res;
            }
            finally
            {
                backgroundWorker.ReportProgress(90, $"Unregistering Busybox Distro: {randomName}");
                wslService.UnregisterDistro(randomName);

                backgroundWorker.ReportProgress(100, "Completed");
            }
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string res;

            if (e.Cancelled)
                res = "Cancelled.";
            else if (e.Error != null)
            {
                Exception actualException = e.Error as AggregateException;
                if (actualException != null)
                    actualException = actualException.InnerException;
                else
                    actualException = e.Error;
                res = actualException.ToString();
            }
            else
                res = e.Result as string;

            if (string.IsNullOrWhiteSpace(res))
                res = "(Result Unknown)";

            webBrowser.DocumentText = $"<pre>{WebUtility.HtmlEncode(res)}</pre>";
        }

        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            statusLabel.Visible = (e.ProgressPercentage < 100);
            statusLabel.Text = $"Status: {e.UserState as string}";
        }
    }
}
