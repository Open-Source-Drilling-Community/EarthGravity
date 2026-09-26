using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using System.ComponentModel.DataAnnotations;

namespace OSDC.Drilling.EarthGravity.Model;

/// <summary>A stateless synchronous EGM96 evaluation request.</summary>
[Semantic(Concepts.Request)]
public class EarthGravityEvaluationRequest
{
    /// <summary>Positions to evaluate. The entire request is rejected when any item is invalid.</summary>
    [Required, MinLength(1)]
    [Semantic(Concepts.Position, Role = Concepts.InputPositions, Reference = Concepts.Wgs84)]
    public List<EarthGravityPosition> Positions { get; set; } = [];
}
