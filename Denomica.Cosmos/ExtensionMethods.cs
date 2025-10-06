using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Denomica.Cosmos
{
    /// <summary>
    /// Extension methods for working with data in Cosmos DB.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Converts an <see cref="IAsyncEnumerable{T}"/> to a list asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">The asynchronous sequence to convert. Cannot be <see langword="null"/>.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of elements  from the
        /// source sequence, in the order they were retrieved.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
        public static async Task<IList<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var list = new List<T>();
            await foreach(var item in source)
            {
                list.Add(item);
            }

            return list;
        }

    }
}
