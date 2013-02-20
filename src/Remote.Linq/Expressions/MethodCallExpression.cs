﻿// Copyright (c) Christof Senn. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Remote.Linq.Expressions
{
    [Serializable]
    [DataContract]
    public sealed class MethodCallExpression : Expression
    {
        internal MethodCallExpression(Expression insatnce, MethodInfo methodInfo, IEnumerable<Expression> arguments)
            : this(insatnce, methodInfo.Name, methodInfo.DeclaringType, BindingFlags.Default, methodInfo.GetParameters().Select(p => p.ParameterType).ToArray(), arguments)
        {
            var bindingFlags = methodInfo.IsStatic ? BindingFlags.Static : BindingFlags.Instance;
            bindingFlags |= methodInfo.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic;
            BindingFlags = bindingFlags;
        }

        internal MethodCallExpression(Expression insatnce, string methodName, Type declaringType, BindingFlags bindingFlags, Type[] parameterTypes, IEnumerable<Expression> arguments)
        {
            Instance = insatnce;
            MethodName = methodName;
            DeclaringTypeName = declaringType.FullName;//.AssemblyQualifiedName;
            ParameterTypeNames = parameterTypes.Length == 0 ? null : parameterTypes.Select(p => p.FullName).ToArray();
            Arguments = arguments.ToArray();
        }

        public override ExpressionType NodeType { get { return ExpressionType.MethodCall; } }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Expression Instance { get; private set; }

        [DataMember(IsRequired = true, EmitDefaultValue = false)]
#if SILVERLIGHT
        public string MethodName { get; private set; }
#else
        private string MethodName { get; set; }
#endif

        [DataMember(Name = "DeclaringType", IsRequired = true, EmitDefaultValue = false)]
#if SILVERLIGHT
        public string DeclaringTypeName { get; private set; }
#else
        private string DeclaringTypeName { get; set; }
#endif

        [DataMember(Name = "ParameterTypes", IsRequired = false, EmitDefaultValue = false)]
#if SILVERLIGHT
        public string[] ParameterTypeNames { get; private set; }
#else
        private string[] ParameterTypeNames { get; set; }
#endif

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
#if SILVERLIGHT
        public BindingFlags BindingFlags { get; private set; }
#else
        private BindingFlags BindingFlags { get; set; }
#endif

        public MethodInfo MethodInfo
        {
            get
            {
                if (ReferenceEquals(_methodInfo, null))
                {
                    var declaringType = Type.GetType(DeclaringTypeName);
                    if (ReferenceEquals(declaringType, null))
                    {
                        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            declaringType = assembly.GetType(DeclaringTypeName);
                            if (!ReferenceEquals(declaringType, null)) break;
                        }
                        if (ReferenceEquals(declaringType, null))
                        {
                            throw new Exception(string.Format("Declaring type '{0}' could not be reconstructed", DeclaringTypeName));
                        }
                    }


                    var parameterTypes = ParameterTypeNames == null ? new Type[0] : ParameterTypeNames
                        .Select(p =>
                        {
                            var parameterType = Type.GetType(p);
                            if (ReferenceEquals(parameterType, null))
                            {
                                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                                {
                                    parameterType = assembly.GetType(p);
                                    if (!ReferenceEquals(parameterType, null)) break;
                                }
                                if (ReferenceEquals(parameterType, null))
                                {
                                    throw new Exception(string.Format("Parameter type '{0}' could not be reconstructed", p));
                                }
                            }
                            return parameterType;
                        })
                        .ToArray();


                    var methodInfo = declaringType.GetMethod(MethodName, BindingFlags, null, parameterTypes, null);
                    if (methodInfo == null) methodInfo = declaringType.GetMethod(MethodName);
                    _methodInfo = methodInfo;
                }
                return _methodInfo;
            }
        }
        private MethodInfo _methodInfo;
        
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Expression[] Arguments { get; private set; }

        public override string ToString()
        {
            return string.Format("{0}.{1}({2})",
                Instance == null ? DeclaringTypeName : Instance.ToString(), 
                MethodName, 
                // ParameterTypes == null ? null : string.Join(", ", ParameterTypes)
                Arguments == null ? null : string.Join(", ", Arguments.Select(a => a.ToString()).ToArray()));
        }
    }
}
