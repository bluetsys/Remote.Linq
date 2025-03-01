﻿// Copyright (c) Christof Senn. All rights reserved. See license.txt in the project root for license information.

namespace Remote.Linq.Tests.Serialization.Expressions
{
    using System;
    using System.IO;
    using System.Linq.Expressions;
    using Xunit;
    using RemoteLambdaExpression = Remote.Linq.Expressions.LambdaExpression;

    public abstract class When_using_GotoAndLabelExpressions_with_names
    {
        public class With_binary_formatter : When_using_GotoAndLabelExpressions_with_names
        {
            public With_binary_formatter()
                : base(BinarySerializationHelper.Clone)
            {
            }
        }

        public class With_data_contract_serializer : When_using_GotoAndLabelExpressions_with_names
        {
            public With_data_contract_serializer()
                : base(DataContractSerializationHelper.CloneExpression)
            {
            }
        }

        public class With_newtonsoft_json_serializer : When_using_GotoAndLabelExpressions_with_names
        {
            public With_newtonsoft_json_serializer()
                : base(NewtonsoftJsonSerializationHelper.Clone)
            {
            }
        }

        public class With_system_text_json_serializer : When_using_GotoAndLabelExpressions_with_names
        {
            public With_system_text_json_serializer()
                : base(SystemTextJsonSerializationHelper.Clone)
            {
            }
        }

#if NETFRAMEWORK
        public class With_net_data_contract_serializer : When_using_GotoAndLabelExpressions_with_names
        {
            public With_net_data_contract_serializer()
                : base(NetDataContractSerializationHelper.Clone)
            {
            }
        }
#endif

        public class With_protobuf_net_serializer : When_using_GotoAndLabelExpressions_with_names
        {
            public With_protobuf_net_serializer()
                : base(ProtobufNetSerializationHelper.Clone)
            {
            }
        }

        public class With_xml_serializer : When_using_GotoAndLabelExpressions_with_names
        {
            public With_xml_serializer()
                : base(XmlSerializationHelper.CloneExpression)
            {
            }
        }

        private readonly Expression<Func<StreamWriter, long>> _originalExpression;

        private readonly RemoteLambdaExpression _remoteExpression;

        private readonly RemoteLambdaExpression _serializedRemoteExpression;

        protected When_using_GotoAndLabelExpressions_with_names(Func<RemoteLambdaExpression, RemoteLambdaExpression> serialize)
        {
            ParameterExpression writer = Expression.Parameter(typeof(StreamWriter));
            ParameterExpression position = Expression.Variable(typeof(long));

            MemberExpression positionProperty = Expression.PropertyOrField(Expression.PropertyOrField(writer, "BaseStream"), "Position");

            LabelTarget returnLabel = Expression.Label(typeof(long), "LabelName");

            var expression = Expression.Lambda<Func<StreamWriter, long>>(
                Expression.Block(
                    new[] { position },
                    Expression.IfThen(
                        Expression.ReferenceEqual(Expression.Constant("SomeText"), Expression.Constant(null, typeof(string))),
                        Expression.Block(
                            Expression.Call(typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) }), Expression.Constant("Text is null")),
                            Expression.Return(returnLabel, Expression.Constant(0L), typeof(long)))),
                    Expression.Assign(position, positionProperty),
                    Expression.Call(writer, typeof(TextWriter).GetMethod(nameof(TextWriter.WriteLine), new[] { typeof(string) }), Expression.Constant("SomeText")),
                    Expression.Return(returnLabel, Expression.Subtract(positionProperty, position), typeof(long)),
                    Expression.Label(returnLabel, Expression.Default(typeof(long)))),
                writer);

            _originalExpression = expression;

            _remoteExpression = expression.ToRemoteLinqExpression();

            _serializedRemoteExpression = serialize(_remoteExpression);
        }

        [Fact]
        public void Expression_result_should_be_equal()
        {
            var argument = StreamWriter.Null;

            long long1 = _originalExpression.Compile()(argument);

            long long2 = _remoteExpression.ToLinqExpression<StreamWriter, long>().Compile()(argument);

            long long3 = _serializedRemoteExpression.ToLinqExpression<StreamWriter, long>().Compile()(argument);

            0L
                .ShouldMatch(long1)
                .ShouldMatch(long2)
                .ShouldMatch(long3);
        }
    }
}