using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Data.Linq;
using System.Text;
using LINQEntityBaseExampleData;

namespace LINQEntityBaseExampleData
{
    public class NorthwindDataAccess
    {
        public static Customer RetrieveCustomerDataByID(string CustomerID, out string DBLog)
        {
            Customer customer;

            using (NorthWindDataContext db = new NorthWindDataContext())
            {
                TextWriter log = new StringWriter();
                DataLoadOptions lo = new DataLoadOptions();
                db.Log = log;
                lo.LoadWith<Customer>(c => c.Orders);
                lo.LoadWith<Order>(o => o.Order_Details);
                lo.LoadWith<Order_Detail>(od => od.Product);
                db.LoadOptions = lo;
                db.DeferredLoadingEnabled = false;
                customer = (from c in db.Customers
                            where c.CustomerID == "ALFKI"
                            select c).First();
                DBLog = log.ToString();
            }

            return customer;
        }

        public static void UpdateCustomerData(Customer ModifiedCustomer, out string DBLog)
        {
            // Update Order via root object, creating a new data context
            using (NorthWindDataContext db = new NorthWindDataContext())
            {
                TextWriter log = new StringWriter();
                               
                db.Log = log;
                db.DeferredLoadingEnabled = false;
                ModifiedCustomer.SynchroniseWithDataContext(db, true); //cascade delete                
                db.SubmitChanges();

                DBLog = log.ToString();
            }
        }
    }
}
