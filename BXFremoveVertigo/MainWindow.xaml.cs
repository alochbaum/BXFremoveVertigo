using System;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BXFremoveVertigo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnDoIt_Click(object sender, RoutedEventArgs e)
        {
            List<string> m_lStr = new List<string>();
            bool blVertigo = false;
            bool blInEvent = false;
            int iCount = 0;
            string strTemp = tbOutFolder.Text + @"\" +System.IO.Path.GetFileName(tbInFile.Text);
            using (StreamWriter writer = new StreamWriter(strTemp))
            {
                string[] lines = System.IO.File.ReadAllLines(tbInFile.Text);

                foreach (string line in lines)
                {
                    if (line.Contains("<ScheduledEvent"))
                    {
                        blInEvent = true;
                        blVertigo = false;
                        m_lStr.Clear();
                        rtbStatus.AppendText(line + "\r\n");
                        iCount++;
                    }
                    if (line.Contains("Vertigo")) blVertigo = true;
                    if(line.Contains("</ScheduledEvent"))
                    {
                        blInEvent = false; // note the line with /Schedule is writting in next if statement.
                        if (!blVertigo)  // not writing vertigo events
                        {
                            foreach(string strLine in m_lStr)
                            {
                                writer.WriteLine(strLine);
                            }
                        }
                    }
                    if (!blInEvent&&!blVertigo) writer.WriteLine(line);
                    else m_lStr.Add(line);
                }
            }
            rtbStatus.AppendText($"Wrote {iCount} events");
        }

        private void btInDir_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
