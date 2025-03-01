﻿// Copyright (c) Christof Senn. All rights reserved. See license.txt in the project root for license information.

namespace Remote.Linq.Tests.Serialization
{
    using Remote.Linq.ExpressionVisitors;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    public static class BinarySerializationHelper
    {
        public static T Clone<T>(T graph)
        {
#pragma warning disable SYSLIB0011 // Type or member is obsolete
            var serializer = new BinaryFormatter();
            using var stream = new MemoryStream();
            serializer.Serialize(stream, graph);
            stream.Seek(0, SeekOrigin.Begin);
            return (T)serializer.Deserialize(stream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
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