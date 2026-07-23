namespace LinqDeepDive.Application.Dtos;

public sealed class LinqDemoResultDto
{
    public List<LinqConceptDto> Concepts { get; set; } = [];
    public IEnumerableVsIQueryableDto IEnumerableVsIQueryable { get; set; } = new();
    public DeferredVsImmediateDto DeferredVsImmediate { get; set; } = new();
    public SqlTranslationDto SqlTranslation { get; set; } = new();
    public List<OrderViewDto> FilterProjectionChaining { get; set; } = [];
    public FilterProjectionDemoDto FilterProjectionDemo { get; set; } = new();
    public CommonMistakesDto CommonMistakes { get; set; } = new();
}

public sealed class IEnumerableVsIQueryableDto
{
    public long IEnumerableMilliseconds { get; set; }
    public int IEnumerableRowsLoaded { get; set; }
    public long IQueryableMilliseconds { get; set; }
    public int IQueryableRowsLoaded { get; set; }
}

public sealed class DeferredVsImmediateDto
{
    public int DeferredBeforeCount { get; set; }
    public int DeferredAfterCount { get; set; }
    public int ImmediateListCount { get; set; }
}

public sealed class SqlTranslationDto
{
    public string Sql { get; set; } = string.Empty;
}

public sealed class OrderViewDto
{
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Category { get; set; } = string.Empty;
}

public sealed class CommonMistakesDto
{
    public long EarlyToListMilliseconds { get; set; }
    public long SqlFilterMilliseconds { get; set; }
    public long TrackingMilliseconds { get; set; }
    public long NoTrackingMilliseconds { get; set; }
}

public sealed class FilterProjectionDemoDto
{
    public long FilteringBadMilliseconds { get; set; }
    public long FilteringGoodMilliseconds { get; set; }
    public int FilteringBadRecords { get; set; }
    public int FilteringGoodRecords { get; set; }
    public long ProjectionBadMilliseconds { get; set; }
    public long ProjectionGoodMilliseconds { get; set; }
    public int ProjectionRecords { get; set; }
    public long ChainingBadMilliseconds { get; set; }
    public long ChainingGoodMilliseconds { get; set; }
    public int ChainingRecords { get; set; }
}

public sealed class TermDefinitionDto
{
    public string Term { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
    public string ExampleCode { get; set; } = string.Empty;
    public string BadExampleCode { get; set; } = string.Empty;
    public string GoodExampleCode { get; set; } = string.Empty;
    public string ActionId { get; set; } = string.Empty;
}

public sealed class CodeExampleDto
{
    public string Title { get; set; } = string.Empty;
    public string CSharpCode { get; set; } = string.Empty;
}

public sealed class PresentationSlideDto
{
    public string Title { get; set; } = string.Empty;
    public string Intro { get; set; } = string.Empty;
    public string ExampleCode { get; set; } = string.Empty;
    public List<string> InternalSteps { get; set; } = [];
    public string SqlOutput { get; set; } = string.Empty;
    public string BadExampleCode { get; set; } = string.Empty;
    public string FailureExplanation { get; set; } = string.Empty;
}

public sealed class MistakeExampleDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CSharpCode { get; set; } = string.Empty;
}

public sealed class LinqConceptDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
    public string Overview { get; set; } = string.Empty;
    public List<TermDefinitionDto> TermDefinitions { get; set; } = [];
    public string DemoCode { get; set; } = string.Empty;
    public List<CodeExampleDto> CodeExamples { get; set; } = [];
    public List<PresentationSlideDto> PresentationSlides { get; set; } = [];
    public List<MistakeExampleDto> MistakeExamples { get; set; } = [];
}

public sealed class ConceptActionResultDto
{
    public string ConceptId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int RecordsReturned { get; set; }
    public long ExecutionMilliseconds { get; set; }
    public double EstimatedMemoryKb { get; set; }
    public string PerformanceRating { get; set; } = string.Empty;
    public List<ComparisonItemDto> Comparisons { get; set; } = [];
}

public sealed class ComparisonItemDto
{
    public string Label { get; set; } = string.Empty;
    public long Milliseconds { get; set; }
    public int Records { get; set; }
}
