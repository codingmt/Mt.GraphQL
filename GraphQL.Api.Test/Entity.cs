namespace Mt.GraphQL.Api.Test
{
    internal class Entity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Type { get; set; }
        public bool IsCustomer { get; set; }
        public DateTime? Date { get; set; }
        public int? Related_Id { get; set; }
        public Entity? Related { get; set; }
    }
}
