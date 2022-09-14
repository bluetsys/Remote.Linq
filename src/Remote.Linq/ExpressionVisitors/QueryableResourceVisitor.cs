﻿// Copyright (c) Christof Senn. All rights reserved. See license.txt in the project root for license information.

namespace Remote.Linq.ExpressionVisitors
{
    using Aqua.Dynamic;
    using Aqua.EnumerableExtensions;
    using Aqua.TypeExtensions;
    using Aqua.TypeSystem;
    using Remote.Linq.DynamicQuery;
    using Remote.Linq.Expressions;
    using System;
    using System.Linq;
    using System.Threading;

    public abstract class QueryableResourceVisitor
    {
        internal static TExpression ReplaceResourceDescriptorsByQueryable<TExpression, TQueryable>(TExpression expression, Func<Type, TQueryable> provider, ITypeResolver? typeResolver)
            where TExpression : Expression
            => (TExpression)new ResourceDescriptorVisitor<TQueryable>(provider, typeResolver).Run(expression);

        internal static Expression ReplaceQueryablesByResourceDescriptors(Expression expression, ITypeInfoProvider? typeInfoProvider)
            => new QueryableVisitor(typeInfoProvider).Run(expression);

        protected class ResourceDescriptorVisitor<TQueryable> : RemoteExpressionVisitorBase
        {
            private readonly ITypeResolver _typeResolver;
            private readonly Func<Type, TQueryable> _provider;

            internal protected ResourceDescriptorVisitor(Func<Type, TQueryable> provider, ITypeResolver? typeResolver)
            {
                _provider = provider.CheckNotNull(nameof(provider));
                _typeResolver = typeResolver ?? TypeResolver.Instance;
            }

            internal Expression Run(Expression expression) => Visit(expression);

            protected override Expression VisitConstant(ConstantExpression node)
            {
                var value = node.CheckNotNull(nameof(node)).Value;
                if (TryGetQueryableByQueryableResourceDescriptor(value, out var queryable))
                {
                    return CreateClosure(queryable);
                }

                if (TryResolveQueryableSuorceInConstantQueryArgument(value, out var constantQueryArgument))
                {
#pragma warning disable CA1062 // Validate arguments of public methods -> false positive.
                    return new ConstantExpression(constantQueryArgument, node.Type);
#pragma warning restore CA1062 // Validate arguments of public methods
                }

                if (TryResolveSubstitutedValue(value, out var substitutedValue))
                {
                    return new ConstantExpression(substitutedValue, node.Type);
                }

                return base.VisitConstant(node);

                static Expression CreateClosure(TQueryable? value)
                {
                    // TODO: move EF Core specific code/behavious into corresponding project.
                    // to support EF Core subqueries, queryables must no be returned as ConstantExpression but wrapped as closure.
                    if (value?.GetType().Implements(typeof(IQueryable<>), out var genericargs) is true)
                    {
                        var queryableType = typeof(IQueryable<>).MakeGenericType(genericargs);
                        var closure = Activator.CreateInstance(typeof(Closure<>).MakeGenericType(queryableType), value)
                            ?? throw new RemoteLinqException($"Failed to create closure for type {queryableType}.");
                        var valueProperty = closure.GetType().GetProperty(nameof(Closure<TQueryable>.Value))
                            ?? throw new RemoteLinqException("Failed to get 'Closure.Value' property info.");
                        return new MemberExpression(new ConstantExpression(closure), valueProperty);
                    }

                    return new ConstantExpression(value);
                }
            }

            private bool TryGetQueryableByQueryableResourceDescriptor(object? value, out TQueryable? queryable)
            {
                if (value is QueryableResourceDescriptor queryableResourceDescriptor)
                {
                    var queryableType = queryableResourceDescriptor.Type.ResolveType(_typeResolver);
                    queryable = _provider(queryableType);
                    return true;
                }

                queryable = default;
                return false;
            }

            private bool TryResolveQueryableSuorceInConstantQueryArgument(object? value, out ConstantQueryArgument? newConstantQueryArgument)
            {
                if (value is ConstantQueryArgument constantQueryArgument)
                {
                    var hasChanged = false;
                    var tempConstantQueryArgument = new ConstantQueryArgument(constantQueryArgument.Value);
                    foreach (var property in tempConstantQueryArgument.Value.Properties.AsEmptyIfNull())
                    {
                        if (TryGetQueryableByQueryableResourceDescriptor(property.Value, out var queryable))
                        {
                            property.Value = queryable;
                            hasChanged = true;
                        }
                    }

                    if (hasChanged)
                    {
                        newConstantQueryArgument = tempConstantQueryArgument;
                        return true;
                    }
                }

                newConstantQueryArgument = null;
                return false;
            }

            private bool TryResolveSubstitutedValue(object? value, out object? resubstitutionValue)
            {
                if (value is SubstitutionValue substitutionValue)
                {
                    var type = substitutionValue.Type.ResolveType(_typeResolver);
                    if (type == typeof(CancellationToken))
                    {
                        // TODO: should/can cancellation token be retrieved from context?
                        resubstitutionValue = CancellationToken.None;
                        return true;
                    }
                }

                resubstitutionValue = null;
                return false;
            }
        }

        protected class QueryableVisitor : RemoteExpressionVisitorBase
        {
            private readonly ITypeInfoProvider _typeInfoProvider;

            internal protected QueryableVisitor(ITypeInfoProvider? typeInfoProvider)
            {
                _typeInfoProvider = typeInfoProvider ?? new TypeInfoProvider(false, false);
            }

            internal Expression Run(Expression expression)
                => Visit(expression);

            protected override Expression VisitConstant(ConstantExpression node)
            {
                if (node.CheckNotNull(nameof(node)).Value.AsQueryableResourceTypeOrNull() is Type resourceType)
                {
                    var typeInfo = _typeInfoProvider.GetTypeInfo(resourceType);
                    var queryableResourceDescriptor = new QueryableResourceDescriptor(typeInfo);
                    return new ConstantExpression(queryableResourceDescriptor);
                }

                if (node.Value is ConstantQueryArgument constantQueryArgument)
                {
                    var copy = new DynamicObject(constantQueryArgument.Value);
                    foreach (var property in copy.Properties.AsEmptyIfNull())
                    {
                        if (property.Value.AsQueryableResourceTypeOrNull() is Type resourceTypePropertyValue)
                        {
                            var typeInfo = _typeInfoProvider.GetTypeInfo(resourceTypePropertyValue);
                            var queryableResourceDescriptor = new QueryableResourceDescriptor(typeInfo);
                            property.Value = queryableResourceDescriptor;
                        }
                    }

                    return new ConstantExpression(new ConstantQueryArgument(copy), node.Type);
                }

                if (node.Value is CancellationToken)
                {
                    var substitutionValue = new SubstitutionValue(node.Type);
                    return new ConstantExpression(substitutionValue, node.Type);
                }

                return base.VisitConstant(node);
            }
        }
    }
}