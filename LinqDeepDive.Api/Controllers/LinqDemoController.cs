using LinqDeepDive.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LinqDeepDive.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class LinqDemoController(LinqDemoService service) : ControllerBase
{
    [HttpGet("concepts")]
    public async Task<IActionResult> Concepts()
    {
        var concepts = await service.GetConceptsAsync();
        return Ok(concepts);
    }

    [HttpGet("run")]
    public async Task<IActionResult> Run()
    {
        var result = await service.RunAsync();
        return Ok(result);
    }

    [HttpGet("action")]
    public async Task<IActionResult> RunAction([FromQuery] string conceptId, [FromQuery] string name)
    {
        var result = await service.RunConceptActionAsync(conceptId, name);
        return Ok(result);
    }
}
