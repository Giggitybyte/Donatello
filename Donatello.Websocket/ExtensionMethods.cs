using System;
using System.Buffers;

namespace Donatello.Websocket
{
    internal static class ExtensionMethods
    {
        /// <summary>
        /// Preforms a pseudo-resize of a rented array.
        /// </summary>
        internal static void Resize<T>(this ArrayPool<T> pool, ref T[] array, int newSize)
        {
            if (array is null)
                array = pool.Rent(newSize);

            else if (array.Length == newSize)
                return;

            else
            {
                T[] newArray = pool.Rent(newSize);
                int itemsToCopy = Math.Min(array.Length, newSize);

                Array.Copy(array, 0, newArray, 0, itemsToCopy);

                pool.Return(array, true);

                array = newArray;
            }
        }
    }
}
