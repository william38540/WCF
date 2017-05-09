using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.ServiceModel;
using System.ServiceProcess;
using System.Configuration;
using System.Configuration.Install;

namespace WindowsService
{
    // Provide the ProjectInstaller class which allows 
    // the service to be installed by the Installutil.exe tool
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        private ServiceProcessInstaller process;
        private ServiceInstaller service;

        public ProjectInstaller()
        {
            process = new ServiceProcessInstaller();
            process.Account = ServiceAccount.LocalSystem;
            service = new ServiceInstaller();
            service.ServiceName = "WCFWindowsServiceSample";
            Installers.Add(process);
            Installers.Add(service);
        }
    }

    class CalculatorWindowsService : ServiceBase
    {
        public ServiceHost serviceHost = null;
        public CalculatorWindowsService()
        {
            // Name the Windows Service
            ServiceName = "WCFWindowsServiceSample";
        }

        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        public static void Main()
        {
            ServiceBase.Run(new CalculatorWindowsService());
        }
        // Start the Windows service.
        protected override void OnStart(string[] args)
        {
            if (serviceHost != null)
            {
                serviceHost.Close();
            }

            // Create a ServiceHost for the CalculatorService type and 
            // provide the base address.
            // serviceHost = new ServiceHost(typeof(CalculatorService.CalculatorService));
            serviceHost = new ServiceHost(typeof(CalculatorService.CalculatorService),new Uri("http://localhost:8000/ServiceModelSamples/Service.svc"));
            if (serviceHost != null)
            {
                // Open the ServiceHost to create listeners and start listening for messages.
                serviceHost.Open();
            }
        }

        protected override void OnStop()
        {
            if (serviceHost != null)
            {
                serviceHost.Close();
                serviceHost = null;
            }
        }
    }
}
