using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mt.GraphQL.Api.Test.Web.Core.Models
{
    public class Contact
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Function { get; set; }
        public bool IsAuthorizedToSign { get; set; }
        public DateTime? DateOfBirth { get; set; }

        [ForeignKey(nameof(Customer))]
        public int Customer_Id { get; set; }
        public Customer Customer { get; set; }
    }
}