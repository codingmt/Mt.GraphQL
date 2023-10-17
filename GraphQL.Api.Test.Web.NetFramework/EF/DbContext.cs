using Mt.GraphQL.Api.Test.Web.NetFramework.Models;
using System;
using System.Data.Entity;
using System.Linq;

namespace Mt.GraphQL.Api.Test.Web.NetFramework.EF
{
    public class DbContext : System.Data.Entity.DbContext
    {
        public DbContext() : base(Effort.DbConnectionFactory.CreateTransient(), true)
        {
            if (!Customers.Any())
            {
                Customers.AddRange(
                    new[]
                    {
                        new Customer{ Id = 1, Name = "Customer 1" },
                        new Customer{ Id = 2, Name = "Customer 2" },
                        new Customer{ Id = 3, Name = "Customer 3" }
                    });

                Contacts.AddRange(
                    new[]
                    {
                        new Contact{ Id = 1, Name = "Contact 1.1", Function = "CEO", IsAuthorizedToSign = true, DateOfBirth = new DateTime(1970, 5, 15), Customer_Id = 1 },
                        new Contact{ Id = 2, Name = "Contact 1.2", Function = "Secretary", IsAuthorizedToSign = false, DateOfBirth = new DateTime(1980, 6, 16), Customer_Id = 1 },
                        new Contact{ Id = 3, Name = "Contact 1.3", Function = "Sales Mgr", IsAuthorizedToSign = false, DateOfBirth = new DateTime(1990, 7, 17), Customer_Id = 1 },

                        new Contact{ Id = 4, Name = "Contact 2.1", Function = "CEO", IsAuthorizedToSign = true, DateOfBirth = new DateTime(1971, 5, 18), Customer_Id = 2 }
                    });

                SaveChanges();
            }
        }

        public DbSet<Customer> Customers { get; set; }

        public DbSet<Contact> Contacts { get; set; }
    }
}