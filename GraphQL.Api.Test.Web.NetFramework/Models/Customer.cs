using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Mt.GraphQL.Api.Test.Web.NetFramework.Models
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }

        public List<Contact> Contacts { get; set; }
    }
}