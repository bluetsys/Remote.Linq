﻿// Copyright (c) Christof Senn. All rights reserved. See license.txt in the project root for license information.

#if NETFRAMEWORK

namespace Remote.Linq.Tests.Serialization
{
    using Remote.Linq.ExpressionVisitors;
    using System.IO;
    using System.Runtime.Serialization;

    public static class NetDataContractSerializationHelper
    {
        public static T Clone<T>(this T graph)
        {
            var serializer = new NetDataContractSerializer();
            using var stream = new MemoryStream();
            serializer.Serialize(stream, graph);
            stream.Seek(0, SeekOrigin.Begin);
            return (T)serializer.Deserialize(stream);
        }

        public static T CloneExpression<T>(T expression)
            where T : Remote.Linq.Expressions.Expression
        {
            var exp1 = expression.ReplaceGenericQueryArgumentsByNonGenericArguments();
            var exp2 = Clone(exp1);
            var exp3 = exp2.ReplaceNonGenericQueryArgumentsByGenericArguments();
            return exp3;
        }
    }
}

#endif