using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mt.GraphQL.Api.Test.Web.NetFramework.Models
{
    public class Entity
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public bool IsCustomer { get; set; }
        public DateTime? Date { get; set; }

        [ForeignKey(nameof(Related))]
        public int? Related_Id { get; set; }
        public Entity Related { get; set; }
    }
}