using AutoMoqExtensions.AutoMockUtils;
using AutoMoqExtensions.FixtureUtils.Requests;
using AutoMoqExtensions.MockUtils;
using AutoMoqExtensions.VerifyInfo;
using Castle.DynamicProxy;
using Moq;
using System.Reflection;

namespace AutoMoqExtensions;

internal interface ISetCallBase
{
    void ForceSetCallbase(bool value);
}
public partial class AutoMock<T> : Mock<T>, IAutoMock, ISetCallBase where T : class
{
    public override T Object => GetMocked();
    public virtual ITracker? Tracker { get; set; }
    public virtual AutoMockFixture Fixture => Tracker?.StartTracker.Fixture
            ?? throw new Exception($"Fixture not set, was this created by `{nameof(AutoMockFixture)}`?");
    public List<IVerifyInfo<T>> VerifyList { get; } = new List<IVerifyInfo<T>>();
    public Dictionary<string, MemberInfo> MethodsSetup { get; } = new Dictionary<string, MemberInfo>();
    public Dictionary<string, CannotSetupMethodException> MethodsNotSetup { get; }
                                        = new Dictionary<string, CannotSetupMethodException>();

    private static object castleProxyFactoryInstance;
    private static FieldInfo generatorFieldInfo;
    private static ProxyGenerator originalProxyGenerator;
    static AutoMock()
    {
        var moqAssembly = typeof(Mock).Assembly;

        var proxyFactoryType = moqAssembly.GetType("Moq.ProxyFactory");
        castleProxyFactoryInstance = proxyFactoryType.GetProperty("Instance").GetValue(null);

        var castleProxyFactoryType = moqAssembly.GetType("Moq.CastleProxyFactory");
        generatorFieldInfo = castleProxyFactoryType.GetField("generator", BindingFlags.NonPublic | BindingFlags.Instance);
        originalProxyGenerator = (ProxyGenerator)generatorFieldInfo.GetValue(castleProxyFactoryInstance);

        ResetGenerator();
    }
    public override bool CallBase { get => base.CallBase; set
          {
            if (mocked is not null) throw new Exception("Cannot set callbase after object has been created");
            base.CallBase = value;
        } }

    public object? Target => target;

    void ISetCallBase.ForceSetCallbase(bool value) => base.CallBase = value;
   
    private void SetupGenerator()
        => generatorFieldInfo.SetValue(castleProxyFactoryInstance, new AutoMockProxyGenerator(target, this.CallBase));
    private static void ResetGenerator()
        => generatorFieldInfo.SetValue(castleProxyFactoryInstance, new AutoMockProxyGenerator());
    private T? target;
    public bool TrySetTarget(T target)
    {
        if (mocked is not null || this.target is not null) return false;

        SetTarget(target);
        return true;
    }
    public void SetTarget(T target)
    {
        if (mocked is not null) throw new Exception("Cannot set target when object is already created");
        if (this.target is not null) throw new Exception("Can only set target once");

        this.target = target;
    }

    private T? mocked;
    public Type GetInnerType() => typeof(T);
    private static PropertyInfo additionalInterfaceProp = typeof(Mock)
                            .GetProperty("AdditionalInterfaces", BindingFlags.Instance | BindingFlags.NonPublic);
    private static Type iautoMockedType = typeof(IAutoMocked);
    public void EnsureMocked()
    {
        if (mocked is null)
        {
            var additionalInterfaces = (List<Type>)additionalInterfaceProp.GetValue(this);
            // The generator is static so we have to reduce it to the minimum
            SetupGenerator();
            additionalInterfaces.Add(iautoMockedType);
            mocked = base.Object;
            this.target = null;
            additionalInterfaces.Remove(iautoMockedType);
            ResetGenerator(); // We need to reset it in case the user wants to use the Mock directly as this property is static...
        } 
    }
    object IAutoMock.GetMocked() => GetMocked();
    public T GetMocked()
    {
        EnsureMocked();

        return mocked!;
    }

    public static implicit operator T(AutoMock<T> m) => m.GetMocked();

    public AutoMock(MockBehavior behavior) : base(behavior) { setupUtils = new SetupUtils<T>(this); }
    public AutoMock(params object?[] args) : base(args) { setupUtils = new SetupUtils<T>(this); }
    public AutoMock(MockBehavior behavior, params object?[] args) : base(behavior, args)
    {
        setupUtils = new SetupUtils<T>(this);            
    }
    public AutoMock() : base() { setupUtils = new SetupUtils<T>(this); }

    void IAutoMock.Verify() => this.Verify();
    public new void Verify()
    {
        VerifyList.ForEach(v => v.Verify(this));// TODO... maybe we should catch everything and rethrow as aggregate exception
        base.Verify();
    }

    [Obsolete("Use Verify, as AutoFixture sets up everything")]
    public new void VerifyAll()
    {
        VerifyList.ForEach(v => v.Verify(this));// TODO... maybe we should catch everything and rethrow as aggregate exception
        base.VerifyAll();
    }

    private readonly SetupUtils<T> setupUtils;
}
