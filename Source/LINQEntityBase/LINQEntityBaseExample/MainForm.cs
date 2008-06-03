using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.Linq;
using LINQEntityBaseExampleData;

namespace LINQEntityBaseExample
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;

                // EXAMPLE: 
                // 1. Detach From Original
                // 2. Add/Modify
                // 3. Re-Attach to new DataContext + Commit
                // 4. Grab the end results from the database

                // NOTES:
                // Hierarchy is :
                // Customer --> Order --> Order Detail
                // Product is not updateable because it is not in hierarchy, 
                // as it is only referenced.

                Customer customer;
                string DBLog = "";
                WCFService.ServiceClient WCFDataService;

                StringWriter consoleOutput = new StringWriter();
                Console.SetOut(consoleOutput);

                Console.WriteLine("-------------------------");
                Console.WriteLine("-- Retrieving Entities --");
                Console.WriteLine("-------------------------");

                //Get the customer and related children via WCF
                WCFDataService = new WCFService.ServiceClient();
                customer = WCFDataService.RetrieveCustomerDataByID(out DBLog, "ALFKI");
                Console.WriteLine(DBLog);

                List<Product> products = WCFDataService.GetProductData(out DBLog).ToList();
                Console.WriteLine(DBLog);                



                //Print out a list of original objects before modification
                Console.WriteLine("-----------------------");
                Console.WriteLine("-- Values  Retrieved --");
                Console.WriteLine("-----------------------");

                foreach (LINQEntityBase entity in customer.ToEntityTree())
                {
                    Console.WriteLine("Original {0} --> {1}", entity.LINQEntityGUID, entity.GetType().Name);
                }

                Console.WriteLine();


                Console.WriteLine("-----------------------");
                Console.WriteLine("--    Modify Data    --");
                Console.WriteLine("-----------------------");

                // Tell the root object it's doing the change tracking                              
                customer.SetAsChangeTrackingRoot(chkKeepOriginals.Checked);

                // Update Customer
                string fax = customer.Fax;
                string phone = customer.Phone;
                customer.Fax = "Fax";
                customer.Phone = "Phone";
                Console.WriteLine("Modified {0} --> {1}", customer.LINQEntityGUID, customer.GetType().Name);

                // Add an order record without children
                Order orderAdded = new Order()
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

                customer.Orders.Add(orderAdded);

                Console.WriteLine("Added    {0} --> {1}", orderAdded.LINQEntityGUID, orderAdded.GetType().Name);

                // Add an order record with child records
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

                // Add order details
                Order_Detail order_DetailAdded = new Order_Detail()
                {
                    UnitPrice = 55,
                    Quantity = 10,
                    Discount = 0.15F
                };

                order_DetailAdded.Product = products.FirstOrDefault();

                orderAdded.Order_Details.Add(order_DetailAdded);

                Console.WriteLine("Added    {0} --> {1}", orderAdded.LINQEntityGUID, orderAdded.GetType().Name);
                Console.WriteLine("Added    {0} --> {1}", order_DetailAdded.LINQEntityGUID, order_DetailAdded.GetType().Name);

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
                                            where o.Freight < 20 && o.LINQEntityState != EntityState.New
                                            select o).ToList();

                foreach (Order orderDeleted in ordersDelete)
                {
                    orderDeleted.SetAsDeleteOnSubmit();
                    Console.WriteLine("Deleted  {0} --> {1}", orderDeleted.LINQEntityGUID, orderDeleted.GetType().Name);
                }

                Console.WriteLine();
                Console.WriteLine("-----------------------");
                Console.WriteLine("--    Review Data    --");
                Console.WriteLine("-----------------------");
                foreach (LINQEntityBase entity in customer.ToEntityTree())
                {
                    if (entity.LINQEntityState == EntityState.Deleted)
                        Console.WriteLine("Deleted  {0} --> {1}", entity.LINQEntityGUID, entity.GetType().Name);
                    if (entity.LINQEntityState == EntityState.Detached)
                        Console.WriteLine("Detached  {0} --> {1}", entity.LINQEntityGUID, entity.GetType().Name);
                    if (entity.LINQEntityState == EntityState.New)
                        Console.WriteLine("Added    {0} --> {1}", entity.LINQEntityGUID, entity.GetType().Name);
                    if (entity.LINQEntityState == EntityState.Modified)
                        Console.WriteLine("Modified {0} --> {1}", entity.LINQEntityGUID, entity.GetType().Name);
                    if (entity.LINQEntityState == EntityState.Original)
                        Console.WriteLine("Original {0} --> {1}", entity.LINQEntityGUID, entity.GetType().Name);
                }

                /// Make the update to the database                
                Console.WriteLine();
                Console.WriteLine("---------------------------");
                Console.WriteLine("--    Update Database    --");
                Console.WriteLine("---------------------------");

                //Update the customer and related children via WCF
                //(Note that on the server side, where database update is made
                // cascading deletes = true)
                DBLog = WCFDataService.UpdateCustomerData(customer);
                Console.WriteLine(DBLog);

                Console.WriteLine();
                Console.WriteLine("---------------------------------");
                Console.WriteLine("--    GetData from Database    --");
                Console.WriteLine("---------------------------------");

                // Get the customer data again from the database
                DBLog = "";
                customer = WCFDataService.RetrieveCustomerDataByID(out DBLog, "ALFKI");
                consoleOutput.WriteLine(DBLog);

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
                Console.WriteLine("-----------------------");
                Console.WriteLine("    Demo Finished      ");
                Console.WriteLine("-----------------------");

                txtInfo.Clear();
                txtInfo.AppendText(consoleOutput.ToString());
                txtInfo.SelectionStart = 0;
                txtInfo.ScrollToCaret();

            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

    }
}
