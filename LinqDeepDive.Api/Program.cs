using LinqDeepDive.Application;
using LinqDeepDive.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=LinqDeepDiveDb;Trusted_Connection=True;TrustServerCertificate=True;";
var forceRecreate = builder.Configuration.GetValue<bool>("DemoSeed:ForceRecreate");
var customerCount = builder.Configuration.GetValue<int?>("DemoSeed:CustomerCount") ?? 8000;
var minOrdersPerCustomer = builder.Configuration.GetValue<int?>("DemoSeed:MinOrdersPerCustomer") ?? 20;
var maxOrdersPerCustomer = builder.Configuration.GetValue<int?>("DemoSeed:MaxOrdersPerCustomer") ?? 40;

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddCors(options =>
{
    options.AddPolicy("ui", policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();
await app.Services.SeedAsync(forceRecreate, customerCount, minOrdersPerCustomer, maxOrdersPerCustomer);
app.UseCors("ui");
app.UseAuthorization();
app.MapControllers();
app.Run();
