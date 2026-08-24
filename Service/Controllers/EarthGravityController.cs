using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OSDC.Drilling.EarthGravity.Model;

namespace OSDC.Drilling.EarthGravity.Service.Controllers;

[Produces("application/json")]
[Route("[controller]")]
[ApiController]
public class EarthGravityController(
    EarthGravityEvaluator evaluator,
    UsageStatisticsEarthGravity statistics,
    IOptions<EarthGravityServiceOptions> options) : ControllerBase
{
    /// <summary>Returns the loaded EGM96 model information for service discovery.</summary>
    /// <remarks>This is the microservice entry endpoint. It returns the same model identity and provenance as the ModelInfo endpoint.</remarks>
    [HttpGet(Name = "GetEarthGravityEntry")]
    public ActionResult<EarthGravityModelInfo> GetEarthGravityEntry() => GetModelInfoResponse();

    /// <summary>Synchronously evaluates EGM96 total gravity for WGS84 positions expressed in OSDC SI units.</summary>
    /// <remarks>This operation is stateless. Latitude and longitude are radians. Depth is metres, positive downward from the WGS84 reference ellipsoid. The complete request is rejected if any position is invalid.</remarks>
    [HttpPost("Evaluate", Name = "EvaluateEarthGravity")]
    [ProducesResponseType(typeof(EarthGravityEvaluationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EarthGravityValidationProblem), StatusCodes.Status422UnprocessableEntity)]
    public ActionResult<EarthGravityEvaluationResponse> EvaluateEarthGravity(
        [FromBody] EarthGravityEvaluationRequest request, CancellationToken cancellationToken)
    {
        statistics.IncrementEvaluation(false, request?.Positions?.Count ?? 0);
        try
        {
            return Ok(evaluator.Evaluate(request, options.Value.MaximumPositionsPerRequest, cancellationToken));
        }
        catch (EarthGravityValidationException exception)
        {
            statistics.IncrementFailedEvaluation();
            return UnprocessableEntity(new EarthGravityValidationProblem
            {
                Message = exception.Message,
                Errors = exception.Errors.ToList()
            });
        }
    }

    /// <summary>Returns the loaded EGM96 model identity, provenance, degree, order, runtime version, and coefficient hash.</summary>
    [HttpGet("ModelInfo", Name = "GetEarthGravityModelInfo")]
    public ActionResult<EarthGravityModelInfo> GetEarthGravityModelInfo() => GetModelInfoResponse();

    private ActionResult<EarthGravityModelInfo> GetModelInfoResponse()
    {
        statistics.IncrementModelInfo();
        return Ok(evaluator.ModelInfo);
    }
}
