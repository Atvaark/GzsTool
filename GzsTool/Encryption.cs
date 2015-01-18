using System;

namespace GzsTool
{
    internal static class Encryption
    {
        private static ulong ToULong(uint high, uint low)
        {
            return ((ulong) high << 32) + low;
        }

        public static byte[] DeEncryptQar(byte[] pData, uint offset)
        {
            int blockCount = pData.Length/8;
            uint v5 = 8*((uint) blockCount + 2*offset);
            int low = (int) (101436752*offset + 12679594);
            uint high = 0;
            int bufferOffset = 0;
            for (int i = 0; i < blockCount; i++)
            {
                ulong xorData = (ulong) low - 12679594;
                pData[bufferOffset] ^= (byte) (xorData >> 16);
                xorData = (ulong) low - 6339797;
                pData[bufferOffset + 1] ^= (byte) (xorData >> 16);
                xorData = (ulong) low;
                pData[bufferOffset + 2] ^= (byte) (xorData >> 16);
                xorData = (ulong) low + 6339797;
                pData[bufferOffset + 3] ^= (byte) (xorData >> 16);
                xorData = (ulong) low + 12679594;
                pData[bufferOffset + 4] ^= (byte) (xorData >> 16);
                xorData = (ulong) low + 19019391;
                pData[bufferOffset + 5] ^= (byte) (xorData >> 16);
                xorData = (ulong) low + 25359188;
                pData[bufferOffset + 6] ^= (byte) (xorData >> 16);
                xorData = (ulong) low + 31698985;
                pData[bufferOffset + 7] ^= (byte) (xorData >> 16);
                bufferOffset += 8;

                low += 50718376;
                high = (uint) ((ToULong(high, (uint) low) + 50718376) >> 32);
            }

            int remainingBytes = pData.Length & 7;
            uint v10 = 6339797*v5;
            uint v11 = 0;
            for (int i = 0; i < remainingBytes; i++)
            {
                var pair = ToULong(v11, v10);
                pData[bufferOffset] ^= (byte) (pair >> 16);
                v11 = (uint) ((pair + 6339797) >> 32);
                v10 += 6339797;
                bufferOffset++;
            }
            return pData;
        }

        private static void ReplaceUInt(byte[] destinationArray, int offset, uint value)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, destinationArray, offset, sizeof (uint));
        }

        public static byte[] DeEncrypt(byte[] data, uint key)
        {
            int offset = 0;
            uint i;
            int len = data.Length;
            byte[] result = new byte[len];
            uint v5 = key | ((key ^ 0xFFFFCDEC) << 16);
            for (i = 69069*key; len >= 64; len -= 64)
            {
                int n = 16;
                do
                {
                    uint block = v5 ^ BitConverter.ToUInt32(data, offset);
                    ReplaceUInt(result, offset, block);
                    offset += 4;
                    v5 = 3*(i + 23023*v5);
                } while (n-- != 1);
            }

            for (uint v12; len >= 16; v5 = 3*(i + 23023*v12))
            {
                uint b0 = BitConverter.ToUInt32(data, offset);
                uint b1 = BitConverter.ToUInt32(data, offset + 4);
                uint b2 = BitConverter.ToUInt32(data, offset + 8);
                uint b3 = BitConverter.ToUInt32(data, offset + 12);
                uint v9 = 3*(i + 23023*v5);
                ReplaceUInt(result, offset, v5 ^ b0);
                uint v11 = 3*(i + 23023*v9);
                ReplaceUInt(result, offset + 4, v9 ^ b1);
                ReplaceUInt(result, offset + 8, v11 ^ b2);
                v12 = 3*(i + 23023*v11);
                ReplaceUInt(result, offset + 12, v12 ^ b3);
                len -= 16;
                offset += 16;
            }

            for (; len >= 4; v5 = 3*(i + 23023*v5))
            {
                uint block = v5 ^ BitConverter.ToUInt32(data, offset);
                ReplaceUInt(result, offset, block);
                len -= 4;
                offset += 4;
            }

            return result;
        }
    }
}
