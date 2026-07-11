# LINQ Deep Dive Presentation

## Slide 1 - Title

LINQ Deep Dive in .NET 10  
Execution behavior, SQL translation, and performance impact

## Slide 2 - Goal

- Understand how LINQ executes
- Compare in-memory vs database execution
- See why query style affects speed and memory
- Learn practical rules for writing fast queries

## Slide 3 - Demo Setup

- .NET 10 Console App
- EF Core with SQL Server
- Customers + Orders dataset
- Seeded test data for realistic timings

## Slide 4 - IEnumerable vs IQueryable

- `IEnumerable` runs in memory
- `IQueryable` builds expression trees for provider execution
- With EF Core, `IQueryable` translates to SQL and filters at database side
- Result: less data transfer and better performance

Live demo:

- Show scenario 1 output
- Compare rows loaded and elapsed milliseconds

## Slide 5 - Deferred vs Immediate Execution

- Deferred: query runs only when enumerated
- Immediate: `ToList`, `Count`, `First` execute now
- Re-running a deferred query can return updated data
- Materialized list stays fixed after load

Live demo:

- Show scenario 2 output before/after insert

## Slide 6 - LINQ to SQL Translation

- EF Core converts LINQ expression into SQL
- You can inspect SQL with `ToQueryString()`
- Good LINQ shape produces efficient SQL

Live demo:

- Show scenario 3 generated SQL text

## Slide 7 - Filtering, Projection, Chaining

- Filter early with `Where`
- Project only needed columns with `Select`
- Chain operators in readable sequence
- Keep queries clear and minimal

Live demo:

- Show scenario 4 top results

## Slide 8 - Common Mistakes

- Calling `ToList` too early
- Filtering in memory instead of SQL
- Tracking read-only data when not needed
- Forgetting `AsNoTracking` for read scenarios

Live demo:

- Show scenario 5 timing comparison

## Slide 9 - Same Query, Different Styles

- Method syntax
- Query syntax
- Split chained style
- Same semantics, same result, different readability choices

Live demo:

- Show scenario 6 counts

## Slide 10 - Key Takeaways

- Prefer `IQueryable` for EF Core query composition
- Delay materialization until final step
- Filter and project in SQL
- Use `AsNoTracking` for read-heavy queries
- Measure with real data and execution timing

## Slide 11 - Q&A

Which LINQ style is most readable for your team and still keeps SQL efficient?
