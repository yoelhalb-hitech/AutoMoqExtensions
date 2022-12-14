using AutoMoqExtensions.FixtureUtils.Requests;
using AutoMoqExtensions.FixtureUtils.Requests.HelperRequests.AutoMock;
using AutoMoqExtensions.FixtureUtils.Requests.HelperRequests.NonAutoMock;
using AutoMoqExtensions.FixtureUtils.Requests.MainRequests;
using System.Reflection;
using static AutoMoqExtensions.MockUtils.CannotSetupMethodException;

namespace AutoMoqExtensions.MockUtils;

internal class MockSetupService
{
    private static readonly DelegateSpecification delegateSpecification = new DelegateSpecification();

    private readonly IAutoMock mock;
    private readonly ISpecimenContext context;
    private readonly Type mockedType;
    private readonly ITracker? tracker;
    private readonly bool noMockDependencies;

    public MockSetupService(IAutoMock mock, ISpecimenContext context)
    {
        this.mock = mock;
        this.context = context;
        // Don't do mock.GetMocked().GetType() as it has additional properties etc.
        this.mockedType = mock.GetInnerType();
        this.tracker = mock.Tracker;
        this.noMockDependencies = mock.Tracker?.StartTracker.MockDependencies ?? false;
    }

    public void Setup()
    {
        var allProperties = mockedType.GetAllProperties().Where(p => p.GetMethod?.IsPublicOrInternal() == true);

        // Properties with both get and prublic/internal set will be handled in the command for it
        // TODO... for virtual methods we can do it here and use a custom invocation func so to delay the generation of the objects
        // Remeber that `private` setters in the base will have no setter in the proxy
        var singleMethodProperties = allProperties.Where(p => !p.HasGetAndSet() || p.SetMethod.IsPrivate);
        foreach (var prop in singleMethodProperties)
        {
            SetupSingleMethodProperty(prop);
        }

        // If it's private it won't be in the proxy, CAUTION: I am doing it here and not in the `GetMethod()` in case we want to change the logic it should be in one place
        var methods = GetMethods().Where(m => !m.IsPrivate);
        foreach (var method in methods)
        {
            SetupMethod(method);
        }
    }

    private void Setup(MemberInfo member, Action action, string? trackingPath = null)
    {
        var method = member as MethodInfo;
        var prop = member as PropertyInfo;

        trackingPath ??= method?.GetTrackingPath() ?? prop!.GetTrackingPath();
        var methods = prop?.GetMethods() ?? new[] { method! };

        if (mock.CallBase && !mock.GetInnerType().IsInterface && !methods.Any(m => m.IsAbstract))
        { // It is callbase and has an implementation so let's ignore it
            HandleCannotSetup(trackingPath, CannotSetupReason.CallBaseNoAbstract);
            return;
        }

        var configureInfo = CanBeConfigured(methods);
        if (!configureInfo.CanConfigure)
        {
            HandleCannotSetup(trackingPath, configureInfo.Reason!.Value);
            return;
        }

        try
        {
            action();                
            mock.MethodsSetup.Add(trackingPath, member);                               
        }
        catch (Exception ex)
        {
            mock.MethodsNotSetup.Add(trackingPath, new CannotSetupMethodException(CannotSetupReason.Exception, ex));
        }
    }

    private void SetupMethod(MethodInfo method)
    {
        Setup(method, () => new MethodSetupServiceWithDifferentResult(mock, mockedType, method, context).Setup());          
    }

    private void SetupSingleMethodProperty(PropertyInfo prop)
    {
        var method = prop.GetMethods().First();

        Setup(method, () => new MethodSetupServiceWithSameResult(mock, mockedType, method, context).Setup(), prop.GetTrackingPath());
    }

    private void SetupAutoProperty(PropertyInfo prop)
    {
        Setup(prop, () =>
        {
            var request = noMockDependencies
                                    ? new PropertyRequest(mockedType, prop, tracker)
                                    : new AutoMockPropertyRequest(mockedType, prop, tracker);
            var propValue = context.Resolve(request);
            SetupHelpers.SetupAutoProperty(mockedType, prop.PropertyType, mock, prop, propValue);
        });
    }

    private IEnumerable<MethodInfo> GetMethods()
    {
        // If "type" is a delegate, return "Invoke" method only and skip the rest of the methods.
        var methods = delegateSpecification.IsSatisfiedBy(mockedType)
            ? new[] { mockedType.GetTypeInfo().GetMethod("Invoke") }
            : mockedType.GetAllMethods();

        var propMethods = mockedType.GetAllProperties().SelectMany(p => p.GetMethods());
        return methods.Except(propMethods).Where(m => m.IsPublicOrInternal());
    }

    private void HandleCannotSetup(string trackingPath, CannotSetupReason reason) 
        => mock.MethodsNotSetup.Add(trackingPath, new CannotSetupMethodException(reason));

    private (bool CanConfigure, CannotSetupReason? Reason) CanBeConfigured(MethodInfo[] methods)
    {
        if (!mockedType.IsInterface && methods.Any(m => !m.IsOverridable())) return (false, CannotSetupReason.NonVirtual);

        if (methods.Any(m => m.IsPrivate)) return (false, CannotSetupReason.Private);           

        if (methods.Any(m => !m.IsPublicOrInternal())) return (false, CannotSetupReason.Protected);            

        return (true, null);
    }
}
