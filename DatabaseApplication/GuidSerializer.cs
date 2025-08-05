using DatabaseCore;

namespace DatabaseApplication;

public class GuidSerializer : ISerializer<Guid>
{
    public bool IsFixedSize
    {
        get
        {
            return true;
        }
    }

    public int Length
    {
        get
        {
            return 16;
        }
    }
    public byte[] Serialize(Guid value)
    {
        return value.ToByteArray();
    }

    public Guid Deserialize(byte[] buffer, int offset, int length)
    {
        if (length != 16)
        {
            throw new ArgumentException ("Invalid length: " + length);
        }

        return BufferHelper.ReadBufferGuid(buffer, offset);
    }
}