using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.PerformanceData;
using System.IO;
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
using System.Windows.Threading;
using Application = System.Windows.Forms.Application;

namespace BXFremoveVertigo
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region setup and shutdown
        DispatcherTimer _timer;
        private int iAutoSeconds = 60;
        public MainWindow()
        {
            InitializeComponent();
            this.Title = "Remove Vertigo Events from BXF Schedules vrs" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            LoadSettings();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += timer_Tick;
        }
        private void LoadSettings()
        {
            tbInDir.Text = ConfigurationManager.AppSettings["InDirectory"];
            tbOutFolder.Text = ConfigurationManager.AppSettings["OutDirectory"];
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);

            config.AppSettings.Settings.Remove("InDirectory");
            config.AppSettings.Settings.Add("InDirectory", tbInDir.Text);
            config.AppSettings.Settings.Remove("OutDirectory");
            config.AppSettings.Settings.Add("OutDirectory", tbOutFolder.Text);

            config.Save(ConfigurationSaveMode.Modified);
        }
#endregion
        private string ConvertFile(string strInFile)
        {
            string strReturn = "None";
            List<string> m_lStr = new List<string>();
            bool blVertigo = false;
            bool blInEvent = false;
            int iCount = 0;
            string strOutFile = tbOutFolder.Text + @"\" + System.IO.Path.GetFileName(strInFile);
            lbStatus.Content = $"Starting to convert {strInFile} to {strOutFile}";
            try
            {
                using (StreamWriter writer = new StreamWriter(strOutFile))
                {
                    string[] lines = System.IO.File.ReadAllLines(strInFile);

                    foreach (string line in lines)
                    {
                        if (line.Contains("<ScheduledEvent"))
                        {
                            blInEvent = true;
                            blVertigo = false;
                            m_lStr.Clear();
                            iCount++;
                        }
                        if (line.Contains("Vertigo")) blVertigo = true;
                        if (line.Contains("</ScheduledEvent"))
                        {
                            blInEvent = false; // note the line with /Schedule is writting in next if statement.
                            if (!blVertigo)  // not writing vertigo events
                            {
                                foreach (string strLine in m_lStr)
                                {
                                    writer.WriteLine(strLine);
                                }
                            }
                        }
                        if (!blInEvent && !blVertigo) writer.WriteLine(line);
                        else m_lStr.Add(line);
                    }
                }
                // Add section to move file
                string strMove = tbInDir.Text + System.IO.Path.DirectorySeparatorChar + @"_Processed";
                lbStatus.Content = $"Trying to move {strInFile} to {strMove}";
                if (!Directory.Exists(strMove)) Directory.CreateDirectory(strMove);
                strMove += System.IO.Path.DirectorySeparatorChar + System.IO.Path.GetFileName(strInFile);
                File.Move(strInFile, strMove);
                lbStatus.Content = $"Moved file to {strMove}";
            }
            catch (Exception ex)
            {
                strReturn = $"Got Error Converting file {ex.Message}\r";
            }
            if(strReturn.Length<6) strReturn = $"Wrote {iCount} events to {strOutFile}\r";
            return strReturn;
        }
        private void btn1File_Click(object sender, RoutedEventArgs e)
        {
            // Need to add action to stop timer
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".xml";
            dlg.Filter = "BXF Files (*.xml)|*.xml|All Files (*.*)|*.*";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                rtbStatus.AppendText( ConvertFile(filename));
            }
            // Need to add action to start timer
        }

        private void btInDir_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "In folder to scan for BXF Schedules";
                fbd.SelectedPath = tbInDir.Text;
                DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    tbInDir.Text = fbd.SelectedPath.ToString();
                }
            }
        }

        private void btOutDir_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Out folder for modified BXF Schedules";
                fbd.SelectedPath = tbOutFolder.Text;
                DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    tbOutFolder.Text = fbd.SelectedPath.ToString();
                }
            }
        }


        private async Task DoScan()
        {
            lbStatus.Content = "Scanning Folder Now";
            try
            {
                DirectoryInfo d = new DirectoryInfo(tbInDir.Text);

                foreach (var file in d.GetFiles("*.xml"))
                {
                    lbStatus.Content = ($"Found {file.FullName} waiting 3 seconds to process for copy to complete\r");
                    await FileDelay(file.FullName);
                    lbStatus.Content = ($"Processing{file.FullName}");
                }
                if (Directory.GetFiles(tbInDir.Text, "*.xml", SearchOption.TopDirectoryOnly).Length == 0) lbStatus.Content = "No more xml files in the monitor directory";
            }
            catch (Exception exc)
            {
                lbStatus.Content = $"While Scanning Error: {exc.Message}";
            }          

        }
        private async void btnScanDir_Click(object sender, RoutedEventArgs e)
        {
            cbAutoScan.IsChecked = false;
            _timer.Stop();
            await DoScan();
        }
        private async Task FileDelay(string strInFile)
        {
            await Task.Delay(3000);
            rtbStatus.AppendText( ConvertFile(strInFile));
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            iAutoSeconds--;
            lbAuto.Content = $"Folder Scan in {iAutoSeconds}";
            if (iAutoSeconds < 1) _timer.Stop();
            if (iAutoSeconds < 1 && cbAutoScan.IsChecked == true) _ = DoAutoScan();
        }
        private async Task DoAutoScan()
        {
            lbAuto.Content = "Scanning Folder";
            await DoScan();
            iAutoSeconds = 60;
            if(cbAutoScan.IsChecked == true) _timer.Start();
        }
        private void cbAutoScan_Checked(object sender, RoutedEventArgs e)
        {
            iAutoSeconds = 60;
            _timer.Start();
        }

        private void cbAutoScan_Unchecked(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            lbAuto.Content = "(Automatic Scan Off)";
        }
    }
}
