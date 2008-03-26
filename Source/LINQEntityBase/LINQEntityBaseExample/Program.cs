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

            Customer customer;

            Console.WriteLine("-------------------------");
            Console.WriteLine("-- Retrieving Entities --");
            Console.WriteLine("-------------------------");

            // Note that each table has a RowVersion field and DeferredLoading is off.
            using (NorthWindDataContext db = new NorthWindDataContext())
            {
                DataLoadOptions lo = new DataLoadOptions();
                lo.LoadWith<Customer>(c => c.Orders);
                lo.LoadWith<Order>(o => o.Order_Details);
                lo.LoadWith<Order_Detail>(od => od.Product);
                db.LoadOptions = lo;
                db.DeferredLoadingEnabled = false;
                //db.Log = Console.Out;
                customer = (from c in db.Customers
                            where c.CustomerID == "ALFKI"
                            select c).First();
                
            }

            //Print out a list of original objects
            Console.WriteLine("-----------------------");
            Console.WriteLine("-- Values  Retrieved --");
            Console.WriteLine("-----------------------");

            foreach (LINQEntityBase entity in customer.ToEntityTree())
            {
                Console.WriteLine("Original {0} --> {1}", entity.LINQEntityGUID, entity.GetType().Name);
            }

            Console.WriteLine();
            Console.WriteLine("Press Any Key to Continue....");
            Console.ReadKey();
            Console.WriteLine();


            Console.WriteLine("-----------------------");
            Console.WriteLine("--    Modify Data    --");
            Console.WriteLine("-----------------------");

            // Tell the root object it's doing the change tracking
            customer.SetAsChangeTrackingRoot();

            /// WE ARE NOW DISCONNECTED ///

            // Update Customer
            string fax = customer.Fax;
            string phone = customer.Phone;
            customer.Fax = phone;
            customer.Phone = fax;
            Console.WriteLine("Modified {0} --> {1}", customer.LINQEntityGUID, customer.GetType().Name);            

            // Add an order record
            Order orderAdded = new Order()
                    {EmployeeID = 3, 
                     OrderDate = DateTime.Now,
                     RequiredDate = DateTime.Now.AddDays(30),
                     ShippedDate  = DateTime.Now.AddDays(10),
                     ShipVia = 1,
                     Freight = 10,
                     ShipName = "I have been added",
                     ShipAddress = "Obere Str. 57",
                     ShipCity = "Berlin",
                     ShipRegion = null,
                     ShipPostalCode = "12209",
                     ShipCountry = "USA"};
            
            customer.Orders.Add(orderAdded);

            Console.WriteLine("Added    {0} --> {1}", orderAdded.LINQEntityGUID, orderAdded.GetType().Name);

            // Add an order record
            orderAdded = new Order()
            {
                EmployeeID = 3,
                OrderDate = DateTime.Now,
                RequiredDate = DateTime.Now.AddDays(30),
                ShippedDate = DateTime.Now.AddDays(10),
                ShipVia = 1,
                Freight = 10,
                ShipName = "I have been added",
                ShipAddress = "Obere Str. 57",
                ShipCity = "Berlin",
                ShipRegion = null,
                ShipPostalCode = "12209",
                ShipCountry = "USA"
            };

            Console.WriteLine("Added    {0} --> {1}", orderAdded.LINQEntityGUID, orderAdded.GetType().Name);

            customer.Orders.Add(orderAdded);


            // Modify Some Orders
            List<Order> orders = (from o in customer.Orders
                                 where o.ShipCountry == "Germany"
                                  select o).Take(2).ToList();
            
            foreach (Order order in orders)
            {
                // $1 to frieght charge
                order.Freight += 1;
                Console.WriteLine("Modified {0} --> {1}", order.LINQEntityGUID, order.GetType().Name);
            }

            // Delete some orders (NOTE: it's using the flat list!)
            List<Order> ordersDelete = (from o in customer.ToEntityTree().OfType<Order>()
                                        where o.Freight < 20 && o.IsNew == false
                                        select o).ToList();

            foreach (Order orderDeleted in ordersDelete)
            {
                customer.Orders.Remove(orderDeleted);
                Console.WriteLine("Deleted  {0} --> {1}", orderDeleted.LINQEntityGUID, orderDeleted.GetType().Name);
            }



            Console.WriteLine();
            Console.WriteLine("Press Any Key to Continue....");
            Console.ReadKey();
            Console.WriteLine();

            Console.WriteLine("--------------------------------");
            Console.WriteLine("--    Review Modified Data    --");
            Console.WriteLine("--------------------------------");
            foreach (LINQEntityBase entity in customer.ToEntityTree())
            {
                if (entity.IsDeleted)
                    Console.WriteLine("Deleted  {0} --> {1}", entity.LINQEntityGUID, entity.GetType().Name);
                if (entity.IsNew)
                    Console.WriteLine("Added    {0} --> {1}", entity.LINQEntityGUID, entity.GetType().Name);
                if (entity.IsModified)
                    Console.WriteLine("Modified {0} --> {1}", entity.LINQEntityGUID, entity.GetType().Name);
                if (!entity.IsDeleted && !entity.IsModified && !entity.IsNew)
                    Console.WriteLine("Original {0} --> {1}", entity.LINQEntityGUID, entity.GetType().Name);
            }

            Console.WriteLine();
            Console.WriteLine("Press Any Key to Continue....");
            Console.ReadKey();
            Console.WriteLine();

            // Update Order via root object, creating a new data context
            using(NorthWindDataContext db = new NorthWindDataContext())
            {
                Console.WriteLine();
                Console.WriteLine("--------------------------------");
                Console.WriteLine("--    Commit Modified Data    --");
                Console.WriteLine("--------------------------------"); 
                
                db.DeferredLoadingEnabled = false;
                customer.SynchroniseWithDataContext(db,true);
                db.Log = Console.Out;
                db.SubmitChanges();

            }

            Console.WriteLine();
            Console.WriteLine("Press Any Key to Continue....");
            Console.ReadKey();
            Console.WriteLine();

            /// WE ARE NOW DISCONNECTED AGAIN! ///


            //Print out a list of original objects
            Console.WriteLine();
            Console.WriteLine("-----------------------");
            Console.WriteLine("Entities After Changes");
            Console.WriteLine("-----------------------");

            foreach (LINQEntityBase entity in customer.ToEntityTree())
            {
                Console.WriteLine("Original {0} --> {1}", entity.LINQEntityGUID, entity.GetType().Name);
            }

            Console.WriteLine();
            Console.WriteLine("Press Any Key to Continue....");
            Console.ReadKey();
        }



    }
}
