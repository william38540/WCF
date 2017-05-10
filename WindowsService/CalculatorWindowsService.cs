using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Reflection;
using System.ServiceProcess;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WindowsService
{
    internal class CalculatorWindowsService
    {
        private static bool _isInstalled;
        private static readonly string ServiceName = "WCFWindowsServiceSample";
        private static readonly string DisplayName = @"WCF Windows Service Sample";
        private static readonly string Comment = @"Create a WCF windows service.";

        /// <summary>
        ///     Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            var file = Assembly.GetExecutingAssembly().ManifestModule.Name;
            string app = null;

            try
            {
                app = Path.GetFileNameWithoutExtension(file);
                app = ServiceName;
            }
            catch (ArgumentException)
            {
            }
            var serviceName = ServiceName;
            var serviceStarting = IsInstalled(ServiceName);

            if (!serviceStarting)
            {
                var attrs = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                var sTitle = ((AssemblyTitleAttribute) attrs[0]).Title;
                if (args.Length >= 1)
                {
                    var commandLine = new Arguments(args);
                    var user = commandLine["user"];
                    var pwdc = commandLine["pwdc"];
                    var pwd = commandLine["pwd"];

                    //if (!String.IsNullOrEmpty(pwdc) && String.IsNullOrEmpty(pwd))
                    //    pwd = Crypt.Decrypt(pwdc);
                    ////MessageBox.Show("Password :" + pwd);
                    //if (args[0].ToLower().Contains("/c"))
                    //{
                    //    var pwdTxt = Crypt.Encrypt(pwd);
                    //    Clipboard.SetText(pwdTxt);
                    //    MessageBox.Show(String.Format("Le mot de passe crypté est [{0}], celui-ci est disponibe dans le presse-papier", pwdTxt), "Cryptage mot de passe", MessageBoxButtons.OK);
                    //    return;
                    //}


                    try
                    {
                        if (!_isInstalled && args[0].ToLower().Contains("/i"))
                        {
                            SelfInstaller.InstallMe(ServiceName, DisplayName, Comment, user, pwd);
                            UninstallService(); // Désinstallation du service
                        }
                        else if (args[0].ToLower().Contains("/ui"))
                        {
                            var dr = MessageBox.Show($@"Voulez-vous réinstaller le service {sTitle} ?",
                                sTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                            if (dr == DialogResult.Yes)
                            {
                                if (_isInstalled)
                                    SelfInstaller.InstallMe(ServiceName, DisplayName, Comment, user, pwd);
                                //On regarde à nouveau si le service est lancé
                                serviceStarting = IsInstalled(serviceName);
                                var bInstall = SelfInstaller.InstallMe(ServiceName, DisplayName, Comment, user, pwd);
                                MessageBox.Show(
                                    $@"La réinstallation du service {sTitle} a {(bInstall ? "réussi" : "échoué")}.",
                                    sTitle, MessageBoxButtons.OK,
                                    bInstall ? MessageBoxIcon.Information : MessageBoxIcon.Stop);
                            }
                            serviceStarting = IsInstalled(serviceName);
                            if (_isInstalled)
                            {
                                dr = MessageBox.Show($@"Voulez-vous démarrer le service {sTitle} ?",
                                    sTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                                if (dr == DialogResult.Yes)
                                {
                                    //Start service
                                    var rv = ServicesManagerHelper.StartService(app);
                                    MessageBox.Show(
                                        $@"Le démarrage du service {sTitle} a {
                                                (rv == ServicesManagerHelper.ReturnValue.Success ? "réussi" : "échoué")
                                            }.",
                                        sTitle, MessageBoxButtons.OK,
                                        rv == ServicesManagerHelper.ReturnValue.Success
                                            ? MessageBoxIcon.Information
                                            : MessageBoxIcon.Stop);
                                }
                            }
                        }
                        else if (_isInstalled)
                        {
                            if (args[0].ToLower().Contains("/u"))
                            {
                                ServicesManagerHelper.StopService(app);
                                SelfInstaller.UninstallMe(ServiceName);
                            }
                        }
                    }
                    catch (ArgumentNullException)
                    {
                    }

                    try
                    {
                        if (_isInstalled && args[0].ToLower().Contains("/r"))
                        {
                            var rkHKLM = Registry.LocalMachine;
                            var rkService = rkHKLM.OpenSubKey($@"SYSTEM\CurrentControlSet\services\{app}");
                            var fullName = Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName;
                            var folderService = rkService.GetValue("ImagePath").ToString().Replace("\"", "");

                            if (IsUpgradVersion(fullName, folderService))
                                if (folderService != null && fullName != folderService)
                                {
                                    var rv = ServicesManagerHelper.StartService(app);
                                    if (rv == ServicesManagerHelper.ReturnValue.ServiceAlreadyRunning)
                                        ServicesManagerHelper.StopService(app);

                                    if (File.Exists(folderService))
                                        File.Delete(folderService);
                                    File.Copy(fullName, folderService);
                                }
                                else
                                {
                                    SelfInstaller.InstallMe(ServiceName, DisplayName, Comment, user, pwd);
                                }
                        }
                    }
                    catch (ArgumentNullException)
                    {
                    }
                    try
                    {
                        switch (commandLine["OPTION"])
                        {
                            case "start":
                                ServicesManagerHelper.StartService(app);
                                break;
                            case "stop":
                                ServicesManagerHelper.StopService(app);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($@"Service {ex.Message}", sTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    #region isInstalled

                    if (_isInstalled)
                    {
                        var dr = MessageBox.Show($@"Voulez-vous désinstaller le service {sTitle} ?",
                            sTitle,
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (dr == DialogResult.Yes)
                        {
                            var bInstall = SelfInstaller.UninstallMe(ServiceName);
                            MessageBox.Show(
                                $@"La désinstallation du service {sTitle} a {(bInstall ? "réussi" : "échoué")}.",
                                sTitle, MessageBoxButtons.OK,
                                bInstall ? MessageBoxIcon.Information : MessageBoxIcon.Stop);
                        }
                    }
                    else
                    {
                        var dr =
                            MessageBox.Show($@"Voulez-vous installer le service {sTitle} ?",
                                sTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (dr == DialogResult.Yes)
                        {
                            var bInstall = SelfInstaller.InstallMe(ServiceName, DisplayName, Comment, string.Empty, string.Empty);
                            MessageBox.Show(
                                $@"L’installation du service {sTitle} a {(bInstall ? "réussi" : "échoué")}.",
                                sTitle, MessageBoxButtons.OK,
                                bInstall ? MessageBoxIcon.Information : MessageBoxIcon.Stop);
                        }
                        serviceStarting = IsInstalled(serviceName);
                        if (_isInstalled)
                        {
                            dr = MessageBox.Show($@"Voulez-vous démarrer le service {sTitle} ?",
                                sTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                            if (dr == DialogResult.Yes)
                            {
                                //Start service
                                var rv = ServicesManagerHelper.StartService(app);
                                MessageBox.Show(
                                    $@"Le démarrage du service {sTitle} a {
                                            (rv == ServicesManagerHelper.ReturnValue.Success ? "réussi" : "échoué")
                                        }.",
                                    sTitle, MessageBoxButtons.OK,
                                    rv == ServicesManagerHelper.ReturnValue.Success
                                        ? MessageBoxIcon.Information
                                        : MessageBoxIcon.Stop);
                            }
                        }
                    }

                    #endregion
                }
            }
            else
            {
                try
                {
                    var servicestorun = new ServiceBase[] {new Service()};
                    ServiceBase.Run(servicestorun);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($@"Message {ex.Message},{ex?.InnerException?.Message}");
                }
            }
        }

        private static bool IsInstalled(string serviceName)
        {
            var serviceStarting = false;
            _isInstalled = false;
            var services = new ServiceController[] { };
            try
            {
                services = ServiceController.GetServices();
            }
            catch (Win32Exception)
            {
            }
            foreach (var service in services)
                try
                {
                    if (service.ServiceName.Equals(serviceName))
                    {
                        _isInstalled = true;
                        if (service.Status == ServiceControllerStatus.StartPending)
                            serviceStarting = true;
                        break;
                    }
                }
                catch (NullReferenceException)
                {
                }
            return serviceStarting;
        }

        private static bool IsUpgradVersion(string fileSource, string fileTarget)
        {
            if (fileTarget == null)
                return true;

            var source = FileVersionInfo.GetVersionInfo(fileSource);
            var target = FileVersionInfo.GetVersionInfo(fileTarget);

            if (source.FileMajorPart > target.FileMajorPart)
                return true;
            if (source.FileMajorPart == target.FileMajorPart && source.FileMinorPart > target.FileMinorPart)
                return true;
            if (source.FileMajorPart == target.FileMajorPart && source.FileMinorPart == target.FileMinorPart &&
                source.FileBuildPart > target.FileBuildPart)
                return true;
            if (source.FileMajorPart == target.FileMajorPart && source.FileMinorPart == target.FileMinorPart &&
                source.FileBuildPart == target.FileBuildPart && source.FileBuildPart > target.FileBuildPart)
                return true;
            return false;
        }

        private static void UninstallService()
        {
            var services = new ServiceController[] { };
            try
            {
                services = ServiceController.GetServices();
            }
            catch (Win32Exception)
            {
            }
            foreach (var service in services)
                try
                {
                    if (service.ServiceName.Equals(ServiceName))
                    {
                        if (service.Status == ServiceControllerStatus.Running ||
                            service.Status == ServiceControllerStatus.StartPending)
                            ServicesManagerHelper.StopService(ServiceName);

                        UninstallService(ServiceName);
                    }
                }
                catch (NullReferenceException)
                {
                }
        }

        private static ServicesManagerHelper.ReturnValue UninstallService(string svcName)
        {
            var objPath = $@"Win32_Service.Name='{svcName}'";
            using (var service = new ManagementObject(new ManagementPath(objPath)))
            {
                try
                {
                    var outParams = service.InvokeMethod("delete", null, null);
                    return (ServicesManagerHelper.ReturnValue) Enum.Parse(typeof(ServicesManagerHelper.ReturnValue),
                        outParams["ReturnValue"].ToString());
                }
                catch (Exception ex)
                {
                    if (ex.Message.ToLower().Trim() == "not found" || ex.GetHashCode() == 41149443)
                        return ServicesManagerHelper.ReturnValue.ServiceNotFound;
                    throw ex;
                }
            }
        }
    }
}