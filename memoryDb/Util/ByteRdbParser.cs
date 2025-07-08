using System.Text;

namespace RedisServer.Util.Serialization
{
    public static class ByteRdbParser
    {
        public static string ReadStreamId(BinaryReader reader)
        {
            int len = ReadLengthEncodedInt(reader);
            string raw = Encoding.UTF8.GetString(reader.ReadBytes(len));
            return raw;
        }


        public static void SkipLengthPrefixedString(BinaryReader reader)
        {
            int firstByte = reader.ReadByte();
            int typeBits = (firstByte & 0xC0) >> 6;

            if (typeBits == 3)
            {
                int encType = firstByte & 0x3F;

                if (encType <= 13)
                {
                    int size = encType switch
                    {
                        0 => 1, // Encoded as int8
                        1 => 2, // int16
                        2 => 4, // int32
                        _ => 1  // Treat unknowns minimally
                    };
                    reader.BaseStream.Seek(size, SeekOrigin.Current);
                }
                else
                {
                    // Skip 1 byte value if not known
                    reader.BaseStream.Seek(1, SeekOrigin.Current);
                }

                return;
            }

            int length = DecodeLength(reader, firstByte);
            reader.BaseStream.Seek(length, SeekOrigin.Current);
        }



        public static int DecodeLength(BinaryReader reader, int firstByte)
        {
            int typeBits = (firstByte & 0xC0) >> 6;
            switch (typeBits)
            {
                case 0:
                    return firstByte & 0x3F;

                case 1:
                    int secondByte = reader.ReadByte();
                    return ((firstByte & 0x3F) << 8) | secondByte;

                case 2:
                    byte[] bytes = reader.ReadBytes(4);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);
                    uint raw = BitConverter.ToUInt32(bytes, 0);
                    if (raw > int.MaxValue)
                        throw new InvalidDataException($"Length prefix too large: {raw}");
                    return (int)raw;

                default:
                    throw new InvalidDataException($"Invalid length encoding: {typeBits}");
            }
        }

        public static double ReadDouble(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(8);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            return BitConverter.ToDouble(bytes, 0);
        }


        public static string ReadLengthPrefixedString(BinaryReader reader)
        {
            int firstByte = reader.ReadByte();
            int length = DecodeLength(reader, firstByte);
            byte[] bytes = reader.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }

        public static int ReadLengthEncodedInt(BinaryReader reader)
        {
            int firstByte = reader.ReadByte();
            return DecodeLength(reader, firstByte);
        }
    }
}