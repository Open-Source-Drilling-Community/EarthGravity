using GeographicLib;
using NUnit.Framework;
using OSDC.Drilling.EarthGravity.Model;

namespace OSDC.Drilling.EarthGravity.ModelTest;

public class Tests
{
    private EarthGravityEvaluator evaluator_ = null!;

    [SetUp]
    public void Setup() => evaluator_ = new EarthGravityEvaluator();

    [Test]
    public void EvaluateConvertsRadiansAndPositiveDownDepthForGeographicLib()
    {
        const double latitude = 0.7;
        const double longitude = -1.2;
        const double depth = 1250;
        EarthGravityEvaluationResponse response = evaluator_.Evaluate(Request(latitude, longitude, depth));
        var direct = new GravityModel("egm96", Path.Combine(AppContext.BaseDirectory, "GravityModelFiles"))
            .Gravity(latitude * 180 / Math.PI, longitude * 180 / Math.PI, -depth);

        Assert.Multiple(() =>
        {
            Assert.That(response.Samples[0].Gravity.East, Is.EqualTo(direct.gx).Within(1e-12));
            Assert.That(response.Samples[0].Gravity.North, Is.EqualTo(direct.gy).Within(1e-12));
            Assert.That(response.Samples[0].Gravity.Up, Is.EqualTo(direct.gz).Within(1e-12));
            Assert.That(response.Samples[0].Gravity.TotalPotential, Is.EqualTo(direct.W).Within(1e-6));
            Assert.That(response.Samples[0].Position.Depth, Is.EqualTo(depth));
        });
    }

    [Test]
    public void ModelInformationIsTraceable()
    {
        Assert.Multiple(() =>
        {
            Assert.That(evaluator_.ModelInfo.ID, Is.EqualTo("EGM1996A"));
            Assert.That(evaluator_.ModelInfo.Degree, Is.EqualTo(360));
            Assert.That(evaluator_.ModelInfo.Order, Is.EqualTo(360));
            Assert.That(evaluator_.ModelInfo.ReferenceEllipsoid, Is.EqualTo("WGS84"));
            Assert.That(evaluator_.ModelInfo.IncludesCentrifugalAcceleration, Is.True);
            Assert.That(evaluator_.ModelInfo.CoefficientSHA256, Has.Length.EqualTo(64));
        });
    }

    [TestCase(Math.PI, 0, 0, "Latitude")]
    [TestCase(0, 4, 0, "Longitude")]
    [TestCase(0, 0, double.NaN, "Depth")]
    public void InvalidPositionRejectsCompleteRequest(double latitude, double longitude, double depth, string property)
    {
        EarthGravityValidationException exception = Assert.Throws<EarthGravityValidationException>(
            () => evaluator_.Evaluate(Request(latitude, longitude, depth)))!;
        Assert.That(exception.Errors, Has.Some.Property("Property").EqualTo(property));
    }

    [Test]
    public void MaximumBatchSizeIsEnforced()
    {
        var request = new EarthGravityEvaluationRequest { Positions = [new(), new()] };
        EarthGravityValidationException exception = Assert.Throws<EarthGravityValidationException>(() => evaluator_.Evaluate(request, 1))!;
        Assert.That(exception.Errors, Has.Some.Property("Code").EqualTo("too_many"));
    }

    private static EarthGravityEvaluationRequest Request(double latitude, double longitude, double depth) => new()
    {
        Positions = [new EarthGravityPosition { Latitude = latitude, Longitude = longitude, Depth = depth }]
    };
}
