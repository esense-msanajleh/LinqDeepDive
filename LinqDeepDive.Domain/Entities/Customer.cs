namespace LinqDeepDive.Domain.Entities;

public sealed class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public List<Order> Orders { get; set; } = [];
}
