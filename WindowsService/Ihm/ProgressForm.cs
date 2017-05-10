using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Progress
{
    /// <summary>
    /// Simple progress form.
    /// </summary>
    public partial class ProgressForm : Form
    {
        /// <summary>
        /// Will be passed to the background worker.
        /// </summary>
        public object Argument { get; set; }
        /// <summary>
        /// Background worker's result.
        /// You may also check ShowDialog return value
        /// to know how the background worker finished.
        /// </summary>
        public RunWorkerCompletedEventArgs Result { get; private set; }
        /// <summary>
        /// True if the user clicked the Cancel button
        /// and the background worker is still running.
        /// </summary>
        public bool CancellationPending
        {
            get { return worker.CancellationPending; }
        }
        /// <summary>
        /// Text displayed once the Cancel button is clicked.
        /// </summary>
        public string CancellingText { get; set; }
        /// <summary>
        /// Default status text.
        /// </summary>
        public string DefaultStatusText { get; set; }

        /// <summary>
        /// Delegate for the DoWork event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Contains the event data.</param>
        public delegate void DoWorkEventHandler(ProgressForm sender, DoWorkEventArgs e);
        /// <summary>
        /// Occurs when the background worker starts.
        /// </summary>
        public event DoWorkEventHandler DoWork;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ProgressForm()
        {
            InitializeComponent();

            CancellingText = "Annulation en cours...";

            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += new System.ComponentModel.DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
        }

        private void ProgressForm_Load(object sender, EventArgs e)
        {
            //reset to defaults just in case the user wants to reuse the form
            Result = null;
            buttonCancel.Enabled = true;
            labelStatus.Text = DefaultStatusText;
            //start the background worker as soon as the form is loaded
            worker.RunWorkerAsync(Argument);
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            //notify the background worker we want to cancel
            worker.CancelAsync();
            //disable the cancel button and change the status text
            buttonCancel.Enabled = false;
            labelStatus.Text = CancellingText;
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            //the background worker started
            //let's call the user's event handler
            if (DoWork != null)
                DoWork(this, e);
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //the background worker completed
            //keep the resul and close the form
            Result = e;
            if (e.Error != null)
                DialogResult = DialogResult.Abort;
            else if (e.Cancelled)
                DialogResult = DialogResult.Cancel;
            else
                DialogResult = DialogResult.OK;
            Close();
        }

        BackgroundWorker worker;
    }
}
