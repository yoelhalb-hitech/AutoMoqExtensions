﻿using AutoFixture.Kernel;
using AutoMoqExtensions.AutoMockUtils;
using AutoMoqExtensions.FixtureUtils.Requests;
using AutoMoqExtensions.FixtureUtils.Specifications;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AutoMoqExtensions.FixtureUtils.Postprocessors
{
    internal class AutoMockDependenciesPostprocessor : ISpecimenBuilder
    {
        public AutoMockDependenciesPostprocessor(ISpecimenBuilder builder)
        {
            Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public ISpecimenBuilder Builder { get; }

        public object? Create(object request, ISpecimenContext context)
        {
            if (request is not AutoMockDependenciesRequest dependencyRequest)
                return new NoSpecimen();

            if (dependencyRequest.Request.IsAbstract || dependencyRequest.Request.IsInterface)           
                return TryAutoMock(dependencyRequest, context);            

            if(AutoMockHelpers.IsAutoMock(dependencyRequest.Request))
            {
                var inner = AutoMockHelpers.GetMockedType(dependencyRequest.Request)!;
                var automockRequest = new AutoMockRequest(inner, dependencyRequest) { MockShouldCallbase = true };

                return context.Resolve(automockRequest);
            }

            try
            {
                var specimen = Builder.Create(request, context);
                if (specimen is NoSpecimen || specimen is OmitSpecimen) return TryAutoMock(dependencyRequest, context);
                
                if (specimen is null)
                {
                    dependencyRequest.SetResult(specimen);
                    return specimen;
                }

                if (specimen.GetType() != dependencyRequest.Request)
                {
                    var result = new NoSpecimen();
                    dependencyRequest.SetResult(result);
                    return result;
                }

                dependencyRequest.SetResult(specimen);
                return specimen;
            }
            catch
            {
                return TryAutoMock(dependencyRequest, context);
            }
        }

        private object? TryAutoMock(AutoMockDependenciesRequest dependencyRequest, ISpecimenContext context)
        {
            //If it's not the start request then it arrives here only if it isn't a valid AutoMock
            if (!Object.ReferenceEquals(dependencyRequest, dependencyRequest.StartTracker)  
                            || !AutoMockHelpers.IsAutoMockAllowed(dependencyRequest.Request))
                return new NoSpecimen();

            // Can't leave it for the relay as we want the dependencies mocked correctly
            try
            {
                // We want MockShouldCallbase so to get dependencies
                // It should automatically revert to the MockShouldCallbase on the StartTracker for the next objects
                // TODO... add tests                
                var autoMockRequest = new AutoMockRequest(dependencyRequest.Request, dependencyRequest) { MockShouldCallbase = true };
                var autoMockResult = context.Resolve(autoMockRequest);
                dependencyRequest.SetResult(autoMockResult);
                return autoMockResult;
            }
            catch
            {
                return new NoSpecimen();
            }
        }
    }
}
