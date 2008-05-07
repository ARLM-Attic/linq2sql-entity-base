using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using LINQEntityBaseExampleData;

namespace LINQEntityBaseExampleWCFService
{    
    public class Service : IService
    {
        public Customer RetrieveCustomerDataByID(string CustomerID, out string DBLog)
        {
            return NorthwindDataAccess.RetrieveCustomerDataByID(CustomerID, out DBLog);
        }

        public void UpdateCustomerData(Customer ModifiedCustomer, out string DBLog)
        {
            NorthwindDataAccess.UpdateCustomerData(ModifiedCustomer, out DBLog);
        }
    }
}
