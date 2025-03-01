﻿// Copyright (c) Christof Senn. All rights reserved. See license.txt in the project root for license information.

namespace Remote.Linq.DynamicQuery
{
    using Aqua.TypeSystem;
    using System;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    /// <summary>
    /// This type is used to distinguish variable query arguments from constant query arguments.
    /// </summary>
    [Serializable]
    [DataContract]
    [KnownType(typeof(DateTimeOffset)), XmlInclude(typeof(DateTimeOffset))]
    [KnownType(typeof(System.Numerics.BigInteger)), XmlInclude(typeof(System.Numerics.BigInteger))]
    [KnownType(typeof(System.Numerics.Complex)), XmlInclude(typeof(System.Numerics.Complex))]
    [QueryArgument]
    public sealed class VariableQueryArgument
    {
        public VariableQueryArgument()
        {
        }

        public VariableQueryArgument(object? value, Type? type = null)
        {
            if (type is null)
            {
                if (value is null)
                {
                    type = typeof(object);
                }
                else
                {
                    type = value.GetType();
                }
            }

            Type = type.AsTypeInfo();

            Value = value;
        }

        public VariableQueryArgument(object? value, TypeInfo? type = null)
        {
            if (type is null)
            {
                var valueType = value is null ? typeof(object) : value.GetType();

                type = valueType.AsTypeInfo();
            }

            Type = type;

            Value = value;
        }

        [DataMember(Order = 1, IsRequired = true, EmitDefaultValue = false)]
        public TypeInfo Type { get; set; } = null!;

        [DataMember(Order = 2, IsRequired = true, EmitDefaultValue = true)]
        public object? Value { get; set; }

        public override string ToString() => $"{nameof(VariableQueryArgument)}({Value.QuoteValue()})";
    }
}
