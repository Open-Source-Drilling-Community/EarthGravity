namespace OSDC.Drilling.EarthGravity.Model;

public sealed class EarthGravityValidationException(IReadOnlyList<EarthGravityValidationError> errors)
    : Exception("The Earth gravity evaluation request is invalid.")
{
    public IReadOnlyList<EarthGravityValidationError> Errors { get; } = errors;
}
