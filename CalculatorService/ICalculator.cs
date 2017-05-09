using System;
using System.Collections.Generic;
using System;
using System.ServiceModel;

namespace CalculatorService
{
    // Define a service contract.
    [ServiceContract(Namespace = "Microsoft.Samples.ConfigSimplification")]
    public interface ICalculator
    {
        [OperationContract]
        double Add(double n1, double n2);

        [OperationContract]
        double Subtract(double n1, double n2);

        [OperationContract]
        double Multiply(double n1, double n2);

        [OperationContract]
        double Divide(double n1, double n2);
    }
}
