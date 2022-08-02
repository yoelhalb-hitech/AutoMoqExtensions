﻿using AutoMoqExtensions.AutoMockUtils;
using AutoMoqExtensions.MockUtils;
using Moq;

namespace AutoMoqExtensions.Test.AutoMockFixture_Tests;

internal class Tracking_Tests
{
    [Test]
    public void Test_Create_AddsToTrackerDict_NonAutoMock()
    {
        var fixture = new AutoMockFixture();
        var result = fixture.CreateNonAutoMock<SingletonUserClass>();

        fixture.TrackerDict.Should().HaveCount(1);
        fixture.TrackerDict.First().Key.Should().Be(result);
    }

    [Test]
    public void Test_Create_AddsToTrackerDict_NonAutoMock_WithDependencies()
    {
        var fixture = new AutoMockFixture();
        var result = fixture.CreateWithAutoMockDependencies<SingletonUserClass>();

        fixture.TrackerDict.Should().HaveCount(1);
        fixture.TrackerDict.First().Key.Should().Be(result);
    }

    [Test]
    public void Test_Create_AddsUnderlyingMockToTrackerDict()
    {
        var fixture = new AutoMockFixture();
        var result = fixture.CreateAutoMock<SingletonUserClass>();

        fixture.TrackerDict.Should().HaveCount(1);
        fixture.TrackerDict.First().Key.Should().Be(Mock.Get(result));
    }

    [Test]
    public void Test_ListsSetupMethods()
    {
        var fixture = new AutoMockFixture();
        var result = fixture.CreateAutoMock<WithCtorArgsTestClass>();
        var mock = AutoMockHelpers.GetAutoMock(result);

        mock!.MethodsSetup.Should().ContainKey("TestClassPropGet");
        mock!.MethodsNotSetup.Should().ContainKey("TestClassProp");
        mock!.MethodsNotSetup["TestClassProp"].Reason.Should().Be(CannotSetupMethodException.CannotSetupReason.NonVirtual);
    }

}
