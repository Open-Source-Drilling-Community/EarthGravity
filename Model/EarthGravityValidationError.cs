namespace OSDC.Drilling.EarthGravity.Model;

public record EarthGravityValidationError(int? PositionIndex, string Property, string Code, string Message);
