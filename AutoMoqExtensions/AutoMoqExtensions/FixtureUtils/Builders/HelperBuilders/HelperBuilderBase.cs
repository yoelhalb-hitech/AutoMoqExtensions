
using AutoMoqExtensions.FixtureUtils.Requests;
using AutoMoqExtensions.FixtureUtils.Requests.MainRequests;
using AutoMoqExtensions.FixtureUtils.Specifications;

namespace AutoMoqExtensions.FixtureUtils.Builders.HelperBuilders;

internal abstract class HelperBuilderBase<TRequest> : ISpecimenBuilder where TRequest : ITracker
{
    private static readonly AutoMockableSpecification autoMockableSpecification = new();

    public virtual object? Create(object request, ISpecimenContext context)
    {
        if (request is not TRequest trackedRequest) return new NoSpecimen();
        
        return HandleInternal(trackedRequest, context);
    }

    protected virtual object? HandleInternal(TRequest trackedRequest, ISpecimenContext context)
    {
        var type = GetRequest(trackedRequest);
        if (!autoMockableSpecification.IsSatisfiedBy(type) || !trackedRequest.StartTracker.MockDependencies)
        {
            return HandleNonAutoMock(trackedRequest, context, type);
        }

        var specimen = context.Resolve(new AutoMockRequest(type, trackedRequest));

        if (specimen is NoSpecimen || specimen is OmitSpecimen || specimen is null)
        {
            trackedRequest.SetResult(specimen);
            return specimen;
        }

        trackedRequest.SetCompleted();

        return specimen;
    }

    protected virtual object? HandleNonAutoMock(TRequest trackedRequest, ISpecimenContext context, Type type)
    {
        object newRequest;

        if (type.IsPrimitive || type == typeof(string)) // AutoFixture has special hadnling based on the property name etc.
        {
            newRequest = GetFullRequest(trackedRequest);
        }
        else
        {
            newRequest = trackedRequest.StartTracker.MockDependencies
                    ? new AutoMockDependenciesRequest(type, trackedRequest)
                    : new NonAutoMockRequest(type, trackedRequest);
        }

        var result = context.Resolve(newRequest);
        trackedRequest.SetResult(result);

        return result;
    }

    protected abstract Type GetRequest(TRequest request);

    protected abstract object GetFullRequest(TRequest request);
}
