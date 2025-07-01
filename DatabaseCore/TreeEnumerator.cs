using System;
using System.Collections.Generic;
using System.Collections;

namespace DatabaseCore;

public class TreeEnumerator<K, V> : IEnumerator<Tuple<K, V>>
{
    bool doneIterating = false;

    public bool MoveNext()
    {
        if (doneIterating)
        {
            return false;
        }
        return true;
    }
}