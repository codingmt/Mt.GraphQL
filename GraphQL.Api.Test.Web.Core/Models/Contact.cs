using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mt.GraphQL.Api.Test.Web.Core.Models
{
    public class Contact : ModelBase
    {
        [StringLength(50)]
        public string Name { get; set; }
        [StringLength(50)]
        public string Function { get; set; }
        public bool IsAuthorizedToSign { get; set; }
        public DateTime? DateOfBirth { get; set; }

        [ForeignKey(nameof(Customer))]
        public int Customer_Id { get; set; }
        public Customer Customer { get; set; }
        public bool FunctionIsSet 
        {
            get => Function != null;
        }
    }
}