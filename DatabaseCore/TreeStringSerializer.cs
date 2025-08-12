using System;

namespace DatabaseCore;

public class TreeStringSerializer : ISerializer<string>
{
    public bool IsFixedSize
    {
        get
        {
            return false;
        }
    }

    public int Length
    {
        get
        {
            throw new InvalidOperationException();
        }
    }
    public byte[] Serialize(string value)
    {
        return System.Text.Encoding.UTF8.GetBytes(value);
    }

    public string Deserialize(byte[] buffer, int offset, int length)
    {
        return System.Text.Encoding.UTF8.GetString(buffer, offset, length);
    }
}