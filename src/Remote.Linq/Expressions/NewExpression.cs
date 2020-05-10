﻿// Copyright (c) Christof Senn. All rights reserved. See license.txt in the project root for license information.

namespace Remote.Linq.Expressions
{
    using Aqua.TypeSystem;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;

    [Serializable]
    [DataContract]
    public sealed class NewExpression : Expression
    {
        public NewExpression()
        {
        }

        public NewExpression(TypeInfo type)
        {
            Type = type;
        }

        public NewExpression(Type type)
            : this(new TypeInfo(type))
        {
        }

        public NewExpression(ConstructorInfo constructor, IEnumerable<Expression> arguments, IEnumerable<MemberInfo> members = null)
            : this(constructor.DeclaringType)
        {
            Constructor = constructor;
            Arguments = arguments is null || !arguments.Any() ? null : arguments.ToList();
            Members = members is null || !members.Any() ? null : members.ToList();
        }

        public NewExpression(System.Reflection.ConstructorInfo constructor, IEnumerable<Expression> arguments = null, IEnumerable<System.Reflection.MemberInfo> members = null)
            : this(new ConstructorInfo(constructor), arguments, members?.Select(x => MemberInfo.Create(x)))
        {
        }

        public NewExpression(string name, Type declaringType, IEnumerable<Type> parameterTypes, IEnumerable<Expression> arguments = null, IEnumerable<System.Reflection.MemberInfo> members = null)
            : this(new ConstructorInfo(name, declaringType, parameterTypes), arguments, members?.Select(x => MemberInfo.Create(x)))
        {
        }

        public override ExpressionType NodeType => ExpressionType.New;

        [DataMember(Order = 1, IsRequired = false, EmitDefaultValue = false)]
        public ConstructorInfo Constructor { get; set; }

        [DataMember(Order = 2, IsRequired = false, EmitDefaultValue = false)]
        public List<Expression> Arguments { get; set; }

        [DataMember(Order = 3, IsRequired = false, EmitDefaultValue = false)]
        public List<MemberInfo> Members { get; set; }

        [DataMember(Order = 4, IsRequired = false, EmitDefaultValue = false)]
        public TypeInfo Type { get; set; }

        public override string ToString()
            => string.Format(
                "New {0}({1})",
                Constructor?.DeclaringType ?? Type,
                Arguments is null ? null : string.Join(", ", Arguments));
    }
}
