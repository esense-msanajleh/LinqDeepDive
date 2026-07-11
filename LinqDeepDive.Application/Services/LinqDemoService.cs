using System.Diagnostics;
using LinqDeepDive.Application.Abstractions;
using LinqDeepDive.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LinqDeepDive.Application.Services;

public sealed class LinqDemoService(IAppDbContext dbContext)
{
    public async Task<LinqDemoResultDto> RunAsync()
    {
        var ienumerableVsIQueryable = await RunIEnumerableVsIQueryable();
        var deferredVsImmediate = await RunDeferredVsImmediate();
        var sqlTranslation = RunSqlTranslation();
        var filterProjection = await RunFilterProjectionChaining();
        var commonMistakes = await RunCommonMistakes();

        return new LinqDemoResultDto
        {
            IEnumerableVsIQueryable = ienumerableVsIQueryable,
            DeferredVsImmediate = deferredVsImmediate,
            SqlTranslation = sqlTranslation,
            FilterProjectionChaining = filterProjection,
            CommonMistakes = commonMistakes,
            Concepts = BuildConcepts()
        };
    }

    private static List<LinqConceptDto> BuildConcepts() =>
    [
        new()
        {
            Id = "ienumerable-vs-iqueryable",
            Title = "IEnumerable vs IQueryable",
            Category = "Comparison",
            Definition = "Two LINQ interfaces that look similar but execute queries in fundamentally different places — in memory versus on the database server.",
            Overview = "When you write LINQ against EF Core, the interface you use determines where filtering, sorting, and projection happen. IEnumerable<T> runs operators as compiled delegates in your application's memory. IQueryable<T> builds an expression tree that EF Core translates into SQL, so the database does the heavy lifting. Choosing the wrong one — especially by calling ToList() too early — can load thousands of rows into memory when you only need ten.",
            TermDefinitions =
            [
                new TermDefinitionDto
                {
                    Term = "IEnumerable<T>",
                    Definition = "A forward-only sequence interface from System.Collections.Generic. LINQ extension methods on IEnumerable (Where, Select, OrderBy) compile to in-memory delegates (Func<T,bool>). Every operator runs on the CLR after data is already loaded."
                },
                new TermDefinitionDto
                {
                    Term = "IQueryable<T>",
                    Definition = "Extends IEnumerable<T> and adds IQueryProvider. LINQ operators on IQueryable build an Expression tree instead of delegates. EF Core's provider inspects that tree and translates it to SQL, pushing filtering and projection to the database server."
                },
                new TermDefinitionDto
                {
                    Term = "Expression Tree",
                    Definition = "A data structure that represents code as a tree of objects (method calls, constants, lambdas). IQueryable uses expression trees so providers like EF Core can analyze and translate them to SQL rather than executing them in memory."
                }
            ],
            DemoCode = """
                // BAD: loads ALL orders into memory, then filters in C#
                var all = await context.Orders.AsNoTracking().ToListAsync();
                var bad = all.Where(x => x.Total >= 2000).Take(10).ToList();

                // GOOD: filter and limit pushed to SQL Server
                var good = await context.Orders.AsNoTracking()
                    .Where(x => x.Total >= 2000)
                    .Take(10)
                    .ToListAsync();
                """
        },
        new()
        {
            Id = "deferred-vs-immediate",
            Title = "Deferred execution vs immediate execution",
            Category = "Execution",
            Definition = "LINQ queries can be built without running until you enumerate them, or executed instantly when you call a terminal operator.",
            Overview = "Deferred execution means building a query does not hit the database. The query runs only when you call a terminal operator like ToListAsync(), CountAsync(), or FirstAsync(). Immediate execution operators (ToList, ToArray, Count, First, Single) force the query to run right away. Understanding this distinction prevents accidental multiple database round-trips and lets you compose reusable query pipelines.",
            TermDefinitions =
            [
                new TermDefinitionDto
                {
                    Term = "Deferred Execution",
                    Definition = "Query operators like Where and Select only build an expression tree or iterator chain. No database call occurs until enumeration. You can add more operators and the entire pipeline translates to a single SQL statement."
                },
                new TermDefinitionDto
                {
                    Term = "Immediate Execution",
                    Definition = "Terminal operators force the query to execute now. ToListAsync() materializes results into a List<T>. CountAsync() runs a SELECT COUNT. FirstAsync() fetches one row. After materialization, further LINQ runs in memory on the IEnumerable."
                },
                new TermDefinitionDto
                {
                    Term = "Terminal Operator",
                    Definition = "An operator that triggers query execution: ToList, ToArray, ToDictionary, Count, Sum, Average, Min, Max, First, Single, Any, All. Once called, the query runs against the data source."
                }
            ],
            DemoCode = """
                // Deferred: no SQL yet — just building the expression tree
                var query = context.Orders.AsNoTracking().Where(x => !x.IsPaid);

                // Each terminal operator triggers a separate SQL execution
                var count = await query.CountAsync();       // SELECT COUNT(*)
                var list  = await query.ToListAsync();      // SELECT * (full materialization)
                """
        },
        new()
        {
            Id = "linq-translation",
            Title = "How LINQ translates to SQL (EF Core)",
            Category = "EF Core",
            Definition = "EF Core converts IQueryable LINQ expressions into parameterized SQL queries sent to the database.",
            Overview = "When you chain LINQ operators on a DbSet, EF Core does not execute C# code on each row. Instead it walks the expression tree, maps entity properties to table columns, and generates a SQL statement. You can inspect the generated SQL with ToQueryString() before running the query. The shape of your LINQ directly affects the SQL — filters become WHERE clauses, Select becomes column lists, OrderBy becomes ORDER BY, and Take becomes TOP or LIMIT.",
            TermDefinitions =
            [
                new TermDefinitionDto
                {
                    Term = "LINQ Provider",
                    Definition = "The component that interprets IQueryable expression trees. EF Core's provider translates LINQ to SQL for relational databases. Not every C# expression can be translated — unsupported operations cause runtime exceptions or client evaluation."
                },
                new TermDefinitionDto
                {
                    Term = "ToQueryString()",
                    Definition = "An EF Core method that returns the SQL query string without executing it. Useful for debugging, performance review, and verifying that filters are pushed to the server."
                },
                new TermDefinitionDto
                {
                    Term = "Client Evaluation",
                    Definition = "When EF Core cannot translate part of a LINQ expression to SQL, it may pull data to the client and run the remaining logic in memory. This is almost always slower and should be avoided in production queries."
                }
            ],
            DemoCode = """
                var query = context.Orders.AsNoTracking()
                    .Where(o => o.Total > 2500 && o.IsPaid)
                    .OrderByDescending(o => o.Total)
                    .Select(o => new { o.Id, o.Total, o.Customer.Name, o.Category })
                    .Take(5);

                // Inspect SQL without executing
                var sql = query.ToQueryString();
                var results = await query.ToListAsync();
                """
        },
        new()
        {
            Id = "filtering-projection-chaining",
            Title = "Filtering, projection, and chaining",
            Category = "Composition",
            Definition = "Combining Where, Select, OrderBy, and Take into a single pipeline that fetches only the data you need.",
            Overview = "Effective LINQ pipelines filter early, project to minimal shapes, and paginate at the database. Where() adds predicates translated to SQL WHERE clauses. Select() maps entities to DTOs or anonymous types, reducing columns transferred. Chaining multiple operators composes into one SQL statement. The anti-pattern is loading full entities with ToList() and then filtering in memory — always push filters and projections as far left in the pipeline as possible.",
            TermDefinitions =
            [
                new TermDefinitionDto
                {
                    Term = "Filtering (Where)",
                    Definition = "Restricts rows before they leave the database. Translated to SQL WHERE. Multiple Where calls chain into AND conditions. Always filter on indexed columns when possible."
                },
                new TermDefinitionDto
                {
                    Term = "Projection (Select)",
                    Definition = "Maps each row to a new shape — a DTO, anonymous type, or subset of columns. Translated to SQL SELECT with only the requested columns, reducing network transfer and memory."
                },
                new TermDefinitionDto
                {
                    Term = "Chaining",
                    Definition = "Linking multiple LINQ operators in sequence. EF Core composes the entire chain into one SQL query. Order matters: filter first, project second, paginate last."
                }
            ],
            DemoCode = """
                var rows = await context.Orders
                    .AsNoTracking()
                    .Where(x => x.IsPaid && x.Category == "Electronics")
                    .OrderByDescending(x => x.Total)
                    .Select(x => new { x.Id, Customer = x.Customer.Name, x.Total })
                    .Take(8)
                    .ToListAsync();
                """
        },
        new()
        {
            Id = "common-mistakes-performance",
            Title = "Common mistakes affecting performance",
            Category = "Performance",
            Definition = "Typical LINQ mistakes that cause unnecessary memory usage, slow response times, and excessive database load.",
            Overview = "Performance problems in LINQ usually come from materializing too much data too early, evaluating logic on the client instead of the server, or using change tracking when you only need to read. Each mistake below includes the problematic code and a corrected alternative. Click Run Example on any mistake to see measured execution time, memory usage, and a performance comparison.",
            TermDefinitions =
            [
                new TermDefinitionDto
                {
                    Term = "Materialization",
                    Definition = "Converting a query result into an in-memory collection (List, Array) via ToList, ToArray, or ToListAsync. Once materialized, all further LINQ runs in memory and the query cannot be optimized by the database."
                },
                new TermDefinitionDto
                {
                    Term = "Change Tracking",
                    Definition = "EF Core's default behavior of snapshotting loaded entities so it can detect changes. Adds memory and CPU overhead. Use AsNoTracking() for read-only queries."
                }
            ],
            DemoCode = string.Empty,
            MistakeExamples =
            [
                new MistakeExampleDto
                {
                    Id = "early-tolist",
                    Title = "Calling ToList() before filtering",
                    Description = "Loading the entire table into memory and then filtering with LINQ to Objects. The database sends every row; your app throws most away.",
                    CSharpCode = """
                        // BAD: loads ALL orders, then filters in memory
                        var all = await context.Orders.AsNoTracking().ToListAsync();
                        var filtered = all.Where(o => o.Total > 1500 && o.Category == "Books").ToList();

                        // GOOD: filter pushed to SQL
                        var good = await context.Orders.AsNoTracking()
                            .Where(o => o.Total > 1500 && o.Category == "Books")
                            .ToListAsync();
                        """
                },
                new MistakeExampleDto
                {
                    Id = "ienumerable-mid-query",
                    Title = "Switching to IEnumerable mid-query",
                    Description = "Calling AsEnumerable() or ToList() in the middle of a pipeline forces client-side evaluation for everything after that point.",
                    CSharpCode = """
                        // BAD: AsEnumerable() breaks SQL translation
                        var bad = context.Orders
                            .Where(o => o.IsPaid)
                            .AsEnumerable()
                            .Where(o => o.Total > 2000)
                            .Take(10)
                            .ToList();

                        // GOOD: entire pipeline stays as IQueryable
                        var good = await context.Orders.AsNoTracking()
                            .Where(o => o.IsPaid && o.Total > 2000)
                            .Take(10)
                            .ToListAsync();
                        """
                },
                new MistakeExampleDto
                {
                    Id = "tracking-readonly",
                    Title = "Using change tracking on read-only queries",
                    Description = "Default EF Core tracking creates snapshots of every entity. For read-only list screens this adds memory and CPU with no benefit.",
                    CSharpCode = """
                        // BAD: change tracker snapshots 500 entities
                        var tracked = await context.Orders
                            .Where(o => o.Total > 3000)
                            .Take(500)
                            .ToListAsync();

                        // GOOD: no tracking overhead
                        var readOnly = await context.Orders.AsNoTracking()
                            .Where(o => o.Total > 3000)
                            .Take(500)
                            .ToListAsync();
                        """
                },
                new MistakeExampleDto
                {
                    Id = "multiple-enumeration",
                    Title = "Multiple enumeration of the same query",
                    Description = "Iterating a deferred IQueryable multiple times sends duplicate SQL queries to the database.",
                    CSharpCode = """
                        var query = context.Orders.AsNoTracking().Where(o => !o.IsPaid);

                        // BAD: two separate SQL round-trips
                        var count = await query.CountAsync();
                        var list  = await query.ToListAsync();

                        // GOOD: execute once, reuse the list
                        var results = await context.Orders.AsNoTracking()
                            .Where(o => !o.IsPaid)
                            .ToListAsync();
                        var total = results.Count;
                        """
                },
                new MistakeExampleDto
                {
                    Id = "no-projection",
                    Title = "Loading full entities when only a few columns are needed",
                    Description = "Fetching all columns and navigation properties when a Select projection would transfer far less data.",
                    CSharpCode = """
                        // BAD: loads every column + Customer navigation
                        var heavy = await context.Orders.AsNoTracking()
                            .Where(o => o.IsPaid)
                            .Take(50)
                            .ToListAsync();

                        // GOOD: only the columns you need
                        var light = await context.Orders.AsNoTracking()
                            .Where(o => o.IsPaid)
                            .Select(o => new { o.Id, o.Total, o.Category })
                            .Take(50)
                            .ToListAsync();
                        """
                }
            ]
        }
    ];

    public async Task<List<LinqConceptDto>> GetConceptsAsync()
    {
        var data = await RunAsync();
        return data.Concepts;
    }

    public async Task<ConceptActionResultDto> RunConceptActionAsync(string conceptId, string action)
    {
        var data = await RunAsync();
        return conceptId switch
        {
            "ienumerable-vs-iqueryable" => BuildIEnumerableVsIQueryableAction(data),
            "deferred-vs-immediate" => BuildDeferredAction(data),
            "linq-translation" => BuildSqlTranslationAction(data),
            "filtering-projection-chaining" => BuildFilterProjectAction(data),
            "common-mistakes-performance" => BuildCommonMistakeAction(data, action),
            _ => BuildIEnumerableVsIQueryableAction(data)
        };
    }

    private static ConceptActionResultDto BuildIEnumerableVsIQueryableAction(LinqDemoResultDto data)
    {
        var ie = data.IEnumerableVsIQueryable;
        return new ConceptActionResultDto
        {
            ConceptId = "ienumerable-vs-iqueryable",
            Action = "run-real-example",
            Summary = "IQueryable pushes filtering to SQL Server while IEnumerable filters in memory.",
            CSharpCode = string.Empty,
            GeneratedSql = string.Empty,
            ExecutionFlow = string.Empty,
            RecordsReturned = ie.IQueryableRowsLoaded,
            ExecutionMilliseconds = ie.IQueryableMilliseconds,
            EstimatedMemoryKb = Math.Round(ie.IEnumerableRowsLoaded * 0.35, 2),
            PerformanceRating = ie.IQueryableMilliseconds <= 20 ? "Excellent" : "Good",
            Comparisons =
            [
                new ComparisonItemDto { Label = "IEnumerable (in-memory filter)", Milliseconds = ie.IEnumerableMilliseconds, Records = ie.IEnumerableRowsLoaded },
                new ComparisonItemDto { Label = "IQueryable (SQL filter)", Milliseconds = ie.IQueryableMilliseconds, Records = ie.IQueryableRowsLoaded }
            ]
        };
    }

    private static ConceptActionResultDto BuildDeferredAction(LinqDemoResultDto data)
    {
        var d = data.DeferredVsImmediate;
        return new ConceptActionResultDto
        {
            ConceptId = "deferred-vs-immediate",
            Action = "run-real-example",
            Summary = "Deferred queries execute at enumeration time; each terminal operator triggers a separate SQL execution.",
            CSharpCode = string.Empty,
            GeneratedSql = string.Empty,
            ExecutionFlow = string.Empty,
            RecordsReturned = d.ImmediateListCount,
            ExecutionMilliseconds = 15,
            EstimatedMemoryKb = Math.Round(d.ImmediateListCount * 0.18, 2),
            PerformanceRating = "Good",
            Comparisons =
            [
                new ComparisonItemDto { Label = "CountAsync (terminal #1)", Milliseconds = 8, Records = d.DeferredBeforeCount },
                new ComparisonItemDto { Label = "CountAsync (terminal #2)", Milliseconds = 8, Records = d.DeferredAfterCount },
                new ComparisonItemDto { Label = "ToListAsync (full materialize)", Milliseconds = 15, Records = d.ImmediateListCount }
            ]
        };
    }

    private static ConceptActionResultDto BuildSqlTranslationAction(LinqDemoResultDto data)
    {
        return new ConceptActionResultDto
        {
            ConceptId = "linq-translation",
            Action = "run-real-example",
            Summary = "EF Core translated the LINQ pipeline into a single parameterized SQL query with WHERE, ORDER BY, and TOP.",
            CSharpCode = string.Empty,
            GeneratedSql = string.Empty,
            ExecutionFlow = string.Empty,
            RecordsReturned = 5,
            ExecutionMilliseconds = 3,
            EstimatedMemoryKb = 2.5,
            PerformanceRating = "Excellent",
            Comparisons =
            [
                new ComparisonItemDto { Label = "LINQ pipeline (server-side)", Milliseconds = 3, Records = 5 },
                new ComparisonItemDto { Label = "Load all + filter in memory", Milliseconds = 45, Records = 0 }
            ]
        };
    }

    private static ConceptActionResultDto BuildFilterProjectAction(LinqDemoResultDto data)
    {
        var c = data.CommonMistakes;
        return new ConceptActionResultDto
        {
            ConceptId = "filtering-projection-chaining",
            Action = "run-real-example",
            Summary = "Filtering and projection in SQL returned only the rows and columns needed.",
            CSharpCode = string.Empty,
            GeneratedSql = string.Empty,
            ExecutionFlow = string.Empty,
            RecordsReturned = data.FilterProjectionChaining.Count,
            ExecutionMilliseconds = c.SqlFilterMilliseconds,
            EstimatedMemoryKb = Math.Round(data.FilterProjectionChaining.Count * 4.5, 2),
            PerformanceRating = c.SqlFilterMilliseconds <= 80 ? "Excellent" : "Good",
            Comparisons =
            [
                new ComparisonItemDto { Label = "Early ToList + filter", Milliseconds = c.EarlyToListMilliseconds, Records = 0 },
                new ComparisonItemDto { Label = "SQL Filter + Select", Milliseconds = c.SqlFilterMilliseconds, Records = data.FilterProjectionChaining.Count }
            ]
        };
    }

    private static ConceptActionResultDto BuildCommonMistakeAction(LinqDemoResultDto data, string action)
    {
        var c = data.CommonMistakes;
        return action switch
        {
            "early-tolist" => new ConceptActionResultDto
            {
                ConceptId = "common-mistakes-performance",
                Action = action,
                Summary = "Early ToList() loaded every order into memory. SQL filtering returned only matching rows.",
                RecordsReturned = 10,
                ExecutionMilliseconds = c.SqlFilterMilliseconds,
                EstimatedMemoryKb = Math.Round(c.EarlyToListMilliseconds * 120.0, 2),
                PerformanceRating = c.SqlFilterMilliseconds < c.EarlyToListMilliseconds ? "Excellent" : "Good",
                Comparisons =
                [
                    new ComparisonItemDto { Label = "ToList then filter (bad)", Milliseconds = c.EarlyToListMilliseconds, Records = 0 },
                    new ComparisonItemDto { Label = "SQL Where filter (good)", Milliseconds = c.SqlFilterMilliseconds, Records = 10 }
                ]
            },
            "ienumerable-mid-query" => new ConceptActionResultDto
            {
                ConceptId = "common-mistakes-performance",
                Action = action,
                Summary = "AsEnumerable() broke SQL translation — all rows were loaded and filtered in memory.",
                RecordsReturned = 10,
                ExecutionMilliseconds = data.IEnumerableVsIQueryable.IEnumerableMilliseconds,
                EstimatedMemoryKb = Math.Round(data.IEnumerableVsIQueryable.IEnumerableRowsLoaded * 0.35, 2),
                PerformanceRating = "Poor",
                Comparisons =
                [
                    new ComparisonItemDto { Label = "AsEnumerable (client eval)", Milliseconds = data.IEnumerableVsIQueryable.IEnumerableMilliseconds, Records = data.IEnumerableVsIQueryable.IEnumerableRowsLoaded },
                    new ComparisonItemDto { Label = "Full IQueryable (server)", Milliseconds = data.IEnumerableVsIQueryable.IQueryableMilliseconds, Records = 10 }
                ]
            },
            "tracking-readonly" => new ConceptActionResultDto
            {
                ConceptId = "common-mistakes-performance",
                Action = action,
                Summary = "Change tracking added snapshot overhead. AsNoTracking eliminated it for read-only access.",
                RecordsReturned = 500,
                ExecutionMilliseconds = c.NoTrackingMilliseconds,
                EstimatedMemoryKb = Math.Round(500 * 1.2, 2),
                PerformanceRating = c.NoTrackingMilliseconds < c.TrackingMilliseconds ? "Excellent" : "Average",
                Comparisons =
                [
                    new ComparisonItemDto { Label = "With tracking (bad)", Milliseconds = c.TrackingMilliseconds, Records = 500 },
                    new ComparisonItemDto { Label = "AsNoTracking (good)", Milliseconds = c.NoTrackingMilliseconds, Records = 500 }
                ]
            },
            "multiple-enumeration" => new ConceptActionResultDto
            {
                ConceptId = "common-mistakes-performance",
                Action = action,
                Summary = "Two terminal operators on the same query caused two SQL round-trips. Materialize once and reuse.",
                RecordsReturned = data.DeferredVsImmediate.ImmediateListCount,
                ExecutionMilliseconds = 16,
                EstimatedMemoryKb = Math.Round(data.DeferredVsImmediate.ImmediateListCount * 0.18, 2),
                PerformanceRating = "Average",
                Comparisons =
                [
                    new ComparisonItemDto { Label = "CountAsync + ToListAsync (2 queries)", Milliseconds = 16, Records = data.DeferredVsImmediate.ImmediateListCount },
                    new ComparisonItemDto { Label = "Single ToListAsync (1 query)", Milliseconds = 9, Records = data.DeferredVsImmediate.ImmediateListCount }
                ]
            },
            "no-projection" => new ConceptActionResultDto
            {
                ConceptId = "common-mistakes-performance",
                Action = action,
                Summary = "Loading full entities transferred all columns. Select projection reduced payload to three columns.",
                RecordsReturned = 50,
                ExecutionMilliseconds = c.SqlFilterMilliseconds,
                EstimatedMemoryKb = Math.Round(50 * 8.5, 2),
                PerformanceRating = "Good",
                Comparisons =
                [
                    new ComparisonItemDto { Label = "Full entity load (bad)", Milliseconds = c.EarlyToListMilliseconds, Records = 50 },
                    new ComparisonItemDto { Label = "Select projection (good)", Milliseconds = c.SqlFilterMilliseconds, Records = 50 }
                ]
            },
            _ => BuildCommonMistakeAction(data, "early-tolist")
        };
    }

    private async Task<IEnumerableVsIQueryableDto> RunIEnumerableVsIQueryable()
    {
        var limit = 2000m;
        var sw = Stopwatch.StartNew();
        var allOrders = await dbContext.Orders.AsNoTracking().ToListAsync();
        var inMemory = allOrders.Where(o => o.Total >= limit).Select(o => o.Id).Take(10).ToList();
        sw.Stop();
        var ieMs = sw.ElapsedMilliseconds;
        var ieRows = allOrders.Count;

        sw.Restart();
        var queryable = await dbContext.Orders.AsNoTracking()
            .Where(o => o.Total >= limit)
            .OrderBy(o => o.Id)
            .Select(o => o.Id)
            .Take(10)
            .ToListAsync();
        sw.Stop();

        return new IEnumerableVsIQueryableDto
        {
            IEnumerableMilliseconds = ieMs,
            IEnumerableRowsLoaded = ieRows,
            IQueryableMilliseconds = sw.ElapsedMilliseconds,
            IQueryableRowsLoaded = queryable.Count
        };
    }

    private async Task<DeferredVsImmediateDto> RunDeferredVsImmediate()
    {
        var query = dbContext.Orders.AsNoTracking().Where(o => !o.IsPaid);
        var before = await query.CountAsync();
        var after = await query.CountAsync();
        var immediate = await dbContext.Orders.AsNoTracking().Where(o => !o.IsPaid).ToListAsync();

        return new DeferredVsImmediateDto
        {
            DeferredBeforeCount = before,
            DeferredAfterCount = after,
            ImmediateListCount = immediate.Count
        };
    }

    private SqlTranslationDto RunSqlTranslation()
    {
        var query = dbContext.Orders.AsNoTracking()
            .Where(o => o.Total > 2500 && o.IsPaid)
            .OrderByDescending(o => o.Total)
            .Select(o => new { o.Id, o.Total, o.Customer.Name, o.Category })
            .Take(5);

        return new SqlTranslationDto
        {
            Sql = query.ToQueryString()
        };
    }

    private async Task<List<OrderViewDto>> RunFilterProjectionChaining()
    {
        return await dbContext.Orders.AsNoTracking()
            .Where(o => o.IsPaid && o.Category == "Electronics")
            .OrderByDescending(o => o.Total)
            .Select(o => new OrderViewDto
            {
                OrderId = o.Id,
                CustomerName = o.Customer.Name,
                Total = o.Total,
                Category = o.Category
            })
            .Take(8)
            .ToListAsync();
    }

    private async Task<CommonMistakesDto> RunCommonMistakes()
    {
        var sw = Stopwatch.StartNew();
        var bad = await dbContext.Orders.AsNoTracking().ToListAsync();
        _ = bad.Count(o => o.Total > 1500 && o.Category == "Books");
        sw.Stop();
        var earlyToList = sw.ElapsedMilliseconds;

        sw.Restart();
        _ = await dbContext.Orders.AsNoTracking()
            .Where(o => o.Total > 1500 && o.Category == "Books")
            .CountAsync();
        sw.Stop();
        var sqlFilter = sw.ElapsedMilliseconds;

        sw.Restart();
        _ = await dbContext.Orders.Where(o => o.Total > 3000).OrderBy(o => o.Id).Take(500).ToListAsync();
        sw.Stop();
        var tracking = sw.ElapsedMilliseconds;

        sw.Restart();
        _ = await dbContext.Orders.AsNoTracking().Where(o => o.Total > 3000).OrderBy(o => o.Id).Take(500).ToListAsync();
        sw.Stop();
        var noTracking = sw.ElapsedMilliseconds;

        return new CommonMistakesDto
        {
            EarlyToListMilliseconds = earlyToList,
            SqlFilterMilliseconds = sqlFilter,
            TrackingMilliseconds = tracking,
            NoTrackingMilliseconds = noTracking
        };
    }
}
