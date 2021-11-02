namespace Donatello.Gateway;

using System;
using System.Buffers;

internal static class ExtensionMethods
{
    /// <summary>
    /// Preforms a pseudo-resize of a rented array.
    /// </summary>
    internal static void Resize<T>(this ArrayPool<T> pool, ref T[] array, int newSize)
    {
        T[] newArray = pool.Rent(newSize);
        int itemsToCopy = Math.Min(array.Length, newSize);

        Array.Copy(array, 0, newArray, 0, itemsToCopy);

        pool.Return(array, true);

        array = newArray;
    }
}
