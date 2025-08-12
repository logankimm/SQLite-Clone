using DatabaseCore;

namespace DatabaseApplication;

public class ChampSerializer
{
    public byte[] Serialize (ChampModel champ)
    {
        var classBytes = System.Text.Encoding.UTF8.GetBytes(champ.Class);
        var nameBytes = System.Text.Encoding.UTF8.GetBytes(champ.Name);
        var champData = new byte[
            16 +                   // 16 bytes for Guidid
            4 +                    // 4 bytes indicate the length of `breed` string
            classBytes.Length +    // n bytes for breed string
            4 +                    // 4 bytes indicate the length of the `name` string
            nameBytes.Length +     // z bytes for name 
            4 +                    // 4 bytes for Set
            4 +                    // 4 bytes indicate length of DNA data
            champ.UnitData.Length  // y bytes of DNA data
        ];

        // Class
        Buffer.BlockCopy (
            src: LittleEndianByteOrder.GetBytes((int)classBytes.Length), 
            srcOffset: 0, 
            dst: champData, 
            dstOffset: 16, 
            count: 4
        );

        Buffer.BlockCopy(
            src: classBytes,
            srcOffset: 0,
            dst: champData,
            dstOffset: 16 + 4,
            count: classBytes.Length
        );

        // Name
        Buffer.BlockCopy (
            src: LittleEndianByteOrder.GetBytes((int)nameBytes.Length), 
            srcOffset: 0, 
            dst: champData, 
            dstOffset: 16 + 4 + classBytes.Length, 
            count: 4
        );

        Buffer.BlockCopy (
            src: nameBytes, 
            srcOffset: 0, 
            dst: champData, 
            dstOffset: 16 + 4 + classBytes.Length + 4, 
            count: nameBytes.Length
        );

        // Set
        Buffer.BlockCopy (
            src: LittleEndianByteOrder.GetBytes((int)champ.Set), 
            srcOffset: 0, 
            dst: champData, 
            dstOffset: 16 + 4 + classBytes.Length + 4 + nameBytes.Length, 
            count: 4
        );

        // Unit data
        Buffer.BlockCopy (
            src: LittleEndianByteOrder.GetBytes(champ.UnitData.Length), 
            srcOffset: 0, 
            dst: champData, 
            dstOffset: 16 + 4 + classBytes.Length + 4 + nameBytes.Length + 4, 
            count: 4
        );

        Buffer.BlockCopy (
            src: champ.UnitData, 
            srcOffset: 0, 
            dst: champData, 
            dstOffset: 16 + 4 + classBytes.Length + 4 + nameBytes.Length + 4 + 4, 
            count: champ.UnitData.Length
        );

        return champData;
    }

    public ChampModel Deserializer(byte[] data)
    {
        var champModel = new ChampModel();

        champModel.Id = BufferHelper.ReadBufferGuid(data, 0);

        // name
        var nameLength = BufferHelper.ReadBufferInt32(data, 16);
        if (nameLength < 0 || nameLength > (16*1024))
        {
            throw new Exception ("Invalid string length: " + nameLength);
        }
        champModel.Name = System.Text.Encoding.UTF8.GetString(data, 16 + 4, nameLength);

        // class
        var classLength = BufferHelper.ReadBufferInt32(data, 16 + 4 + nameLength + 4);
        if (classLength < 0 || classLength > (16*1024))
        {
            throw new Exception ("Invalid string length: " + classLength);
        }
        champModel.Class = System.Text.Encoding.UTF8.GetString(data, 16 + 4 + nameLength + 4, classLength);

        // set
        champModel.Set = BufferHelper.ReadBufferInt32(data, 16 + 4 + nameLength + 4 + classLength);

        // unit data
        var unitDataLength = BufferHelper.ReadBufferInt32(data, 16 + 4 + nameLength + 4 + classLength + 4);
        if (unitDataLength < 0 || unitDataLength > (16*1024))
        {
            throw new Exception ("Invalid string length: " + unitDataLength);
        }
        champModel.UnitData = new byte[unitDataLength];
        Buffer.BlockCopy(
            src: data,
            srcOffset: 16 + 4 + nameLength + 4 + classLength + 4 + 4,
            dst: champModel.UnitData, dstOffset: 0,
            count: champModel.UnitData.Length);

        return champModel;
    }
}