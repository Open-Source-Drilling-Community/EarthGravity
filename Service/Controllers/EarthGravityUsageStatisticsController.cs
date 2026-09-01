using Microsoft.AspNetCore.Mvc;
using OSDC.Drilling.EarthGravity.Model;

namespace OSDC.Drilling.EarthGravity.Service.Controllers;

[Produces("application/json")]
[Route("[controller]")]
[ApiController]
public class EarthGravityUsageStatisticsController(UsageStatisticsEarthGravity statistics) : ControllerBase
{
    /// <summary>Returns cumulative usage counters persisted by this service. This operation is intentionally not exposed as an MCP tool.</summary>
    [HttpGet(Name = "GetEarthGravityUsageStatistics")]
    public ActionResult<UsageStatisticsEarthGravity> GetEarthGravityUsageStatistics()
    {
        statistics.IncrementStatistics();
        return Ok(statistics);
    }
}
