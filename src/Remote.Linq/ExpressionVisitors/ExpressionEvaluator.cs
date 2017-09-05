﻿namespace Remote.Linq.ExpressionVisitors
{
    using Aqua.TypeSystem.Extensions;
    using Remote.Linq.DynamicQuery;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>  
    /// Enables the partial evalutation of queries.  
    /// From http://msdn.microsoft.com/en-us/library/bb546158.aspx  
    /// </summary>  
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ExpressionEvaluator
    {
        /// <summary>  
        /// Performs evaluation & replacement of independent sub-trees  
        /// </summary>  
        /// <param name="expression">The root of the expression tree.</param>  
        /// <param name="canBeEvaluatedLocally">A function that decides whether a given expression node can be evaluated locally, assumes true if no function defined</param>  
        /// <returns>A new tree with sub-trees evaluated and replaced.</returns>  
        public static Expression PartialEval(this Expression expression, Func<Expression, bool> canBeEvaluatedLocally = null)
        {
            return new SubtreeEvaluator(new Nominator(canBeEvaluatedLocally).Nominate(expression)).Eval(expression);
        }

        private static bool CanBeEvaluatedLocally(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Constant)
            {
                var value = ((ConstantExpression)expression).Value;
                if (value is IRemoteQueryable)
                {
                    return false;
                }

                var type = expression.Type;
                if (type.IsGenericType() && type.GetGenericTypeDefinition() == typeof(VariableQueryArgument<>))
                {
                    return false;
                }

                if (type == typeof(VariableQueryArgument))
                {
                    return false;
                }

                if (type == typeof(VariableQueryArgumentList))
                {
                    return false;
                }
            }

            if (expression.NodeType == ExpressionType.Call)
            {
                var methodCallExpression = (MethodCallExpression)expression;
                var methodDeclaringType = methodCallExpression.Method.DeclaringType;
                if (methodDeclaringType == typeof(Queryable) || methodDeclaringType == typeof(Enumerable))
                {
                    if (methodCallExpression.Arguments.Count > 0)
                    {
                        var argument = methodCallExpression.Arguments[0] as ConstantExpression;
                        if (!ReferenceEquals(null, argument) && argument.Value is IRemoteQueryable)
                        {
                            return false;
                        }
                    }
                }
            }

            switch (expression.NodeType)
            {
                case ExpressionType.Block:
                case ExpressionType.Default:
                case ExpressionType.Label:
                case ExpressionType.Goto:
                case ExpressionType.Lambda:
                case ExpressionType.Loop:
                case ExpressionType.New:
                case ExpressionType.Parameter:
                case ExpressionType.Throw:
                    return false;
            }

            return true;
        }

        /// <summary>  
        /// Evaluates & replaces sub-trees when first candidate is reached (top-down)  
        /// </summary>  
        private sealed class SubtreeEvaluator : ExpressionVisitorBase
        {
            private readonly HashSet<Expression> _candidates;

            internal SubtreeEvaluator(HashSet<Expression> candidates)
            {
                _candidates = candidates;
            }

            internal Expression Eval(Expression expression)
            {
                var expression2 = Visit(expression);
                return expression2;
            }

            protected override Expression Visit(Expression expression)
            {
                if (expression == null)
                {
                    return null;
                }

                if (_candidates.Contains(expression))
                {
                    return Evaluate(expression);
                }

                return base.Visit(expression);
            }

            private Expression Evaluate(Expression expression)
            {
                switch (expression.NodeType)
                {
                    case ExpressionType.Constant:
                    case ExpressionType.Convert:
                    case ExpressionType.ConvertChecked:
                    case ExpressionType.Quote:
                        return expression;
                }

                if (expression.NodeType == ExpressionType.Call && expression.Type == typeof(void))
                {
                    return expression;
                }

                var lambda = Expression.Lambda(expression);
                var func = lambda.Compile();
                object value;

                try
                {
                    value = func.DynamicInvoke(null);
                }
                catch(TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }

                var valueAsExpression = value as Expression;
                if (!ReferenceEquals(null, valueAsExpression))
                {
                    return valueAsExpression;
                }

                return Expression.Property(
                    Expression.New(
                        typeof(VariableQueryArgument<>).MakeGenericType(expression.Type).GetConstructor(new[] { expression.Type }),
                        Expression.Constant(value, expression.Type)),
                    nameof(VariableQueryArgument<object>.Value));
            }
        }

        /// <summary>  
        /// Performs bottom-up analysis to determine which nodes can possibly  
        /// be part of an evaluated sub-tree.  
        /// </summary>  
        private sealed class Nominator : ExpressionVisitorBase
        {
            private Func<Expression, bool> _fnCanBeEvaluated;
            private HashSet<Expression> _candidates;
            private bool _cannotBeEvaluated;

            internal Nominator(Func<Expression, bool> fnCanBeEvaluated)
            {
                _fnCanBeEvaluated = fnCanBeEvaluated == null
                    ? CanBeEvaluatedLocally
                    : new Func<Expression, bool>(exp => fnCanBeEvaluated(exp) && CanBeEvaluatedLocally(exp));
            }

            internal HashSet<Expression> Nominate(Expression expression)
            {
                _candidates = new HashSet<Expression>();
                Visit(expression);
                return _candidates;
            }

            protected override Expression Visit(Expression expression)
            {
                if (expression != null)
                {
                    bool saveCannotBeEvaluated = _cannotBeEvaluated;
                    _cannotBeEvaluated = false;
                    
                    base.Visit(expression);
                    
                    if (!_cannotBeEvaluated)
                    {
                        if (_fnCanBeEvaluated(expression))
                        {
                            _candidates.Add(expression);
                        }
                        else
                        {
                            _cannotBeEvaluated = true;
                        }
                    }
                    
                    _cannotBeEvaluated |= saveCannotBeEvaluated;
                }

                return expression;
            }
        }
    }
}