using System.ComponentModel.DataAnnotations;

namespace Mt.GraphQL.Api.Test.Web.Core.Models
{
    public class Customer : ModelBase
    {
        [StringLength(50)]
        public string Name { get; set; }
        [StringLength(10)]
        public string Code { get; internal set; }

        public List<Contact> Contacts { get; set; }
    }
}
