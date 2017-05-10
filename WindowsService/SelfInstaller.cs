using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WindowsService
{
    public static class SelfInstaller
    {
        public static bool InstallMe(string serviceName, string displayName, string comment, string startUser,
            string startPassword)
        {
            try
            {
                string userName = null;
                string password = null;
                if (!string.IsNullOrEmpty(startUser))
                {
                    if (ServicesManagerHelper.CheckUser(startUser, startPassword))
                    {
                        userName = startUser;
                        password = startPassword;
                    }
                    else
                    {
                        throw new Exception(@"Utilisateur et mot de passe invalide");
                    }
                }
                else
                {
                    var bServiceAccount = ServicesManagerHelper.GetServiceAccount(ref userName, ref password);
                    if (!bServiceAccount)
                        throw new Exception(
                            "Installation du service"); //Attention, si échec, le framework rappelle une seconde fois l'installateur !
                }

                ServicesManagerHelper.InstallService(serviceName, displayName, comment,
                    Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName,
                    ServicesManagerHelper.ServiceType.OwnProcess,
                    ServicesManagerHelper.OnError.UserIsNotNotified,
                    ServicesManagerHelper.StartMode.Automatic, false,
                    userName, password, null, null, null);

                //ManagedInstallerClass.InstallHelper(new string[] {ExePath});
                var file = Assembly.GetExecutingAssembly().ManifestModule.Name;
                var app = Path.GetFileNameWithoutExtension(file);

                var ckey = Registry.LocalMachine.OpenSubKey(
                    $@"SYSTEM\CurrentControlSet\Services\{app}", true);

                if (ckey?.GetValue("Type") != null)
                    ckey.SetValue("Type", (int) ckey.GetValue("Type") | 256);
            }
            catch (Exception ex)
            {
                var attrs = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                MessageBox.Show(ex.Message, ((AssemblyTitleAttribute) attrs[0]).Title, MessageBoxButtons.OK,
                    MessageBoxIcon.Hand);
                return false;
            }
            return true;
        }

        public static bool UninstallMe(string serviceName)
        {
            try
            {
                ServicesManagerHelper.UninstallService(serviceName);
            }
            catch (Exception ex)
            {
                var attrs = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                MessageBox.Show(ex.Message, ((AssemblyTitleAttribute) attrs[0]).Title, MessageBoxButtons.OK,
                    MessageBoxIcon.Hand);
                return false;
            }
            return true;
        }
    }
}