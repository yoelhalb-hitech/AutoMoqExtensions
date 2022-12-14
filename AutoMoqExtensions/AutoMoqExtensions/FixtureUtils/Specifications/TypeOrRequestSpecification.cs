using AutoMoqExtensions.AutoMockUtils;
using AutoMoqExtensions.FixtureUtils.Requests.MainRequests;

namespace AutoMoqExtensions.FixtureUtils.Specifications;

/// <summary>
/// For use to match a type or <see cref="AutoMockRequest"/> for the type or <see cref="AutoMockDirectRequest"/> for the type or <see cref="AutoMockDependenciesRequest"/> for the type
/// </summary>
internal class TypeOrRequestSpecification : IRequestSpecification
{
    public TypeOrRequestSpecification(IRequestSpecification specification)
    {
        Specification = specification;
    }

    public IRequestSpecification Specification { get; }

    public bool IsSatisfiedBy(object request)
    {
        return (request is Type t && Specification.IsSatisfiedBy(t))
                || (request is AutoMockRequest r && Specification.IsSatisfiedBy(r.Request))
                || (request is NonAutoMockRequest n && Specification.IsSatisfiedBy(n.Request))
                || (request is AutoMockDirectRequest dr 
                            && (Specification.IsSatisfiedBy(dr.Request) 
                                      || (AutoMockHelpers.IsAutoMock(dr.Request) 
                                                 && Specification.IsSatisfiedBy(AutoMockHelpers.GetMockedType(dr.Request)))))
                || (request is AutoMockDependenciesRequest der && Specification.IsSatisfiedBy(der.Request));
    }
}
