//-----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All Rights Reserved.
//-----------------------------------------------------------------

using System;
using System.ServiceModel;
using  CalculatorService;

namespace Microsoft.Samples.ConfigSimplification
{

    // Added code to write output to the console window
    public static class CalculatorProgramm 
    {
        // Host the service within this EXE console application.
        public static void Main()
        {
            // Create a ServiceHost for the CalculatorService type.
            using (var serviceHost = new ServiceHost(typeof(CalculatorService.CalculatorService),
                new Uri("http://localhost:8000/ServiceModelSamples/Service.svc")))
            {
                // Open the ServiceHost to create listeners and start listening for messages.
                serviceHost.Open();

                // The service can now be accessed.
                Console.WriteLine("The service is ready.");
                Console.WriteLine("Press <ENTER> to terminate service.");
                Console.WriteLine();
                Console.ReadLine();
            }
        }
    }
}