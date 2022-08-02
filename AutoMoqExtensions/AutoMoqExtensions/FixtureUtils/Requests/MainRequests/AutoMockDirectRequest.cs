﻿
namespace AutoMoqExtensions.FixtureUtils.Requests.MainRequests;

internal class AutoMockDirectRequest : TrackerWithFixture, IRequestWithType, IFixtureTracker, IDisposable
{
    public AutoMockDirectRequest(Type request, ITracker tracker) : base(tracker.StartTracker.Fixture, tracker)
    {
        Request = request;
        if (tracker is null) throw new Exception("Either tracker or fixture must be provided");
    }

    public AutoMockDirectRequest(Type request, AutoMockFixture fixture) : base(fixture, null)
    {
        Request = request;
    }

    public virtual Type Request { get; }
    public virtual bool? NoMockDependencies { get; set; }

    public override string InstancePath => "";


    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Request, NoMockDependencies);

    public override bool IsRequestEquals(ITracker other)
        => other is AutoMockDirectRequest request
            && request.Request == Request && request.NoMockDependencies == NoMockDependencies
            && base.IsRequestEquals(other);

    public void Dispose() => SetCompleted();
}