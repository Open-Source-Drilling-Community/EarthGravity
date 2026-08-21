using System.ComponentModel.DataAnnotations;

namespace OSDC.Drilling.EarthGravity.Model;

/// <summary>A stateless synchronous EGM96 evaluation request.</summary>
public class EarthGravityEvaluationRequest
{
    /// <summary>Positions to evaluate. The entire request is rejected when any item is invalid.</summary>
    [Required, MinLength(1)]
    public List<EarthGravityPosition> Positions { get; set; } = [];
}
