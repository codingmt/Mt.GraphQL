using System.ComponentModel.DataAnnotations;

namespace Mt.GraphQL.Api.Test.Web.Core.Models
{
    public abstract class ModelBase
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; } = new DateTime(2001, 1, 1);
    }
}
