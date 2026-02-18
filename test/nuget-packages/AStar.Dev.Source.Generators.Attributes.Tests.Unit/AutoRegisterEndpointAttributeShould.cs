namespace AStar.Dev.Source.Generators.Attributes.Tests.Unit;

public class AutoRegisterEndpointAttributeShould
{
    [Fact]
    public void SetMethodTypeViaConstructor()
        => new AutoRegisterEndpointAttribute(HttpMethod.Post).MethodType.ShouldBe(HttpMethod.Post);

    [Fact]
    public void DefaultMethodTypeToGetWhenNull()
        => new AutoRegisterEndpointAttribute(null).MethodType.ShouldBe(HttpMethod.Get);

    [Fact]
    public void SetMethodGroupNameViaConstructor()
        => new AutoRegisterEndpointAttribute(HttpMethod.Get, "MyGroup").MethodGroupName.ShouldBe("MyGroup");

    [Fact]
    public void AllowMethodGroupNameToBeNull()
        => new AutoRegisterEndpointAttribute(HttpMethod.Get, null).MethodGroupName.ShouldBeNull();

    [Fact]
    public void BeApplicableToClassesOnly()
        => typeof(AutoRegisterEndpointAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().ValidOn.ShouldBe(AttributeTargets.Class);

    [Fact]
    public void NotBeInherited()
        => typeof(AutoRegisterEndpointAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().Inherited.ShouldBeFalse();

    [Fact]
    public void NotAllowMultipleUsageOnSameType()
        => typeof(AutoRegisterEndpointAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().AllowMultiple.ShouldBeFalse();
}
