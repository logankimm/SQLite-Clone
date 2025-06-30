using System;

namespace DatabaseCore;

static class TreeHelper
{
    public static int BinarySearchFirst<T>(this List<T> array, T value, IComparer<T> comparer)
    {
        if (comparer == null)
        {
            throw new ArgumentNullException("comparer");
        }

        int res = array.BinarySearch(value, comparer);
        if (res >= 1)
        {
            // manually scan backwards to find the first instance
            int prevIndex = res;
            for (int i = (res - 1); i >= 0; i--)
            {
                // if the index value does not match given value, stop search
                if (comparer.Compare(array[i], value) != 0)
                {
                    break;
                }
                prevIndex = i;
            }
            res = prevIndex;
        }
        return res;
    }
    public static int BinarySearchLast<T>(this List<T> array, T value, IComparer<T> comparer)
    {
        if (comparer == null)
        {
            throw new ArgumentNullException("comparer");
        }

        int res = array.BinarySearch(value, comparer);
        if ((res >= 0) && ((res + 1) < array.Count))
        {
            // manually scan forwards to find the last instance
            int prevIndex = res;
            for (int i = (res + 1); i < array.Count; i++)
            {
                // if the index value does not match given value, stop search
                if (comparer.Compare(array[i], value) != 0)
                {
                    break;
                }
                prevIndex = i;
            }
            res = prevIndex;
        }
        return res;
    }
}