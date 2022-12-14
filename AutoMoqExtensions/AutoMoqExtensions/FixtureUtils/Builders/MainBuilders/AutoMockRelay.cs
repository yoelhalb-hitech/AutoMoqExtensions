using AutoMoqExtensions.AutoMockUtils;
using AutoMoqExtensions.FixtureUtils.Requests;
using AutoMoqExtensions.FixtureUtils.Requests.MainRequests;
using AutoMoqExtensions.FixtureUtils.Specifications;

namespace AutoMoqExtensions.FixtureUtils.Builders.MainBuilders;

internal class AutoMockRelay : ISpecimenBuilder
{
    public AutoMockRelay(AutoMockFixture fixture)
             : this(new AutoMockableSpecification(), fixture)
    {
    }

    public AutoMockRelay(IRequestSpecification mockableSpecification, AutoMockFixture fixture)
    {
        this.MockableSpecification = mockableSpecification ?? throw new ArgumentNullException(nameof(mockableSpecification));
        Fixture = fixture;
    }

    public IRequestSpecification MockableSpecification { get; }
    public AutoMockFixture Fixture { get; }

    public object? Create(object request, ISpecimenContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        var t = request as Type ?? (request as SeededRequest)?.Request as Type;
        if (t is null)
            return new NoSpecimen();

        Logger.LogInfo($"In relay, for {t.FullName}");

        // We do direct to bypass the specification test
        var autoMockType = AutoMockHelpers.GetAutoMockType(t); // We make it for an AutoMock type so it will be automocked
        var directRequest = new NonAutoMockRequest(t, Fixture) // Use NonAutoMockRequest so not to mock dependencies
        {
            MockShouldCallbase = true,
        };
        
        var result = context.Resolve(directRequest);

        // Note: null is a valid specimen (e.g., returned by NullRecursionHandler)
        if (result is NoSpecimen || result is OmitSpecimen || result is null)
            return result;

        if (!t.IsAssignableFrom(result.GetType())) return new NoSpecimen();

        HandleResult(result, directRequest, context);
        return result;
    }

    private void HandleResult(object result, ITracker tracker, ISpecimenContext context)
    {            
        tracker.SetResult(result);

        Fixture.TrackerDict[result] = tracker;
    }
}
