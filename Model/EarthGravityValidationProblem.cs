namespace OSDC.Drilling.EarthGravity.Model;

/// <summary>Details returned when an evaluation request is rejected atomically.</summary>
public class EarthGravityValidationProblem
{
    public string Error { get; set; } = "invalid_request";
    public string Message { get; set; } = string.Empty;
    public List<EarthGravityValidationError> Errors { get; set; } = [];
}
