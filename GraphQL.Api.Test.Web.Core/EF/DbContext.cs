using Mt.GraphQL.Api.Test.Web.Core.Models;
using System.Data.Entity;

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
                        new Entity{ Id = 1, Name = "Related to A" },
                        new Entity{ Id = 2, Name = "Related to B" },
                        new Entity{ Id = 3, Name = "Related to C" },
                        new Entity{ Id = 4, Name = "Related to E" },
                        new Entity{ Id = 5, Name = "Related to F" },
                        new Entity { Id = 6, Name= "A", Description = "Entity A", Related_Id = 1 },
                        new Entity { Id = 7, Name= "B", Description = "Entity B", Related_Id = 2 },
                        new Entity { Id = 8, Name= "C", Description = "Entiteit C", Related_Id = 3 },
                        new Entity { Id = 9, Name= "D", Description = "Entiteit D" },
                        new Entity { Id = 10, Name= "E", Description = "Entity E", Related_Id = 4 },
                        new Entity { Id = 11, Name= "F", Description = "Entity F", Related_Id = 5 }
                    });
                SaveChanges();
            }
        }

        public DbSet<Entity> Entities { get; set; }
    }
}