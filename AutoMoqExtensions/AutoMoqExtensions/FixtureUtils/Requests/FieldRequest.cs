﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AutoMoqExtensions.FixtureUtils.Requests
{
    internal class FieldRequest : BaseTracker, IEquatable<FieldRequest>
    {
        public FieldRequest(Type declaringType, FieldInfo fieldInfo, ITracker? tracker) : base(tracker)
        {
            DeclaringType = declaringType;
            FieldInfo = fieldInfo;
        }

        public virtual Type DeclaringType { get; }
        public virtual FieldInfo FieldInfo { get; }

        public override string InstancePath => "." + FieldInfo.Name;

        public override bool Equals(object obj) 
            => obj is FieldRequest other ? this.Equals(other) : base.Equals(obj);

        public override int GetHashCode() => HashCode.Combine(DeclaringType, FieldInfo);

        public virtual bool Equals(FieldRequest other)
            => other is not null && this.DeclaringType == other.DeclaringType && this.FieldInfo == other.FieldInfo;       
    }
}