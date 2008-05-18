using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using LINQEntityBaseExampleData;

namespace LINQEntityBaseExampleWCFService
{    
    [ServiceContract]
    public interface IService
    {

        [OperationContract]
        Customer RetrieveCustomerDataByID(string CustomerID, out string DBLog);

        [OperationContract]
        void UpdateCustomerData(Customer ModifiedCustomer, out string DBLog);

        [OperationContract]
        List<Product> GetProductData(out string DBLog);
    }
}
