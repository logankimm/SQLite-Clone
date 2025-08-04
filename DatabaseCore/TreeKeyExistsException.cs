using System;
using log4net;
using System.Collections.Generic;
using System.Numerics;

namespace DatabaseCore;

public class TreeKeyExistsException : Exception
{
    public TreeKeyExistsException (object key) : base ("Duplicate key: " + key.ToString())
    {
        
    }
}