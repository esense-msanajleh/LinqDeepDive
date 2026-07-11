using LinqDeepDive.Domain.Entities;

namespace LinqDeepDive.Application.Abstractions;

public interface IAppDbContext
{
    IQueryable<Order> Orders { get; }
    IQueryable<Customer> Customers { get; }
}
