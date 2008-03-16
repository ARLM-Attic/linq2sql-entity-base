using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Linq;
using System.Text;

namespace LINQEntityBaseExample
{
    class Program
    {
        static void Main(string[] args)
        {
            // EXAMPLE: 
            // 1. Detach From Original
            // 2. Add/Modify
            // 3. Re-Attach to new DataContext + Commit
            // 4. Delete Record, 
            // 5. Re-Attach to new DataContext + Commit

            // NOTES:
            // Hierarchy is :
            // Customer --> Order --> Order Detail
            // Product is not updateable because it is not in hierarchy, 
            // as it is only referenced.

            List<Customer> customers;

            // Note that each table has a RowVersion field and DeferredLoading is off.
            using (NorthWindDataContext db = new NorthWindDataContext())
            {
                DataLoadOptions lo = new DataLoadOptions();
                lo.LoadWith<Customer>(c => c.Orders);
                lo.LoadWith<Order>(o => o.Order_Details);
                lo.LoadWith<Order_Detail>(od => od.Product);
                db.LoadOptions = lo;
                db.DeferredLoadingEnabled = false; 
                customers = (from c in db.Customers
                            where c.CustomerID == "ALFKI"
                            select c).ToList();

            }

            /// WE ARE NOW DISCONNECTED ///

            // Update Customer
            string fax = customers.First().Fax;
            string phone = customers.First().Phone;
            customers.First().Fax = phone;
            customers.First().Phone = fax;
            Console.WriteLine("Customer Modified: {0}", customers.First().LINQEntityGUID);

            // Add an order record
            Order orderAdded = new Order()
                    {EmployeeID = 3, 
                     OrderDate = DateTime.Now,
                     RequiredDate = DateTime.Now.AddDays(30),
                     ShippedDate  = DateTime.Now.AddDays(10),
                     ShipVia = 1,
                     Freight = 10,
                     ShipName = "Delete Me Later!",
                     ShipAddress = "Obere Str. 57",
                     ShipCity = "Berlin",
                     ShipRegion = null,
                     ShipPostalCode = "12209",
                     ShipCountry = "Germany"};
            
            customers.First().Orders.Add(orderAdded);


            Console.WriteLine("Order Added: {0}", orderAdded.LINQEntityGUID);

            // Modify Some Orders
            List<Order> orders = (from o in customers.First().Orders
                                 where o.ShipCountry == "Germany"
                                 select o).ToList();
            
            foreach (Order order in orders)
            {
                // $1 to frieght charge
                order.Freight += 1;
                Console.WriteLine("Order Modified: {0}", order.LINQEntityGUID);
            }



            // Update Order via root object, creating a new data context
            using(NorthWindDataContext db = new NorthWindDataContext())
            {
                Console.WriteLine("---------------");
                Console.WriteLine("Changes Tracked");
                Console.WriteLine("---------------");
                foreach (LINQEntityBase entity in customers.First().ToEntityTree())
                {
                    if (entity.IsNew)
                        Console.WriteLine("--> Added: {0}", entity.LINQEntityGUID);
                    else if (entity.IsModified)
                        Console.WriteLine("--> Modified {0}", entity.LINQEntityGUID);
                    else if (entity.IsDeleted)
                        Console.WriteLine("-->Deleted {0}", entity.LINQEntityGUID);

                }

                db.DeferredLoadingEnabled = false;
                customers.First().SynchroniseWithDataContext(db);
                db.SubmitChanges();
                Console.WriteLine("----------------");
                Console.WriteLine("Changes Comitted");
                Console.WriteLine("----------------");
            }

            /// WE ARE NOW DISCONNECTED AGAIN! ///

            // Delete an order (NOTE: it's using the flat list!)
            List<Order> ordersDelete = (from o in customers.First().ToEntityTree().OfType<Order>()
                                       where o.ShipName == "Delete Me Later!"
                                       select o).ToList();

            foreach (Order orderDeleted in ordersDelete)
            {
                orderDeleted.IsDeleted = true;
                Console.WriteLine("Order Deleted: {0}", orderDeleted.LINQEntityGUID);
            }

            // Update Order via root object, creating a new data context
            using (NorthWindDataContext db = new NorthWindDataContext())
            {
                Console.WriteLine("---------------");
                Console.WriteLine("Changes Tracked");
                Console.WriteLine("---------------");
                foreach (LINQEntityBase entity in customers.First().ToEntityTree())
                {
                    if (entity.IsNew)
                        Console.WriteLine("--> Added: {0}", entity.LINQEntityGUID);
                    else if (entity.IsModified)
                        Console.WriteLine("--> Modified {0}", entity.LINQEntityGUID);
                    else if (entity.IsDeleted)
                        Console.WriteLine("-->Deleted {0}", entity.LINQEntityGUID);

                }
                customers.First().SynchroniseWithDataContext(db);
                db.SubmitChanges();
                Console.WriteLine("---------------");
                Console.WriteLine("Changes Comitted");
                Console.WriteLine("---------------");

            }

            Console.ReadKey();
        }

    }
}
