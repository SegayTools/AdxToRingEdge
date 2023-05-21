using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TouchPanelJsonGenerator
{
    internal static class LinqExtensionMethod
    {
        /// <summary>
        /// Distinct continuous same values.
        /// example : 1,2,3,3,4,1,1,2,3 -> 1,2,3,4,1,2,3
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="compFunc"></param>
        /// <returns></returns>
        public static IEnumerable<T> DistinctContinuousBy<T, Y>(this IEnumerable<T> collection, Func<T, Y> keySelect) => collection.DistinctContinuousBy((a, b) => keySelect(a)?.Equals(keySelect(b)) ?? keySelect(b)?.Equals(keySelect(a)) ?? true);

        /// <summary>
        /// Distinct continuous same values.
        /// example : 1,2,3,3,4,1,1,2,3 -> 1,2,3,4,1,2,3
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="compFunc"></param>
        /// <returns></returns>
        public static IEnumerable<T> DistinctContinuousBy<T>(this IEnumerable<T> collection, Func<T, T, bool> compFunc)
        {
            var itor = collection.GetEnumerator();
            bool isFirst = true;
            var prev = default(T);

            while (itor.MoveNext())
            {
                var value = itor.Current;
                if (isFirst)
                {
                    yield return value;
                    isFirst = false;
                }
                else if (!compFunc(prev, value))
                    yield return value;
                prev = value;
            }
        }
    }
}
