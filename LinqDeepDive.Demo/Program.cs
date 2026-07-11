using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

var connectionString = Environment.GetEnvironmentVariable("LINQ_DEMO_SQLSERVER")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=LinqDeepDiveDb;Trusted_Connection=True;TrustServerCertificate=True;";

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.ClearProviders();
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Warning);
});

var options = new DbContextOptionsBuilder<DemoDbContext>()
    .UseSqlServer(connectionString)
    .UseLoggerFactory(loggerFactory)
    .Options;

await using var context = new DemoDbContext(options);
await context.Database.EnsureDeletedAsync();
await context.Database.EnsureCreatedAsync();

if (!await context.Customers.AnyAsync())
{
    await SeedData.RunAsync(context);
}

Console.WriteLine("LINQ Deep Dive Demo");
Console.WriteLine($"Customers: {await context.Customers.CountAsync()}");
Console.WriteLine($"Orders: {await context.Orders.CountAsync()}");
Console.WriteLine();

await DemoScenarios.IEnumerableVsIQueryable(context);
await DemoScenarios.DeferredVsImmediate(context);
await DemoScenarios.SqlTranslation(context);
await DemoScenarios.FilterProjectionChaining(context);
await DemoScenarios.CommonMistakes(context);
await DemoScenarios.SameQueryDifferentWays(context);

public sealed class DemoDbContext(DbContextOptions<DemoDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();

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

public sealed class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public List<Order> Orders { get; set; } = [];
}

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

public sealed record OrderView(int OrderId, string CustomerName, decimal Total, string Category);

public static class SeedData
{
    public static async Task RunAsync(DemoDbContext context)
    {
        var random = new Random(7);
        var cities = new[] { "Cairo", "Dubai", "Riyadh", "Amman", "Doha", "Muscat" };
        var categories = new[] { "Books", "Electronics", "Clothes", "Grocery", "Sports" };
        var customers = Enumerable.Range(1, 1200).Select(i => new Customer
        {
            Name = $"Customer {i}",
            City = cities[random.Next(cities.Length)]
        }).ToList();

        await context.Customers.AddRangeAsync(customers);
        await context.SaveChangesAsync();

        var orders = new List<Order>(20000);
        foreach (var customer in customers)
        {
            var orderCount = random.Next(8, 25);
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

        await context.Orders.AddRangeAsync(orders);
        await context.SaveChangesAsync();
    }
}

public static class DemoScenarios
{
    public static async Task IEnumerableVsIQueryable(DemoDbContext context)
    {
        Console.WriteLine("1) IEnumerable vs IQueryable");
        var limit = 2000m;

        var sw = Stopwatch.StartNew();
        var allOrders = await context.Orders.AsNoTracking().ToListAsync();
        var inMemory = allOrders.Where(o => o.Total >= limit).Select(o => o.Id).Take(10).ToList();
        sw.Stop();
        Console.WriteLine($"IEnumerable path: {sw.ElapsedMilliseconds} ms, rows loaded: {allOrders.Count}, result: {inMemory.Count}");

        sw.Restart();
        var queryable = await context.Orders.AsNoTracking()
            .Where(o => o.Total >= limit)
            .OrderBy(o => o.Id)
            .Select(o => o.Id)
            .Take(10)
            .ToListAsync();
        sw.Stop();
        Console.WriteLine($"IQueryable path: {sw.ElapsedMilliseconds} ms, rows loaded: {queryable.Count}, result: {queryable.Count}");
        Console.WriteLine();
    }

    public static async Task DeferredVsImmediate(DemoDbContext context)
    {
        Console.WriteLine("2) Deferred execution vs Immediate execution");
        var query = context.Orders.AsNoTracking().Where(o => !o.IsPaid);
        var before = await query.CountAsync();

        context.Orders.Add(new Order
        {
            CustomerId = 1,
            Total = 1200,
            Category = "Electronics",
            CreatedAtUtc = DateTime.UtcNow,
            IsPaid = false
        });
        await context.SaveChangesAsync();

        var after = await query.CountAsync();
        Console.WriteLine($"Deferred query count before insert: {before}, after insert: {after}");

        var immediate = await context.Orders.AsNoTracking().Where(o => !o.IsPaid).ToListAsync();
        context.Orders.Add(new Order
        {
            CustomerId = 1,
            Total = 1300,
            Category = "Electronics",
            CreatedAtUtc = DateTime.UtcNow,
            IsPaid = false
        });
        await context.SaveChangesAsync();
        Console.WriteLine($"Immediate list size stays fixed: {immediate.Count}");
        Console.WriteLine();
    }

    public static Task SqlTranslation(DemoDbContext context)
    {
        Console.WriteLine("3) LINQ to SQL translation (EF Core)");
        var query = context.Orders.AsNoTracking()
            .Where(o => o.Total > 2500 && o.IsPaid)
            .OrderByDescending(o => o.Total)
            .Select(o => new { o.Id, o.Total, o.Customer.Name, o.Category })
            .Take(5);

        Console.WriteLine("Generated SQL:");
        Console.WriteLine(query.ToQueryString());
        Console.WriteLine();
        return Task.CompletedTask;
    }

    public static async Task FilterProjectionChaining(DemoDbContext context)
    {
        Console.WriteLine("4) Filtering, projection, chaining");
        var data = await context.Orders.AsNoTracking()
            .Where(o => o.IsPaid && o.Category == "Electronics")
            .OrderByDescending(o => o.Total)
            .Select(o => new OrderView(o.Id, o.Customer.Name, o.Total, o.Category))
            .Take(8)
            .ToListAsync();

        foreach (var item in data)
        {
            Console.WriteLine($"{item.OrderId} | {item.CustomerName} | {item.Total} | {item.Category}");
        }

        Console.WriteLine();
    }

    public static async Task CommonMistakes(DemoDbContext context)
    {
        Console.WriteLine("5) Common mistakes affecting performance");

        var sw = Stopwatch.StartNew();
        var bad = await context.Orders.AsNoTracking().ToListAsync();
        var badCount = bad.Where(o => o.Total > 1500 && o.Category == "Books").Count();
        sw.Stop();
        Console.WriteLine($"Mistake: early ToList then filter in memory -> {sw.ElapsedMilliseconds} ms, count: {badCount}");

        sw.Restart();
        var goodCount = await context.Orders.AsNoTracking()
            .Where(o => o.Total > 1500 && o.Category == "Books")
            .CountAsync();
        sw.Stop();
        Console.WriteLine($"Better: keep filtering in SQL -> {sw.ElapsedMilliseconds} ms, count: {goodCount}");

        sw.Restart();
        var tracked = await context.Orders.Where(o => o.Total > 3000).OrderBy(o => o.Id).Take(500).ToListAsync();
        sw.Stop();
        Console.WriteLine($"Tracking query -> {sw.ElapsedMilliseconds} ms, loaded: {tracked.Count}");

        sw.Restart();
        var noTracked = await context.Orders.AsNoTracking().Where(o => o.Total > 3000).OrderBy(o => o.Id).Take(500).ToListAsync();
        sw.Stop();
        Console.WriteLine($"AsNoTracking query -> {sw.ElapsedMilliseconds} ms, loaded: {noTracked.Count}");
        Console.WriteLine();
    }

    public static async Task SameQueryDifferentWays(DemoDbContext context)
    {
        Console.WriteLine("6) Same query in multiple ways");
        var minTotal = 2000m;
        var category = "Sports";

        var methodSyntax = await context.Orders.AsNoTracking()
            .Where(o => o.Total >= minTotal && o.Category == category && o.IsPaid)
            .OrderByDescending(o => o.Total)
            .Select(o => new OrderView(o.Id, o.Customer.Name, o.Total, o.Category))
            .Take(5)
            .ToListAsync();

        var querySyntax = await (
            from o in context.Orders.AsNoTracking()
            where o.Total >= minTotal && o.Category == category && o.IsPaid
            orderby o.Total descending
            select new OrderView(o.Id, o.Customer.Name, o.Total, o.Category)
        ).Take(5).ToListAsync();

        var splitStyleBase = context.Orders.AsNoTracking()
            .Where(o => o.Total >= minTotal)
            .Where(o => o.Category == category)
            .Where(o => o.IsPaid);
        var splitStyle = await splitStyleBase
            .OrderByDescending(o => o.Total)
            .Select(o => new OrderView(o.Id, o.Customer.Name, o.Total, o.Category))
            .Take(5)
            .ToListAsync();

        Console.WriteLine($"Method syntax count: {methodSyntax.Count}");
        Console.WriteLine($"Query syntax count: {querySyntax.Count}");
        Console.WriteLine($"Split chain count: {splitStyle.Count}");
        Console.WriteLine();
    }
}
