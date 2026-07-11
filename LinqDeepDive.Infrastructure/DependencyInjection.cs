using LinqDeepDive.Application.Abstractions;
using LinqDeepDive.Domain.Entities;
using LinqDeepDive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LinqDeepDive.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        return services;
    }

    public static async Task SeedAsync(
        this IServiceProvider serviceProvider,
        bool forceRecreate,
        int customerCount,
        int minOrdersPerCustomer,
        int maxOrdersPerCustomer)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (forceRecreate)
        {
            await context.Database.EnsureDeletedAsync();
        }

        await context.Database.EnsureCreatedAsync();

        if (await context.CustomersSet.AnyAsync())
        {
            return;
        }

        var random = new Random(7);
        var cities = new[] { "Cairo", "Dubai", "Riyadh", "Amman", "Doha", "Muscat" };
        var categories = new[] { "Books", "Electronics", "Clothes", "Grocery", "Sports" };
        var customers = Enumerable.Range(1, customerCount).Select(i => new Customer
        {
            Name = $"Customer {i}",
            City = cities[random.Next(cities.Length)]
        }).ToList();

        await context.CustomersSet.AddRangeAsync(customers);
        await context.SaveChangesAsync();

        var totalCapacity = customerCount * Math.Max(1, (minOrdersPerCustomer + maxOrdersPerCustomer) / 2);
        var orders = new List<Order>(totalCapacity);
        foreach (var customer in customers)
        {
            var orderCount = random.Next(minOrdersPerCustomer, maxOrdersPerCustomer + 1);
            for (var i = 0; i < orderCount; i++)
            {
                orders.Add(new Order
                {
                    CustomerId = customer.Id,
                    Total = random.Next(20, 5000),
                    Category = categories[random.Next(categories.Length)],
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-random.Next(1, 365)),
                    IsPaid = random.NextDouble() > 0.2
                });
            }
        }

        const int batchSize = 20000;
        for (var i = 0; i < orders.Count; i += batchSize)
        {
            var batch = orders.Skip(i).Take(batchSize);
            await context.OrdersSet.AddRangeAsync(batch);
            await context.SaveChangesAsync();
        }
    }
}
