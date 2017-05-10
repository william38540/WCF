using System;
using System.ComponentModel;
using System.Drawing;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess.Design;
using System.Threading;
using System.Windows.Forms;
using WindowsService.Properties;
using Microsoft.Win32;
using Progress;

namespace WindowsService
{
    internal class ServicesManagerHelper
    {
        // Use NTLM security provider to check 
        private const int Logon32ProviderDefault = 0x0;

        // To validate the account
        private const int Logon32LogonNetwork = 0x3;

        // API declaration for validating user credentials

        private const char BlackCircle = '\u25CF';
        private static bool _bServiceAccount = true;

        private static string _sDomainName;
        private static string _sUserName;
        private static string _sPassword;
        private static bool _bLogonUser;

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword,
            int dwLogonType,
            int dwLogonProvider, out int phToken);

        //API to close the credential token
        [DllImport("kernel32", EntryPoint = "CloseHandle")]
        private static extern long CloseHandle(long hObject);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetActiveWindow();

        public static ReturnValue InstallService(string svcName, string svcDispName, string svcDescription,
            string svcPath, ServiceType svcType,
            OnError errHandle, StartMode svcStartMode, bool interactWithDesktop,
            string svcStartName, string svcPassword, string loadOrderGroup,
            string[] loadOrderGroupDependencies, string[] svcDependencies)
        {
            var mc = new ManagementClass("Win32_Service");
            var inParams = mc.GetMethodParameters("create");
            inParams["Name"] = svcName;
            inParams["DisplayName"] = svcDispName;
            inParams["PathName"] = svcPath;
            inParams["ServiceType"] = svcType;
            inParams["ErrorControl"] = errHandle;
            inParams["StartMode"] = svcStartMode.ToString();
            inParams["DesktopInteract"] = interactWithDesktop;
            inParams["StartName"] = svcStartName;
            inParams["StartPassword"] = svcPassword;
            inParams["LoadOrderGroup"] = loadOrderGroup;
            inParams["LoadOrderGroupDependencies"] = loadOrderGroupDependencies;
            inParams["ServiceDependencies"] = svcDependencies;

            try
            {
                var outParams = mc.InvokeMethod("create", inParams, null);
                ChangeDescription(svcName, svcDescription);

                return (ReturnValue) Enum.Parse(typeof(ReturnValue), outParams["ReturnValue"].ToString());
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static ReturnValue UninstallService(string svcName)
        {
            var objPath = $"Win32_Service.Name='{svcName}'";
            using (var service = new ManagementObject(new ManagementPath(objPath)))
            {
                try
                {
                    var outParams = service.InvokeMethod("delete", null, null);
                    return (ReturnValue) Enum.Parse(typeof(ReturnValue), outParams["ReturnValue"].ToString());
                }
                catch (Exception ex)
                {
                    if (ex.Message.ToLower().Trim() == "not found" || ex.GetHashCode() == 41149443)
                        return ReturnValue.ServiceNotFound;
                    throw;
                }
            }
        }

        public static ReturnValue StartService(string svcName)
        {
            var objPath = $"Win32_Service.Name='{svcName}'";
            using (var service = new ManagementObject(new ManagementPath(objPath)))
            {
                try
                {
                    var outParams = service.InvokeMethod("StartService", null, null);
                    return (ReturnValue) Enum.Parse(typeof(ReturnValue), outParams["ReturnValue"].ToString());
                }
                catch (Exception)
                {
                    return ReturnValue.ServiceNotFound;
                }
            }
        }

        public static ReturnValue StopService(string svcName)
        {
            var objPath = $@"Win32_Service.Name='{svcName}'";
            using (var service = new ManagementObject(new ManagementPath(objPath)))
            {
                try
                {
                    var outParams = service.InvokeMethod("StopService", null, null);
                    return (ReturnValue) Enum.Parse(typeof(ReturnValue), outParams["ReturnValue"].ToString());
                }
                catch (Exception ex)
                {
                    if (ex.Message.ToLower().Trim() == "not found" || ex.GetHashCode() == 41149443)
                        return ReturnValue.ServiceNotFound;
                    throw;
                }
            }
        }

        public static ReturnValue ResumeService(string svcName)
        {
            var objPath = $@"Win32_Service.Name='{svcName}'";
            using (var service = new ManagementObject(new ManagementPath(objPath)))
            {
                try
                {
                    var outParams = service.InvokeMethod("ResumeService", null, null);
                    return (ReturnValue) Enum.Parse(typeof(ReturnValue), outParams["ReturnValue"].ToString());
                }
                catch (Exception ex)
                {
                    if (ex.Message.ToLower().Trim() == "not found" || ex.GetHashCode() == 41149443)
                        return ReturnValue.ServiceNotFound;
                    throw;
                }
            }
        }

        public static ReturnValue PauseService(string svcName)
        {
            var objPath = $@"Win32_Service.Name='{svcName}'";
            using (var service = new ManagementObject(new ManagementPath(objPath)))
            {
                try
                {
                    var outParams = service.InvokeMethod("PauseService", null, null);
                    return (ReturnValue) Enum.Parse(typeof(ReturnValue), outParams["ReturnValue"].ToString());
                }
                catch (Exception ex)
                {
                    if (ex.Message.ToLower().Trim() == "not found" || ex.GetHashCode() == 41149443)
                        return ReturnValue.ServiceNotFound;
                    throw;
                }
            }
        }

        public static ReturnValue ChangeStartMode(string svcName, StartMode startMode)
        {
            var objPath = $@"Win32_Service.Name='{svcName}'";
            using (var service = new ManagementObject(new ManagementPath(objPath)))
            {
                var inParams = service.GetMethodParameters("ChangeStartMode");
                inParams["StartMode"] = startMode.ToString();
                try
                {
                    var outParams = service.InvokeMethod("ChangeStartMode", inParams, null);

                    return (ReturnValue) Enum.Parse(typeof(ReturnValue), outParams["ReturnValue"].ToString());
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }

        public static bool ChangeDescription(string svcName, string svcDescription)
        {
            RegistryKey system;

            //HKEY_LOCAL_MACHINE\Services\CurrentControlSet
            RegistryKey currentControlSet;

            //...\Services
            RegistryKey services;

            //...\<Service Name>
            RegistryKey service;

            // ...\Parameters - this is where you can put service-specific configuration
            // Microsoft.Win32.RegistryKey config;

            try
            {
                //Open the HKEY_LOCAL_MACHINE\SYSTEM key
                system = Registry.LocalMachine.OpenSubKey("System");
                //Open CurrentControlSet
                currentControlSet = system.OpenSubKey("CurrentControlSet");
                //Go to the services key
                services = currentControlSet.OpenSubKey("Services");

                //Open the key for your service, and allow writing
                service = services.OpenSubKey(svcName, true);
                //Add your service's description as a REG_SZ value named "Description"
                service.SetValue("Description", svcDescription);
                //(Optional) Add some custom information your service will use...
                // config = service.CreateSubKey("Parameters");
            }
            catch (Exception e)
            {
                throw new Exception(e.Message + "\n" + e.StackTrace);
            }
            return true;
        }

        public static bool IsServiceInstalled(string svcName)
        {
            var objPath = $@"Win32_Service.Name='{svcName}'";
            using (var service = new ManagementObject(new ManagementPath(objPath)))
            {
                try
                {
                    var outParams = service.InvokeMethod("InterrogateService", null, null);

                    return true;
                }
                catch (Exception ex)
                {
                    if (ex.Message.ToLower().Trim() == "not found" || ex.GetHashCode() == 41149443)
                        return false;
                    throw;
                }
            }
        }

        public static ServiceState GetServiceState(string svcName)
        {
            var toReturn = ServiceState.Stopped;
            var _state = string.Empty;

            var objPath = $@"Win32_Service.Name='{svcName}'";
            using (var service = new ManagementObject(new ManagementPath(objPath)))
            {
                try
                {
                    _state = service.Properties["State"].Value.ToString().Trim();
                    switch (_state)
                    {
                        case "Running":
                            toReturn = ServiceState.Running;
                            break;
                        case "Stopped":
                            toReturn = ServiceState.Stopped;
                            break;
                        case "Paused":
                            toReturn = ServiceState.Paused;
                            break;
                        case "Start Pending":
                            toReturn = ServiceState.StartPending;
                            break;
                        case "Stop Pending":
                            toReturn = ServiceState.StopPending;
                            break;
                        case "Continue Pending":
                            toReturn = ServiceState.ContinuePending;
                            break;
                        case "Pause Pending":
                            toReturn = ServiceState.PausePending;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            return toReturn;
        }

        public static bool CanStop(string svcName)
        {
            var objPath = $@"Win32_Service.Name='{svcName}'";
            using (var service = new ManagementObject(new ManagementPath(objPath)))
            {
                try
                {
                    return bool.Parse(service.Properties["AcceptStop"].Value.ToString());
                }
                catch
                {
                    return false;
                }
            }
        }

        public static bool CanPauseAndContinue(string svcName)
        {
            var objPath = $@"Win32_Service.Name='{svcName}'";
            using (var service = new ManagementObject(new ManagementPath(objPath)))
            {
                try
                {
                    return bool.Parse(service.Properties["AcceptPause"].Value.ToString());
                }
                catch
                {
                    return false;
                }
            }
        }

        public static int GetProcessId(string svcName)
        {
            var objPath = $@"Win32_Service.Name='{svcName}'";
            using (var service = new ManagementObject(new ManagementPath(objPath)))
            {
                try
                {
                    return int.Parse(service.Properties["ProcessId"].Value.ToString());
                }
                catch
                {
                    return 0;
                }
            }
        }

        public static string GetPath(string svcName)
        {
            var objPath = $@"Win32_Service.Name='{svcName}'";
            using (var service = new ManagementObject(new ManagementPath(objPath)))
            {
                try
                {
                    return service.Properties["PathName"].Value.ToString();
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        public static bool ShowProperties(string svcName)
        {
            var objPath = $@"Win32_Service.Name='{svcName}'";
            using (var service = new ManagementObject(new ManagementPath(objPath)))
            {
                try
                {
                    foreach (var a in service.SystemProperties)
                        MessageBox.Show(PropData2String(a));
                    foreach (var a in service.Properties)
                        MessageBox.Show(PropData2String(a));
                    if (service.Properties["State"].Value.ToString().Trim() == "Running")
                        return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return false;
        }

        private static string PropData2String(PropertyData pd)
        {
            var toReturn = "Name: " + pd.Name + Environment.NewLine;
            toReturn += "IsArray: " + pd.IsArray + Environment.NewLine;
            toReturn += "IsLocal: " + pd.IsLocal + Environment.NewLine;
            toReturn += "Origin: " + pd.Origin + Environment.NewLine;
            toReturn += "CIMType: " + pd.Type + Environment.NewLine;
            if (pd.Value != null)
                toReturn += "Value: " + pd.Value + Environment.NewLine;
            else
                toReturn += "Value is null" + Environment.NewLine;
            var i = 0;
            foreach (var qd in pd.Qualifiers)
            {
                toReturn += "\tQualifier[" + i + "]IsAmended: " + qd.IsAmended +
                            Environment.NewLine;
                toReturn += "\tQualifier[" + i + "]IsLocal: " + qd.IsLocal + Environment.NewLine;
                toReturn += "\tQualifier[" + i + "]IsOverridable: " + qd.IsOverridable +
                            Environment.NewLine;
                toReturn += "\tQualifier[" + i + "]Name: " + qd.Name + Environment.NewLine;
                toReturn += "\tQualifier[" + i + "]PropagatesToInstance: " +
                            qd.PropagatesToInstance + Environment.NewLine;
                toReturn += "\tQualifier[" + i + "]PropagatesToSubclass: " +
                            qd.PropagatesToSubclass + Environment.NewLine;
                if (qd.Value != null)
                    toReturn += "\tQualifier[" + i + "]Value: " + qd.Value + Environment.NewLine;
                else
                    toReturn += "\tQualifier[" + i + "]Value is null" + Environment.NewLine;
                i++;
            }
            return toReturn;
        }

        // Prompt the user for service installation account values.
        public static bool GetServiceAccount(ref string svcUsername, ref string svcPassword)
        {
            var bAccountSet = false;
            var svcDialog = new ServiceInstallerDialog();
            svcDialog.TopMost = true;
            svcDialog.ShowInTaskbar = true;
            svcDialog.BackColor = Color.White;
            svcDialog.HelpButton = false;
            //         svcDialog.ShowIcon = true;
            var lOIdentity = WindowsIdentity.GetCurrent();
            svcDialog.Username = lOIdentity.Name;
            var attrs = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            var sTitle = @"Administrateur propriétaire du service " + ((AssemblyTitleAttribute) attrs[0]).Title;
            svcDialog.Text = sTitle;
            svcDialog.StartPosition = FormStartPosition.CenterScreen;

            svcDialog.Width += 60; //Enlarge dialog (in french, it's too small)

            //Give focus password edit 
            var childControl = svcDialog.Controls[0];
            childControl.BackgroundImage = Resources.banner;
            childControl.BackgroundImageLayout = ImageLayout.Center;

            // Recurse child controls. 
            foreach (Control grandChild in childControl.Controls)
                if (grandChild.Name == "passwordEdit")
                {
                    //Black circle in Windows XP for passwords
                    ((TextBox) grandChild).PasswordChar = BlackCircle;
                    grandChild.Select(); //Focus on password
                }
                else if (grandChild.Name == "confirmPassword")
                {
                    ((TextBox) grandChild).PasswordChar = BlackCircle;
                }
                else if (grandChild.Name == "label1")
                {
                    //For size to content
                    var sSpace = string.Empty;
                    sSpace = sSpace.PadRight(10, ' ');
                    grandChild.Text += sSpace;
                }

            // Query the user for the service account type.
            do
            {
                IWin32Window owner = Control.FromHandle(GetActiveWindow());
                svcDialog.ShowDialog(owner);
                // hourglass cursor
                Cursor.Current = Cursors.WaitCursor;
                if (svcDialog.Result == ServiceInstallerDialogResult.OK)
                {
                    //Validate credentials
                    var aUserName = svcDialog.Username.Split('\\');
                    _sDomainName = string.Empty;
                    _sUserName = string.Empty;
                    if (aUserName.GetUpperBound(0) >= 0)
                        _sDomainName = aUserName[0];
                    if (aUserName.GetUpperBound(0) >= 1)
                        _sUserName = aUserName[1];
                    _sPassword = svcDialog.Password;
                    bAccountSet = FormLogonUser(_sDomainName, _sUserName, _sPassword);

                    Cursor.Current = Cursors.Default;
                    if (bAccountSet)
                    {
                        // Use the account and password.

                        svcUsername = svcDialog.Username;
                        svcPassword = svcDialog.Password;
                    }
                    else
                    {
                        //Tell the user to enter a valid user and password
                        var result = MessageBox.Show(@"L'authentification a échoué.", sTitle,
                            MessageBoxButtons.OK, MessageBoxIcon.Hand);
                    }
                }
            } while (!bAccountSet && svcDialog.Result != ServiceInstallerDialogResult.Canceled);

            return bAccountSet;
        }

        private static bool FormLogonUser(string sDomainName, string sUserName, string sPassword)
        {
            var bResult = true;

            var form = new ProgressForm();
            var attrs = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            var sTitle = ((AssemblyTitleAttribute) attrs[0]).Title;
            form.Text = sTitle;
            //pass the argument for our background worker
            form.DefaultStatusText = @"Authentification en cours...";
            form.DoWork += FormDoWork;

            _bLogonUser = false;
            //check how the background worker finished
            var result = form.ShowDialog();
            bResult = result != DialogResult.Cancel && _bLogonUser;
            return bResult;
        }

        private static void FormDoWork(ProgressForm sender, DoWorkEventArgs e)
        {
            var sText = (string) e.Argument;

            Thread listenThread;
            //start the listening thread
            listenThread = new Thread(DoLogonUser);
            listenThread.Start();

            while (listenThread.IsAlive)
            {
                Thread.Sleep(50);
                if (sender.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
            }
        }

        private static void DoLogonUser()
        {
            //Validate credentials
            var hToken = 0;
            _bLogonUser = LogonUser(_sUserName, _sDomainName, _sPassword, Logon32LogonNetwork,
                Logon32ProviderDefault, out hToken);
            CloseHandle(hToken);
        }

        public static string GetDomainName(string usernameDomain)
        {
            if (string.IsNullOrEmpty(usernameDomain))
                throw new ArgumentException(@"utilisateur du domaine ne peut être null", "usernameDomain");
            if (usernameDomain.Contains("\\"))
            {
                var index = usernameDomain.IndexOf("\\");
                return usernameDomain.Substring(0, index);
            }
            if (usernameDomain.Contains("@"))
            {
                var index = usernameDomain.IndexOf("@");
                return usernameDomain.Substring(index + 1);
            }
            return "";
        }

        public static string GetUsername(string usernameDomain)
        {
            if (string.IsNullOrEmpty(usernameDomain))
                throw new ArgumentException(@"Le domaine ne peut être null.", "usernameDomain");
            if (usernameDomain.Contains("\\"))
            {
                var index = usernameDomain.IndexOf("\\");
                return usernameDomain.Substring(index + 1);
            }
            if (usernameDomain.Contains("@"))
            {
                var index = usernameDomain.IndexOf("@");
                return usernameDomain.Substring(0, index);
            }
            return usernameDomain;
        }

        public static bool CheckUser(string svnUser, string svnPassword)
        {
            var domainName = GetDomainName(svnUser); // Extract domain name 
            var userName = GetUsername(svnUser); // Extract user name 
            var token = 0;
            var bLogonUser = LogonUser(userName, domainName, svnPassword, Logon32LogonNetwork,
                Logon32ProviderDefault, out token);
            CloseHandle(token);
            return bLogonUser;
        }

        #region Enum

        #region OnError enum

        public enum OnError
        {
            UserIsNotNotified = 0,
            UserIsNotified = 1,
            SystemRestartedLastGoodConfiguraion = 2,
            SystemAttemptStartWithGoodConfiguration = 3
        }

        #endregion

        #region ReturnValue enum

        public enum ReturnValue
        {
            Success = 0,
            NotSupported = 1,
            AccessDenied = 2,
            DependentServicesRunning = 3,
            InvalidServiceControl = 4,
            ServiceCannotAcceptControl = 5,
            ServiceNotActive = 6,
            ServiceRequestTimeout = 7,
            UnknownFailure = 8,
            PathNotFound = 9,
            ServiceAlreadyRunning = 10,
            ServiceDatabaseLocked = 11,
            ServiceDependencyDeleted = 12,
            ServiceDependencyFailure = 13,
            ServiceDisabled = 14,
            ServiceLogonFailure = 15,
            ServiceMarkedForDeletion = 16,
            ServiceNoThread = 17,
            StatusCircularDependency = 18,
            StatusDuplicateName = 19,
            StatusInvalidName = 20,
            StatusInvalidParameter = 21,
            StatusInvalidServiceAccount = 22,
            StatusServiceExists = 23,
            ServiceAlreadyPaused = 24,
            ServiceNotFound = 25
        }

        #endregion

        #region ServiceState enum

        public enum ServiceState
        {
            Running,
            Stopped,
            Paused,
            StartPending,
            StopPending,
            PausePending,
            ContinuePending
        }

        #endregion

        #region ServiceType enum

        public enum ServiceType : uint
        {
            KernelDriver = 0x1,
            FileSystemDriver = 0x2,
            Adapter = 0x4,
            RecognizerDriver = 0x8,
            OwnProcess = 0x10,
            ShareProcess = 0x20,
            Interactive = 0x100
        }

        #endregion

        #region StartMode enum

        public enum StartMode
        {
            Boot = 0,
            System = 1,
            Automatic = 2,
            Manual = 3,
            Disabled = 4
        }

        #endregion

        #endregion Enum
    }
}