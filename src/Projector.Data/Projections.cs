﻿using Projector.Data.Filter;
using Projector.Data.Join;
using Projector.Data.Projection;
using System;
using System.Linq.Expressions;

namespace Projector.Data
{
    public static class Projections
    {
        public static Filter<Tsource> Where<Tsource>(this IDataProvider<Tsource> source, Expression<Func<Tsource, bool>> filterExpression)
        {
            return new Filter<Tsource>(source, filterExpression);
        }

        public static Projection<Tsource, TDest> Projection<Tsource, TDest>(this IDataProvider<Tsource> source, Expression<Func<Tsource, TDest>> transformerExpression)
        {
            return new Projection<Tsource, TDest>(source, transformerExpression);
        }

        public static Join<TLeft, TRight, TKey, TResult> InnerJoin<TLeft, TRight, TKey, TResult>(this IDataProvider<TLeft> leftSource,
                                                                                        IDataProvider<TRight> rightSource,
                                                                                        Expression<Func<TLeft, TKey>> leftKeySelector,
                                                                                        Expression<Func<TRight, TKey>> rightKeySelector,
                                                                                        Expression<Func<TLeft, TRight, TResult>> resultSelector)
        {
            return new Join<TLeft, TRight, TKey, TResult>(leftSource, rightSource, JoinType.Inner);
        }

        public static Join<TLeft, TRight, TKey, TResult> LeftJoin<TLeft, TRight, TKey, TResult>(this IDataProvider<TLeft> leftSource,
                                                                                                IDataProvider<TRight> rightSource,
                                                                                                Expression<Func<TLeft, TKey>> leftKeySelector,
                                                                                                Expression<Func<TRight, TKey>> rightKeySelector,
                                                                                                Expression<Func<TLeft, TRight, TResult>> resultColumnsSelector)
        {
            return new Join<TLeft, TRight, TKey, TResult>(leftSource, rightSource, JoinType.Left);
        }

        public static Join<TLeft, TRight, TKey, TResult> RightJoin<TLeft, TRight, TKey, TResult>(this IDataProvider<TLeft> leftSource,
                                                                                                IDataProvider<TRight> rightSource,
                                                                                                Expression<Func<TLeft, TKey>> leftKeySelector,
                                                                                                Expression<Func<TRight, TKey>> rightKeySelector,
                                                                                                Expression<Func<TLeft, TRight, TResult>> resultColumnsSelector)
        {
            return new Join<TLeft, TRight, TKey, TResult>(leftSource, rightSource, JoinType.Right);
        }

        public static IDataProvider<int> Count<T>(this IDataProvider<T> source)
        {
            throw new NotImplementedException();
        }

        public static IDataProvider<int> OrderBy<T>(this IDataProvider<T> source)
        {
            throw new NotImplementedException();
        }

    }
}
