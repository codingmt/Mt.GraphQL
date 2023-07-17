using Mt.GraphQL.Api.Test.Web.Core.Models;
using System.Data.Entity;
using System.Linq;

namespace Mt.GraphQL.Api.Test.Web.Core.EF
{
    public class DbContext : System.Data.Entity.DbContext
    {
        public DbContext() : base(Effort.DbConnectionFactory.CreateTransient(), true)
        {
            if (!Entities.Any())
            {
                Entities.AddRange(
                    new[]
                    {
                        new Entity{ Id = 11, Name = "Related to A" },
                        new Entity{ Id = 12, Name = "Related to B" },
                        new Entity{ Id = 13, Name = "Related to C" },
                        new Entity{ Id = 15, Name = "Related to E" },
                        new Entity{ Id = 16, Name = "Related to F" },
                        new Entity { Id = 1, Name= "A", Description = "Entity A", Related_Id = 11 },
                        new Entity { Id = 2, Name= "B", Description = "Entity B", Related_Id = 12 },
                        new Entity { Id = 3, Name= "C", Description = "Entiteit C", Related_Id = 13 },
                        new Entity { Id = 4, Name= "D", Description = "Entiteit D" },
                        new Entity { Id = 5, Name= "E", Description = "Entity E", Related_Id = 15 },
                        new Entity { Id = 6, Name= "F", Description = "Entity F", Related_Id = 16 }
                    });
                SaveChanges();
            }
        }

        public DbSet<Entity> Entities { get; set; }
    }
}