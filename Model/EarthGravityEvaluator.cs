using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using GeographicLib;

namespace OSDC.Drilling.EarthGravity.Model;

/// <summary>Loads EGM96 once and evaluates stateless requests using the OSDC public SI convention.</summary>
public sealed class EarthGravityEvaluator
{
    private readonly GravityModel gravityModel_;
    private readonly object evaluationLock_ = new();

    public EarthGravityEvaluator(string? modelDirectory = null)
    {
        string directory = ResolveModelDirectory(modelDirectory);
        string metadataPath = Path.Combine(directory, "egm96.egm");
        string coefficientPath = metadataPath + ".cof";
        if (!File.Exists(metadataPath) || !File.Exists(coefficientPath))
            throw new FileNotFoundException($"Required EGM96 files were not found in '{directory}'.");

        gravityModel_ = new GravityModel("egm96", directory);
        IReadOnlyDictionary<string, string> metadata = ReadMetadata(metadataPath);
        ModelInfo = new EarthGravityModelInfo
        {
            Name = metadata.GetValueOrDefault("Name", "egm96"),
            ID = metadata.GetValueOrDefault("ID", "EGM1996A"),
            Publisher = metadata.GetValueOrDefault("Publisher", "Unknown"),
            ReleaseDate = metadata.GetValueOrDefault("ReleaseDate", "Unknown"),
            DataVersion = metadata.GetValueOrDefault("DataVersion", "Unknown"),
            Degree = gravityModel_.Degree,
            Order = gravityModel_.Order,
            GeographicLibVersion = typeof(GravityModel).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? typeof(GravityModel).Assembly.GetName().Version?.ToString() ?? "Unknown",
            CoefficientSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(coefficientPath))).ToLowerInvariant()
        };
    }

    public EarthGravityModelInfo ModelInfo { get; }

    public EarthGravityEvaluationResponse Evaluate(EarthGravityEvaluationRequest? request, int maximumPositions = 10_000,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<EarthGravityValidationError> errors = Validate(request, maximumPositions);
        if (errors.Count != 0) throw new EarthGravityValidationException(errors);

        var result = new EarthGravityEvaluationResponse { Model = ModelInfo };
        lock (evaluationLock_)
        {
            foreach (EarthGravityPosition position in request!.Positions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                double latitudeDegrees = position.Latitude * 180.0 / Math.PI;
                double longitudeDegrees = position.Longitude * 180.0 / Math.PI;
                double ellipsoidalHeight = -position.Depth;
                (double potential, double east, double north, double up) =
                    gravityModel_.Gravity(latitudeDegrees, longitudeDegrees, ellipsoidalHeight);
                result.Samples.Add(new EarthGravitySample
                {
                    Position = new EarthGravityPosition
                    {
                        Latitude = position.Latitude,
                        Longitude = position.Longitude,
                        Depth = position.Depth
                    },
                    Gravity = new EarthGravityVector
                    {
                        East = east,
                        North = north,
                        Up = up,
                        TotalPotential = potential
                    }
                });
            }
        }
        return result;
    }

    private static IReadOnlyList<EarthGravityValidationError> Validate(EarthGravityEvaluationRequest? request, int maximumPositions)
    {
        var errors = new List<EarthGravityValidationError>();
        if (request?.Positions == null)
        {
            errors.Add(new(null, "Positions", "required", "Positions is required."));
            return errors;
        }
        if (request.Positions.Count == 0)
            errors.Add(new(null, "Positions", "empty", "At least one position is required."));
        if (request.Positions.Count > maximumPositions)
            errors.Add(new(null, "Positions", "too_many", $"At most {maximumPositions.ToString(CultureInfo.InvariantCulture)} positions are allowed."));

        for (int index = 0; index < request.Positions.Count; index++)
        {
            EarthGravityPosition? position = request.Positions[index];
            if (position == null)
            {
                errors.Add(new(index, "Position", "required", "Position must not be null."));
                continue;
            }
            ValidateAngle(errors, index, "Latitude", position.Latitude, -Math.PI / 2, Math.PI / 2);
            ValidateAngle(errors, index, "Longitude", position.Longitude, -Math.PI, Math.PI);
            if (!double.IsFinite(position.Depth))
                errors.Add(new(index, "Depth", "not_finite", "Depth must be a finite value in SI metres."));
        }
        return errors;
    }

    private static void ValidateAngle(List<EarthGravityValidationError> errors, int index, string property,
        double value, double minimum, double maximum)
    {
        if (!double.IsFinite(value))
            errors.Add(new(index, property, "not_finite", $"{property} must be finite."));
        else if (value < minimum || value > maximum)
            errors.Add(new(index, property, "out_of_range", $"{property} must be between {minimum} and {maximum} SI radians."));
    }

    private static string ResolveModelDirectory(string? configured) => !string.IsNullOrWhiteSpace(configured)
        ? Path.GetFullPath(configured)
        : Path.Combine(AppContext.BaseDirectory, "GravityModelFiles");

    private static IReadOnlyDictionary<string, string> ReadMetadata(string path)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string line in File.ReadLines(path))
        {
            string value = line.Trim();
            if (value.Length == 0 || value.StartsWith('#') || value == "EGMF-1") continue;
            int separator = value.IndexOfAny([' ', '\t']);
            if (separator > 0) values[value[..separator]] = value[separator..].Trim();
        }
        return values;
    }
}
