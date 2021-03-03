using System;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Security;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace MailboxTester
{
    public partial class FrmMain : Form
    {
        #region Global variables

        const string ShellUri = "http://schemas.microsoft.com/powershell/Microsoft.Exchange";
        const string ExchangeServerUri = "https://Exchange2013/powershell"; // URL for remote powershell
        const int Port = 443; // If SSL=true then 5986 else 5985 (By default 443 for HTTPS and 80 for HTTP)

        // For commands
        //const string Exch2013PsSnapinName = "Microsoft.Exchange.Management.PowerShell.SnapIn";
        //const string Identity = "pyates@domain.com";
        //const string Database = "f0ec4f30-9d9d-4fc8-9f93-de7ce38e6ab4";
        //const string DomainController = "DC1.domain.com";

        // For credentials
        //const string AdminUser = @"DOMAIN\Administrator";
        //const string PlainPass = "P@ss99";

        #endregion

        #region Form's event handlers

        public FrmMain()
        {
            InitializeComponent();
        }

        private void btnEnableExchangeMailbox_Click(object sender, EventArgs e)
        {
            btnEnableExchangeMailbox.Enabled =
                txtIdentity.Enabled =
                txtMailboxDatabase.Enabled =
                txtComputerName.Enabled =
                txtUsername.Enabled =
                txtPassword.Enabled =
                txtResult.Enabled = false;
            bgWorker.RunWorkerAsync();

            //try
            //{
            //    EnableMailBoxForES2013();
            //    btnEnableExchangeMailbox.Enabled = true;
            //}
            //catch (Exception exception)
            //{
            //    txtResult.Text = exception.ToString();
            //    btnEnableExchangeMailbox.Enabled = true;
            //}
        }

        private void bgWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            e.Result = EnableMailBox();
        }

        private void bgWorker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            var progStatus = e.UserState.ToString();
            lblStatus.Text = string.Format("{0} ({1}% completed)", progStatus, e.ProgressPercentage);
        }

        private void bgWorker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            btnEnableExchangeMailbox.Enabled =
                txtIdentity.Enabled =
                txtMailboxDatabase.Enabled =
                txtComputerName.Enabled =
                txtUsername.Enabled =
                txtPassword.Enabled =
                txtResult.Enabled = true;

            lblStatus.Text = "Developed by - Vikram Singh Saini";

            if (e.Error != null)
            {
                var builder = new StringBuilder("Error occured: ").Append(Environment.NewLine).Append(Environment.NewLine);
                txtResult.ForeColor = Color.DarkRed;

                if (e.Error.Message.Contains("This task does not support recipients of this type."))
                {
                    builder.Append("Mailbox for identity '").Append(txtIdentity.Text).Append("' is already enabled.");
                    txtResult.Text = builder.ToString();
                }
                else
                {
                    builder.Append(e.Error);
                    txtResult.Text = builder.ToString();
                }

            }
            else
            {
                txtResult.ForeColor = Color.DarkGreen;
                txtResult.Text = (string)e.Result;
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Enable mailbox for Exchange Server 2013.
        /// </summary>
        private string EnableMailBox()
        {
            // 1. Create WSManConnectionInfo
            var progStatus = "Creating WSManConnectionInfo";
            bgWorker.ReportProgress(25, progStatus);

            var wsManConInfo = new WSManConnectionInfo((new Uri(ExchangeServerUri)), ShellUri, Credential());
            wsManConInfo.AuthenticationMechanism = AuthenticationMechanism.Default;
            wsManConInfo.Port = Port;
            wsManConInfo.UseCompression = true;

            // Skip certificate checks to avoid CA issues
            wsManConInfo.SkipCACheck = true;
            wsManConInfo.SkipCNCheck = true;

            // 2. Create runspace
            progStatus = "Creating remote runspace";
            bgWorker.ReportProgress(50, progStatus);

            var runspace = RunspaceFactory.CreateRunspace(wsManConInfo);

            using (var powershell = PowerShell.Create())
            {
                // 3. Enable mailbox command
                progStatus = "Initializing 'Enable-Mailbox' command";
                bgWorker.ReportProgress(75, progStatus);

                var cmdEnableMailbox = new Command("Enable-Mailbox");
                cmdEnableMailbox.Parameters.Add("Identity", txtIdentity.Text.Trim());
                cmdEnableMailbox.Parameters.Add("Database", txtMailboxDatabase.Text.Trim());
                cmdEnableMailbox.Parameters.Add("DomainController", txtComputerName.Text.Trim());

                powershell.Commands.AddCommand(cmdEnableMailbox);
                powershell.Runspace = runspace;

                // 4. Open runspace and invoke commands
                progStatus = "Opening runspace and invoking commands";
                bgWorker.ReportProgress(100, progStatus);

                runspace.Open();
                powershell.Invoke();
                HandleErrors(powershell);
            }

            return "Mailbox for identity '" + txtIdentity.Text + "' enabled successfully!";
        }

        /// <summary>
        /// Handle errors if occured during execution of commands.
        /// </summary>
        private static void HandleErrors(PowerShell powerShell)
        {
            // If at least one error occurred
            if (powerShell.Streams.Error.Count <= 0) return;

            // Combine all error messages in one message and throw an exception
            StringBuilder sbErrorMessage = new StringBuilder();

            foreach (ErrorRecord errorRecord in powerShell.Streams.Error)
                sbErrorMessage.AppendFormat("{0} ", errorRecord.Exception.Message);

            throw new Exception(sbErrorMessage.ToString().Trim(), powerShell.Streams.Error[0].Exception);
        }

        /// <summary>
        /// Get PSCredential object for provider username and password.
        /// </summary>
        public PSCredential Credential()
        {
            var password = ConvertToSecureString(txtPassword.Text.Trim());
            var psCredential = new PSCredential(@txtUsername.Text.Trim(), password);

            return psCredential;

        }

        /// <summary>
        /// Convert string value into encrypted secure value.
        /// </summary>
        public static SecureString ConvertToSecureString(string strPassword)
        {
            var secureStr = new SecureString();
            if (strPassword.Length <= 0) return secureStr;

            foreach (var c in strPassword.ToCharArray()) secureStr.AppendChar(c);
            return secureStr;
        }

        #endregion

        #region Label status

        private void lblStatus_Click(object sender, EventArgs e)
        {
            Process.Start("http://lnkd.in/bJ3eyHY");
        }

        private void lblStatus_MouseHover(object sender, EventArgs e)
        {
            Cursor = Cursors.Hand;
        }

        private void lblStatus_MouseLeave(object sender, EventArgs e)
        {
            Cursor = Cursors.Default;
        }

        #endregion
    }
}
