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
using System.Runtime.Serialization;
using System.Xml;

namespace LINQEntityBaseExample
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        public static string SerializeEntity<T>(T entitySource, IEnumerable<Type> KnownTypes)
        {
            DataContractSerializer dcs;
            if (KnownTypes == null)
                dcs = new DataContractSerializer(entitySource.GetType());
            else
                dcs = new DataContractSerializer(entitySource.GetType(), KnownTypes);
            if (entitySource == null)
                return null;
            StringBuilder sb = new StringBuilder();
            XmlWriter xmlw = XmlWriter.Create(sb);
            dcs.WriteObject(xmlw, entitySource);
            xmlw.Close();
            return sb.ToString();
        }

        public static object DeserializeEntity(string entitySource, Type entityType, IEnumerable<Type> KnownTypes)
        {
            DataContractSerializer dcs;

            object entityTarget;
            if (entityType == null)
                return null;

            if (KnownTypes == null)
                dcs = new DataContractSerializer(entityType);
            else
                dcs = new DataContractSerializer(entityType, KnownTypes);
            StringReader sr = new StringReader(entitySource);
            XmlTextReader xmltr = new XmlTextReader(sr);
            entityTarget = (object)dcs.ReadObject(xmltr);
            xmltr.Close();
            return entityTarget;
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
                // 4. Delete Record, 
                // 5. Re-Attach to new DataContext + Commit

                // NOTES:
                // Hierarchy is :
                // Customer --> Order --> Order Detail
                // Product is not updateable because it is not in hierarchy, 
                // as it is only referenced.

                Customer customer;
                StringWriter consoleOutput = new StringWriter();
                Console.SetOut(consoleOutput);

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


                Console.WriteLine("-----------------------");
                Console.WriteLine("--    Modify Data    --");
                Console.WriteLine("-----------------------");

                // Tell the root object it's doing the change tracking                              
                customer.SetAsChangeTrackingRoot(chkKeepOriginals.Checked);

                /// WE ARE NOW DISCONNECTED ///

                // Update Customer
                string fax = customer.Fax;
                string phone = customer.Phone;
                customer.Fax = "Fax";
                customer.Phone = "Phone";
                Console.WriteLine("Modified {0} --> {1}", customer.LINQEntityGUID, customer.GetType().Name);

                // Add an order record
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
                                            where o.Freight < 20 && o.LINQEntityState != EntityState.New
                                            select o).ToList();

                foreach (Order orderDeleted in ordersDelete)
                {
                    customer.Orders.Remove(orderDeleted);
                    Console.WriteLine("Deleted  {0} --> {1}", orderDeleted.LINQEntityGUID, orderDeleted.GetType().Name);
                }

                Console.WriteLine("--------------------------------");
                Console.WriteLine("--    Review Modified Data    --");
                Console.WriteLine("--------------------------------");
                foreach (LINQEntityBase entity in customer.ToEntityTree())
                {
                    if (entity.LINQEntityState == EntityState.Deleted)
                        Console.WriteLine("Deleted  {0} --> {1}", entity.LINQEntityGUID, entity.GetType().Name);
                    if (entity.LINQEntityState == EntityState.New)
                        Console.WriteLine("Added    {0} --> {1}", entity.LINQEntityGUID, entity.GetType().Name);
                    if (entity.LINQEntityState == EntityState.Modified)
                        Console.WriteLine("Modified {0} --> {1}", entity.LINQEntityGUID, entity.GetType().Name);
                    if (entity.LINQEntityState == EntityState.Original)
                        Console.WriteLine("Original {0} --> {1}", entity.LINQEntityGUID, entity.GetType().Name);
                }

                Console.WriteLine();
                Console.WriteLine("--------------------------------");
                Console.WriteLine("--         Serialize          --");
                Console.WriteLine("--(Simulate WCF Serialization)--");
                Console.WriteLine("--------------------------------");
                Console.WriteLine();

                // serialize the customer to a string
                string serialized = SerializeEntity(customer, null);                
                string bookmark = customer.LINQEntityGUID;
                customer = null;

                consoleOutput.WriteLine(serialized);

                Console.WriteLine();
                Console.WriteLine("----------------------------------");
                Console.WriteLine("--         Deserialize          --");
                Console.WriteLine("--(Simulate WCF Deserialization)--");
                Console.WriteLine("----------------------------------");
                Console.WriteLine();

                Customer deserialized;

                // deserialize the customer string back into an object
                deserialized = DeserializeEntity(serialized, typeof(Customer), null) as Customer;
                customer = deserialized;

                Console.WriteLine();
                Console.WriteLine("--------------------------------");
                Console.WriteLine("--    Review Modified Data    --");
                Console.WriteLine("--------------------------------");
                foreach (LINQEntityBase entity in customer.ToEntityTree())
                {
                    if (entity.LINQEntityState == EntityState.Deleted)
                        Console.WriteLine("Deleted  {0} --> {1}", entity.LINQEntityGUID, entity.GetType().Name);
                    if (entity.LINQEntityState == EntityState.New)
                        Console.WriteLine("Added    {0} --> {1}", entity.LINQEntityGUID, entity.GetType().Name);
                    if (entity.LINQEntityState == EntityState.Modified)
                        Console.WriteLine("Modified {0} --> {1}", entity.LINQEntityGUID, entity.GetType().Name);
                    if (entity.LINQEntityState == EntityState.Original)
                        Console.WriteLine("Original {0} --> {1}", entity.LINQEntityGUID, entity.GetType().Name);
                }

                // Update Order via root object, creating a new data context
                using (NorthWindDataContext db = new NorthWindDataContext())
                {
                    Console.WriteLine();
                    Console.WriteLine("--------------------------------");
                    Console.WriteLine("--    Commit Modified Data    --");
                    Console.WriteLine("--------------------------------");

                    db.DeferredLoadingEnabled = false;
                    customer.SynchroniseWithDataContext(db, true); //cascade delete
                    db.Log = Console.Out;
                    db.SubmitChanges();

                }

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
