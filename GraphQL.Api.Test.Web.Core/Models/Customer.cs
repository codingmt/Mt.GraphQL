namespace Mt.GraphQL.Api.Test.Web.Core.Models
{
    public class Customer : ModelBase
    {
        public string Name { get; set; }
        public string Code { get; internal set; }

        public List<Contact> Contacts { get; set; }
    }
}
