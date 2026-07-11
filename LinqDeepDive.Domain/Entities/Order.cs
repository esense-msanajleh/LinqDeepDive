namespace LinqDeepDive.Domain.Entities;

public sealed class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public decimal Total { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public bool IsPaid { get; set; }
}
