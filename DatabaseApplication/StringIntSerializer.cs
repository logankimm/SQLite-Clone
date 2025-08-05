using DatabaseCore;

namespace DatabaseApplication;

public class StringIntSerializer : ISerializer<Tuple<string, int>>
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
            throw new InvalidOperationException ();
        }
    }
    public byte[] Serialize(Tuple<string, int> value)
    {
        var stringBytes = System.Text.Encoding.UTF8.GetBytes(value.Item1);

        var data = new byte[
            4 +                    // First 4 bytes indicate length of the string
            stringBytes.Length +   // another X bytes of actual string content
            4                      // Ends with 4 bytes int value
        ];

        BufferHelper.WriteBuffer((int)stringBytes.Length, data, 0);
        Buffer.BlockCopy(
            src: stringBytes,
            srcOffset: 0,
            dst: data, dstOffset: 4,
            count: stringBytes.Length
        );
        BufferHelper.WriteBuffer((int)value.Item2, data, 4 + stringBytes.Length);
        
        return data;
    }

    public Tuple<string, int> Deserialize(byte[] buffer, int offset, int length)
    {
        if (length != 4)
        {
            throw new ArgumentException ("Invalid length: " + length);
        }

        return BufferHelper.ReadBufferInt32(buffer, offset);
    }
}