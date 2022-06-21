﻿using AutoFixture;
using AutoFixture.Kernel;
using AutoMoqExtensions.FixtureUtils.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoMoqExtensions.FixtureUtils.Postprocessors
{
    internal class CachePostprocessor : ISpecimenBuilder
    {
        public CachePostprocessor(Cache cache)
        {
            Cache = cache;
        }

        public Cache Cache { get; }

        public object? Create(object request, ISpecimenContext context)
        {
            if(Cache.CacheDictionary.ContainsKey(request)) return Cache.CacheDictionary[request];

            return new NoSpecimen();
        }
    }
}