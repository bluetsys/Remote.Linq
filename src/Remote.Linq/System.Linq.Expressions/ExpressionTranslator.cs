﻿// Copyright (c) Christof Senn. All rights reserved. See license.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using RLinq = Remote.Linq.Expressions;

namespace System.Linq.Expressions
{
    public static class ExpressionTranslator
    {
        private static readonly Dictionary<object, ParameterExpression> ParameterCache = new Dictionary<object, ParameterExpression>();

        private static ParameterExpression GetParameterExpression(Type type, string name = "i")
        {
            lock (ParameterCache)
            {
                var key = new { type, name };
                ParameterExpression exp;
                if (!ParameterCache.TryGetValue(key, out exp))
                {
                    exp = Expression.Parameter(type, name);
                    ParameterCache.Add(key, exp);
                }
                return exp;
            }
        }
        
        /// <summary>
        /// Translates a given lambda expression into a query expression
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static RLinq.Expression ToQueryExpression(this LambdaExpression expression)
        {
            return new ExpressionToQueryExpressionTranslator().ToFilterExpression(expression);
        }
        
        /// <summary>
        /// Translates a given query expression into an expression
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static Expression ToLinqExpression(this RLinq.Expression expression)
        {
            var exp = new FilterExpressionToExpressionTranslator().ToExpression(expression);
            return exp;
        }

        /// <summary>
        /// Translates a given query expression into a lambda expression
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static Expression<Func<T, TResult>> ToLinqExpression<T, TResult>(this RLinq.Expression expression)
        {
            var exp = expression.ToLinqExpression();
            var parameter = GetParameterExpression(typeof(T));
            var lambdaExpression = Expression.Lambda<Func<T, TResult>>(exp, parameter);
            return lambdaExpression;
        }

        /// <summary>
        /// Translates a given query expression into a lambda expression
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static LambdaExpression ToLinqExpression<T>(this RLinq.Expression expression)
        {
            var exp = expression.ToLinqExpression();
            var parameter = GetParameterExpression(typeof(T));
            var lambdaExpression = Expression.Lambda(exp, parameter);
            return lambdaExpression;
        }

        private static System.Linq.Expressions.ConstantExpression Wrap(this RLinq.Expression expression)
        {
            return System.Linq.Expressions.Expression.Constant(expression);
        }

        private static RLinq.Expression Unwrap(this System.Linq.Expressions.Expression expression)
        {
            if (expression.NodeType == ExpressionType.Constant && typeof(RLinq.Expression).IsAssignableFrom(expression.Type))
            {
                return (RLinq.Expression)((System.Linq.Expressions.ConstantExpression)expression).Value;
            }
            else
            {
                return null;
            }
        }

        private sealed class ExpressionToQueryExpressionTranslator : ExpressionVisitor
        {
            public RLinq.Expression ToFilterExpression(LambdaExpression expression)
            {
                var partialEvalExpression = expression.PartialEval() as LambdaExpression;
                if (partialEvalExpression == null) throw CreateNotSupportedException(expression);
                var constExpression = Visit(partialEvalExpression.Body);
                return constExpression.Unwrap();
            }

            protected override Expression VisitConstant(ConstantExpression c)
            {
                return new RLinq.ConstantValueExpression(c.Value).Wrap();
            }

            protected override Expression VisitParameter(ParameterExpression p)
            {
                return new RLinq.ParameterExpression(p.Name, p.Type).Wrap();
            }

            protected override Expression VisitBinary(BinaryExpression b)
            {
                #region obsolete
                //if (b.Left.NodeType == ExpressionType.Constant || b.Right.NodeType == ExpressionType.Constant ||
                //    b.Left.NodeType == ExpressionType.Coalesce || b.Right.NodeType == ExpressionType.Coalesce)
                //{
                //    var left = this.Visit(b.Left);
                //    var right = this.Visit(b.Right);
                //    var conversion = this.Visit(b.Conversion);

                //    if (left != b.Left || right != b.Right || conversion != b.Conversion)
                //    {
                //        if (left.NodeType != ExpressionType.Constant)
                //        {
                //            left = Expression.Constant(new Filter.ConstantValueExpression { Value = left.ToString() });
                //        }
                //        if (right.NodeType != ExpressionType.Constant)
                //        {
                //            right = Expression.Constant(new Filter.ConstantValueExpression { Value = right.ToString() });
                //        }

                //        if (b.NodeType == ExpressionType.Coalesce && b.Conversion != null)
                //        {
                //            //what about conversion?
                //            //return Expression.Coalesce(left, right, conversion as LambdaExpression);
                //            return Expression.Constant(new Filter.BinaryExpression
                //            {
                //                LeftHandSideOperand = (Filter.Expression)((ConstantExpression)left).Value,
                //                RightHandSideOperand = (Filter.Expression)((ConstantExpression)right).Value,
                //                Operator = Filter.BinaryOperator.Coalesce,
                //            });
                //        }

                //        return Expression.Constant(new Filter.BinaryExpression
                //        {
                //            LeftHandSideOperand = (Filter.Expression)((ConstantExpression)left).Value,
                //            RightHandSideOperand = (Filter.Expression)((ConstantExpression)right).Value,
                //            Operator = TranslateBinaryOperator(b.NodeType),
                //        });
                //    }
                //}
                //else
                //{
                //    var left = Visit(b.Left);
                //    var right = Visit(b.Right);
                //    var conversion = Visit(b.Conversion);

                //    if (left != b.Left || right != b.Right || conversion != b.Conversion)
                //    {
                //        if (b.NodeType == ExpressionType.Coalesce && b.Conversion != null)
                //        {
                //            //what about conversion?
                //            //return Expression.Coalesce(left, right, conversion as LambdaExpression);
                //            return Expression.Constant(new Filter.BinaryExpression
                //            {
                //                LeftHandSideOperand = (Filter.Expression)((ConstantExpression)left).Value,
                //                RightHandSideOperand = (Filter.Expression)((ConstantExpression)right).Value,
                //                Operator = Filter.BinaryOperator.Coalesce,
                //            });
                //        }

                //        return Expression.Constant(new Filter.BinaryExpression
                //        {
                //            LeftHandSideOperand = (Filter.Expression)((ConstantExpression)left).Value,
                //            RightHandSideOperand = (Filter.Expression)((ConstantExpression)right).Value,
                //            Operator = TranslateBinaryOperator(b.NodeType),
                //        });
                //    }
                //}
                #endregion obsolete

                if (b.Conversion == null)
                {
                    var left = Visit(b.Left);
                    var right = Visit(b.Right);
                    //var conversion = Visit(b.Conversion);

                    if (left.NodeType == ExpressionType.Constant && right.NodeType == ExpressionType.Constant)
                    {
                        if (typeof(RLinq.Expression).IsAssignableFrom(left.Type) && typeof(RLinq.Expression).IsAssignableFrom(right.Type))
                        {
                            if (b.NodeType == ExpressionType.Equal || b.NodeType == ExpressionType.NotEqual)
                            {
                                if (typeof(RLinq.ConstantValueExpression).IsAssignableFrom(left.Type))
                                {
                                    if (((RLinq.ConstantValueExpression)((ConstantExpression)left).Value).Value is bool)
                                    {
                                        if (b.NodeType == ExpressionType.Equal ^ (bool)((RLinq.ConstantValueExpression)((ConstantExpression)left).Value).Value)
                                        {
                                            // != true
                                            // == false
                                            return new RLinq.UnaryExpression(
                                                right.Unwrap(),
                                                RLinq.UnaryOperator.Not
                                            ).Wrap();
                                        }
                                        else
                                        {
                                            // == true
                                            // != false
                                            return right;
                                        }
                                    }
                                    if (((RLinq.ConstantValueExpression)((ConstantExpression)left).Value).Value == null/* && ((Filter.ConstantValueExpression)((ConstantExpression)right).Value).Value != null*/)
                                    {
                                        return new RLinq.UnaryExpression(
                                            right.Unwrap(),
                                            b.NodeType == ExpressionType.Equal ? RLinq.UnaryOperator.IsNull : RLinq.UnaryOperator.IsNotNull
                                        ).Wrap();
                                    }
                                }
                                if (typeof(RLinq.ConstantValueExpression).IsAssignableFrom(right.Type))
                                {
                                    if (((RLinq.ConstantValueExpression)((ConstantExpression)right).Value).Value is bool)
                                    {
                                        if (b.NodeType == ExpressionType.Equal ^ (bool)((RLinq.ConstantValueExpression)((ConstantExpression)right).Value).Value)
                                        {
                                            // != true
                                            // == false
                                            return new RLinq.UnaryExpression(
                                                left.Unwrap(),
                                                RLinq.UnaryOperator.Not
                                            ).Wrap();
                                        }
                                        else
                                        {
                                            // == true
                                            // != false
                                            return left;
                                        }
                                    }
                                    if (((RLinq.ConstantValueExpression)((ConstantExpression)right).Value).Value == null/* && ((Filter.ConstantValueExpression)((ConstantExpression)left).Value).Value != null*/)
                                    {
                                        return new RLinq.UnaryExpression(
                                            left.Unwrap(),
                                            b.NodeType == ExpressionType.Equal ? RLinq.UnaryOperator.IsNull : RLinq.UnaryOperator.IsNotNull
                                        ).Wrap();
                                    }
                                }
                            }

                            return new RLinq.BinaryExpression(
                                left.Unwrap(),
                                right.Unwrap(),
                                TranslateBinaryOperator(b.NodeType)
                            ).Wrap();
                        }
                    }
                }

                throw CreateNotSupportedException(b);
            }

            protected override Expression VisitMemberAccess(MemberExpression m)
            {
                RLinq.PropertyAccessExpression parent = null;

                if (m.Expression.NodeType == ExpressionType.Parameter)
                {
                }
                else if (m.Expression.NodeType == ExpressionType.MemberAccess)
                {
                    var exp = Visit(m.Expression);
                    var parentExpression = exp.Unwrap() as RLinq.PropertyAccessExpression;
                    if (parentExpression == null) throw new Exception("navigation path could not be resolved");
                    parent = parentExpression;
                }
                else
                {
                    throw CreateNotSupportedException(m);
                }

                var propertyInfo = (PropertyInfo)m.Member;
                return new RLinq.PropertyAccessExpression(propertyInfo, parent).Wrap();
            }
            
            protected override Expression VisitMethodCall(MethodCallExpression m)
            {
                // property selector
                if (m.Method.Name == "get_Item" && m.Arguments.Count == 1 && m.Arguments[0].NodeType == ExpressionType.Constant && ((ConstantExpression)m.Arguments[0]).Type == typeof(string))
                {
                    var exp = (ConstantExpression)m.Arguments[0];
                    var parent = Visit(m.Object).Unwrap() as RLinq.PropertyAccessExpression;
                    var propertyInfo = m.Object.Type.GetProperty((string)exp.Value, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (propertyInfo == null) throw new Exception(string.Format("'{0}' is not a valid or an ambiguous property of type {1}", (string)exp.Value, m.Object.Type.FullName));
                    return new RLinq.PropertyAccessExpression(propertyInfo, parent).Wrap();
                }
                else if (m.Object != null && m.Object.Type == typeof(string) && m.Arguments.Count == 1)
                {
                    var obj = Visit(m.Object);
                    var p1 = obj.Unwrap();

                    var param = Visit(m.Arguments[0]);
                    var p2 = param.Unwrap();

                    switch (((MethodCallExpression)m).Method.Name)
                    {
                        case "Contains":
                            return new RLinq.BinaryExpression(p1, p2, RLinq.BinaryOperator.Contains).Wrap();
                        case "StartsWith":
                            return new RLinq.BinaryExpression(p1, p2, RLinq.BinaryOperator.StartsWith).Wrap();
                        case "EndsWith":
                            return new RLinq.BinaryExpression(p1, p2, RLinq.BinaryOperator.EndsWith).Wrap();
                    }
                }
                else if (m.Method.Name == "Contains" && m.Object == null && m.Arguments.Count == 2 && m.Arguments[0].NodeType == ExpressionType.Constant && ((ConstantExpression)m.Arguments[0]).Value is System.Collections.IEnumerable)
                {
                    var list =
                        from item in ((System.Collections.IEnumerable)((ConstantExpression)m.Arguments[0]).Value).OfType<object>()
                        select new RLinq.ConstantValueExpression(item);

                    var a2 = Visit(m.Arguments[1]).Unwrap();

                    return new RLinq.BinaryExpression(a2, new RLinq.CollectionExpression(list), RLinq.BinaryOperator.In).Wrap();
                }

                throw CreateNotSupportedException(m);
            }

            protected override Expression VisitUnary(UnaryExpression u)
            {
                var operand = Visit(u.Operand).Unwrap();

                if (u.NodeType == ExpressionType.Convert && u.Type == u.Type.UnderlyingSystemType)
                {
                    return new RLinq.ConversionExpression(operand, u.Type).Wrap();
                }

                if (u.NodeType == ExpressionType.Not && operand != null)
                {
                    return new RLinq.UnaryExpression(operand, RLinq.UnaryOperator.Not).Wrap();
                }

                throw CreateNotSupportedException(u);
            }

            private static NotSupportedException CreateNotSupportedException(Expression expression)
            {
                return new NotSupportedException(string.Format("expression: '{0}'", expression));
            }

            private static RLinq.BinaryOperator TranslateBinaryOperator(ExpressionType nodeType)
            {
                switch (nodeType)
                {
                    case ExpressionType.Add:
                    case ExpressionType.AddChecked:
                        return RLinq.BinaryOperator.Add;
                    case ExpressionType.Subtract:
                    case ExpressionType.SubtractChecked:
                        return RLinq.BinaryOperator.Subtract;
                    case ExpressionType.Multiply:
                    case ExpressionType.MultiplyChecked:
                        return RLinq.BinaryOperator.Multiply;
                    case ExpressionType.Divide:
                        return RLinq.BinaryOperator.Divide;
                    case ExpressionType.Modulo:
                        return RLinq.BinaryOperator.Modulo;
                    case ExpressionType.And:
                        return RLinq.BinaryOperator.BitwiseAnd;
                    case ExpressionType.AndAlso:
                        return RLinq.BinaryOperator.And;
                    case ExpressionType.Or:
                        return RLinq.BinaryOperator.BitwiseOr;
                    case ExpressionType.OrElse:
                        return RLinq.BinaryOperator.Or;
                    case ExpressionType.LessThan:
                        return RLinq.BinaryOperator.And;
                    case ExpressionType.LessThanOrEqual:
                        return RLinq.BinaryOperator.LessThanOrEqual;
                    case ExpressionType.GreaterThan:
                        return RLinq.BinaryOperator.GreaterThan;
                    case ExpressionType.GreaterThanOrEqual:
                        return RLinq.BinaryOperator.GreaterThanOrEqual;
                    case ExpressionType.Equal:
                        return RLinq.BinaryOperator.Equal;
                    case ExpressionType.NotEqual:
                        return RLinq.BinaryOperator.NotEqual;
                    //case ExpressionType.RightShift:
                    //    return Filter.BinaryOperator.RightShift;
                    //case ExpressionType.LeftShift:
                    //    return Filter.BinaryOperator.LeftShift;
                    case ExpressionType.ExclusiveOr:
                        return RLinq.BinaryOperator.ExclusiveOr;
                    case ExpressionType.Coalesce:
                        return RLinq.BinaryOperator.Coalesce;
                    //case ExpressionType.ArrayIndex:
                    default: throw new NotSupportedException(string.Format("No translation defined for binary operator {0}", nodeType));
                }
            }
        }

        private sealed class FilterExpressionToExpressionTranslator
        {
            private static readonly MethodInfo StringStartsWithMethodInfo = typeof(string).GetMethod("StartsWith", new[] { typeof(string) });
            private static readonly MethodInfo StringEndsWithMethodInfo = typeof(string).GetMethod("EndsWith", new[] { typeof(string) });
            private static readonly MethodInfo StringContainsMethodInfo = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            private static readonly MethodInfo EnumerableOfTypeMethodInfo = typeof(System.Linq.Enumerable).GetMethod("OfType", BindingFlags.Public | BindingFlags.Static);
            private static readonly MethodInfo EnumerableContainsMethodInfo = typeof(System.Linq.Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static).Single(m => m.Name == "Contains" && m.GetParameters().Length == 2);

            public Expression ToExpression(RLinq.Expression expression)
            {
                var exp = Visit(expression);
                return exp;
            }

            private Expression Visit(RLinq.Expression expression)
            {
                switch (expression.NodeType)
                {
                    case RLinq.ExpressionType.Binary:
                        return Visit((RLinq.BinaryExpression)expression);
                    case RLinq.ExpressionType.Collection:
                        return Visit((RLinq.CollectionExpression)expression);
                    case RLinq.ExpressionType.ConstantValue:
                        return Visit((RLinq.ConstantValueExpression)expression);
                    case RLinq.ExpressionType.Conversion:
                        return Visit((RLinq.ConversionExpression)expression);
                    case RLinq.ExpressionType.Parameter:
                        return Visit((RLinq.ParameterExpression)expression);
                    case RLinq.ExpressionType.PropertyAccess:
                        return Visit((RLinq.PropertyAccessExpression)expression);
                    case RLinq.ExpressionType.Unary:
                        return Visit((RLinq.UnaryExpression)expression);
                    default:
                        throw new Exception(string.Format("Unknown expression note type: '{0}'", expression.NodeType));
                }
            }

            private Expression Visit(RLinq.ParameterExpression parameterExpression)
            {
                return GetParameterExpression(parameterExpression.ParameterType/*, parameterExpression.ParameterName*/);
            }

            private Expression Visit(RLinq.UnaryExpression unaryExpression)
            {
                var exp = Visit(unaryExpression.Operand);
                switch (unaryExpression.Operator)
                {
                    case RLinq.UnaryOperator.IsNotNull:
                        return Expression.MakeBinary(ExpressionType.NotEqual, exp, Expression.Constant(null));
                    case RLinq.UnaryOperator.IsNull:
                        return Expression.MakeBinary(ExpressionType.Equal, exp, Expression.Constant(null));
                    case RLinq.UnaryOperator.Not:
                        return Expression.MakeUnary(ExpressionType.Not, exp, typeof(bool));
                    default:
                        throw new Exception(string.Format("Unknown unary operation: '{0}'", unaryExpression.Operator));
                }
            }

            private Expression Visit(RLinq.PropertyAccessExpression propertyAccessExpression)
            {
                Expression exp;
                if (propertyAccessExpression.Parent != null)
                {
                    exp = Visit(propertyAccessExpression.Parent);
                }
                else
                {
                    //exp = Expression.Parameter(propertyAccessExpression.PropertyInfo.DeclaringType, "i");
                    exp = GetParameterExpression(propertyAccessExpression.PropertyInfo.DeclaringType);
                }

                return Expression.MakeMemberAccess(exp, propertyAccessExpression.PropertyInfo);                
            }

            private Expression Visit(RLinq.ConversionExpression conversionExpression)
            {
                var exp = Visit(conversionExpression.Operand);
                return Expression.Convert(exp, conversionExpression.Type);
            }

            private Expression Visit(RLinq.ConstantValueExpression constantValueExpression)
            {
                return Expression.Constant(constantValueExpression.Value);
            }

            private Expression Visit(RLinq.CollectionExpression collectionExpression)
            {
                var list =
                    from exp in collectionExpression.List
                    select exp.Value;
                return Expression.Constant(list.ToList());
            }

            private Expression Visit(RLinq.BinaryExpression binaryExpression)
            {
                var p1 = Visit(binaryExpression.LeftOperand);
                var p2 = Visit(binaryExpression.RightOperand);

                switch (binaryExpression.Operator)
                {
                    case RLinq.BinaryOperator.StartsWith:
                        return Expression.Call(p1, StringStartsWithMethodInfo, p2);
                    case RLinq.BinaryOperator.EndsWith:
                        return Expression.Call(p1, StringEndsWithMethodInfo, p2);
                    case RLinq.BinaryOperator.Contains:
                        return Expression.Call(p1, StringContainsMethodInfo, p2);
                    case RLinq.BinaryOperator.In:
                        if (p1.Type == typeof(object))
                        {
                            return Expression.Call(null, EnumerableContainsMethodInfo.MakeGenericMethod(p1.Type), p2, p1);
                        }
                        else
                        {
                            var typeConvertionMethod = Expression.Call(null, EnumerableOfTypeMethodInfo.MakeGenericMethod(p1.Type), p2);
                            return Expression.Call(null, EnumerableContainsMethodInfo.MakeGenericMethod(p1.Type), typeConvertionMethod, p1);
                        }
                    default:
                        var type = TranslateBinaryOperator(binaryExpression.Operator);
                        return Expression.MakeBinary(type, p1, p2);
                }
            }

            private static ExpressionType TranslateBinaryOperator(RLinq.BinaryOperator @operator)
            {
                switch (@operator)
                {
                    case RLinq.BinaryOperator.Add:
                        return ExpressionType.Add;
                    case RLinq.BinaryOperator.And:
                        return ExpressionType.AndAlso;
                    case RLinq.BinaryOperator.BitwiseAnd:
                        return ExpressionType.And;
                    case RLinq.BinaryOperator.BitwiseOr:
                        return ExpressionType.Or;
                    case RLinq.BinaryOperator.Coalesce:
                        return ExpressionType.Coalesce;
                    case RLinq.BinaryOperator.Contains:
                        throw new Exception("needs translation into method call expression");
                    case RLinq.BinaryOperator.Divide:
                        return ExpressionType.Divide;
                    case RLinq.BinaryOperator.EndsWith:
                        throw new Exception("needs translation into method call expression");
                    case RLinq.BinaryOperator.Equal:
                        return ExpressionType.Equal;
                    case RLinq.BinaryOperator.ExclusiveOr:
                        return ExpressionType.ExclusiveOr;
                    case RLinq.BinaryOperator.GreaterThan:
                        return ExpressionType.GreaterThan;
                    case RLinq.BinaryOperator.GreaterThanOrEqual:
                        return ExpressionType.GreaterThanOrEqual;
                    case RLinq.BinaryOperator.In:
                        throw new Exception("needs translation into method call expression");
                    case RLinq.BinaryOperator.LessThan:
                        return ExpressionType.LessThan;
                    case RLinq.BinaryOperator.LessThanOrEqual:
                        return ExpressionType.LessThanOrEqual;
                    //case Filter.BinaryOperator.Like:
                    case RLinq.BinaryOperator.Modulo:
                        return ExpressionType.Modulo;
                    case RLinq.BinaryOperator.Multiply:
                        return ExpressionType.Multiply;
                    case RLinq.BinaryOperator.NotEqual:
                        return ExpressionType.NotEqual;
                    //case Filter.BinaryOperator.NotLike:
                    case RLinq.BinaryOperator.Or:
                        return ExpressionType.OrElse;
                    case RLinq.BinaryOperator.StartsWith:
                        throw new Exception("needs translation into method call expression");
                    case RLinq.BinaryOperator.Subtract:
                        return ExpressionType.Subtract;
                    default:
                        throw new Exception(string.Format("Unknown binary operation: '{0}'", @operator));
                }
            }
        }
    }
}