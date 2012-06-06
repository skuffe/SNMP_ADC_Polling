using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.ComponentModel;
using System.Globalization;

namespace SNMP_ADC_Polling
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly BackgroundWorker _worker = new BackgroundWorker();

        public MainWindow()
        {
            InitializeComponent();
            InitializeBackgroundWorker();
            _worker.RunWorkerAsync();
        }

        ///<summary>
        ///Initialization of background worker
        ///</summary>
        private void InitializeBackgroundWorker()
        {
            // Allow worker to report progress.
            this._worker.WorkerReportsProgress = true;
            // Allow cancellation of a running worker.
            this._worker.WorkerSupportsCancellation = true;
            // Add event handler for when RunWorkerAsync() is called - i.e. start of thread.
            this._worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            // Add event handler for when the work has been completed.
            this._worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            // Add event handler for when a progress change occurs.
            this._worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
        }

        ///<summary>
        ///Actual work in backgroundworker thread
        ///</summary>
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            int commlength, miblength, datatype, datalength, datastart;
            string output;
            SNMP conn = new SNMP();
            byte[] response = new byte[1024];

            // Send sysName SNMP request
            response = conn.get("get", "192.168.0.100", "public", "1.3.6.1.3.5.0");
            if (response[0] == 0xff) {
                e.Result = "No response";
                return;
            }

            // If response, get the community name and MIB lengths
            commlength = Convert.ToInt16(response[6]);
            miblength = Convert.ToInt16(response[23 + commlength]);

            // Extract the MIB data from the SNMP response
            datatype = Convert.ToInt16(response[24 + commlength + miblength]);
            datalength = Convert.ToInt16(response[25 + commlength + miblength]);
            datastart = 26 + commlength + miblength;
            output = Encoding.ASCII.GetString(response, datastart, datalength);
            e.Result = output;
            Yield(1000000);
        }

        ///<summary>
        ///Event for when the actual work in the backgroundworker thread has completed.
        ///</summary>
        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null) {
                this.label1.Content = e.Error.Message;
            } else if (e.Cancelled) { // Handling of a cancellation event
                this.label1.Content = "Cancelled";
            } else {
                if (!e.Result.ToString().Contains("response")) {
                    string voltage = (string)e.Result;
                    this.label1.Content = voltage;
                    double max = 3.3;
                    string[] values = voltage.Split('V');
                    this.progressBar1.Value = Math.Floor((double)((double.Parse(values[0], CultureInfo.InvariantCulture) / max * 100)));
                }
                else {
                    this.label1.Content = (string)e.Result;
                }
            }
            _worker.RunWorkerAsync();
        }

        ///<summary>
        ///Progress changed event
        ///</summary>
        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //this.progressBar1.Value = e.ProgressPercentage; // Update the progressbar.
        }

        private void Yield(long ticks)
        {
            // Note: a tick is 100 nanoseconds

            long dtEnd = DateTime.Now.AddTicks(ticks).Ticks;

            while (DateTime.Now.Ticks < dtEnd) {

                this.Dispatcher.Invoke(DispatcherPriority.Background, (DispatcherOperationCallback)delegate(object unused) { return null; }, null);

            }

        }
    }
}
