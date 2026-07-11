using LinqDeepDive.Application.Abstractions;
using LinqDeepDive.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinqDeepDive.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Customer> CustomersSet => Set<Customer>();
    public DbSet<Order> OrdersSet => Set<Order>();

    public IQueryable<Order> Orders => OrdersSet;
    public IQueryable<Customer> Customers => CustomersSet;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>()
            .HasMany(x => x.Orders)
            .WithOne(x => x.Customer)
            .HasForeignKey(x => x.CustomerId);

        modelBuilder.Entity<Customer>()
            .Property(x => x.Name)
            .HasMaxLength(100);

        modelBuilder.Entity<Order>()
            .Property(x => x.Category)
            .HasMaxLength(50);

        modelBuilder.Entity<Order>()
            .Property(x => x.Total)
            .HasPrecision(18, 2);
    }
}
