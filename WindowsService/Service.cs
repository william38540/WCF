using System;
using System.ServiceModel;
using System.ServiceProcess;

namespace WindowsService
{
    internal partial class Service : ServiceBase
    {
        private ServiceHost _serviceHost;

        public Service()
        {
            this.ServiceName = "WCFWindowsServiceSample";
        }

        // Start the Windows service.
        protected override void OnStart(string[] args)
        {
            _serviceHost?.Close();

            // Create a ServiceHost for the CalculatorService type and 
            // provide the base address.
            // serviceHost = new ServiceHost(typeof(CalculatorService.CalculatorService));
            _serviceHost = new ServiceHost(typeof(CalculatorService.CalculatorService),
                new Uri("http://localhost:8000/ServiceModelSamples/Service.svc"));
            _serviceHost?.Open();
        }

        protected override void OnStop()
        {
            if (_serviceHost != null)
            {
                _serviceHost.Close();
                _serviceHost = null;
            }
        }
    }
}