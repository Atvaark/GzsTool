using System;
using System.Linq;

namespace GzsTool.Core.Crypto
{
    internal class Encryption
    {
        public static void Decrypt1(byte[] sectionData, uint hashLow, uint version, byte[] dataHash)
        {
            // TODO: Use a ulong array instead.
            uint[] decryptionTable =
            {
                0xBB8ADEDB,
                0x65229958,
                0x08453206,
                0x88121302,
                0x4C344955,
                0x2C02F10C,
                0x4887F823,
                0xF3818583,
                //0x40C90FDB,
                //0x3FC90FDB,
                //0x3F490FDB,
                //0x3EA2F983,
                //0x3C8EFA35,
                //0x42652EE0,
                //0x40C90FDB,
                //0x3FC90FDB,
                //0x3F490FDB,
                //0x3EA2F983,
                //0x3C8EFA35,
                //0x42652EE0
            };

            int blocks = sectionData.Length / sizeof(ulong);
            if (version != 2)
            {
                for (int i = 0; i < blocks; i++)
                {
                    int offset1 = i * sizeof(ulong);
                    int offset2 = i * sizeof(ulong) + sizeof(uint);
                    int index = (int)(2 * ((hashLow + offset1 / 11) % 4));
                    uint u1 = BitConverter.ToUInt32(sectionData, offset1) ^ decryptionTable[index];
                    uint u2 = BitConverter.ToUInt32(sectionData, offset2) ^ decryptionTable[index + 1];
                    Buffer.BlockCopy(BitConverter.GetBytes(u1), 0, sectionData, offset1, sizeof(uint));
                    Buffer.BlockCopy(BitConverter.GetBytes(u2), 0, sectionData, offset2, sizeof(uint));
                }

                int remaining = sectionData.Length % sizeof(ulong);
                for (int i = 0; i < remaining; i++)
                {
                    int offset = blocks * sizeof(long) + i * sizeof(byte);
                    int index = (int)(2 * ((hashLow + (offset - (offset % sizeof(long))) / 11) % 4));
                    int decryptionIndex = offset % sizeof(long);
                    uint xorMask = decryptionIndex < 4 ? decryptionTable[index] : decryptionTable[index + 1];
                    byte xorMaskByte = (byte)((xorMask >> (8 * decryptionIndex)) & 0xff);
                    byte b1 = (byte)(sectionData[offset] ^ xorMaskByte);
                    sectionData[offset] = b1;
                }
            }
            else
            {
                ulong seed = BitConverter.ToUInt64(dataHash, (int)(hashLow % 2) * 8);
                uint seedLow = (uint)seed & 0xFFFFFFFF;
                uint seedHigh = (uint)(seed >> 32);
                for (int i = 0; i < blocks; i++)
                {
                    int offset1 = i * sizeof(ulong);
                    int offset2 = i * sizeof(ulong) + sizeof(uint);
                    int index = 2 * (int)((hashLow + seed + (ulong)(offset1 / 11)) % 4);
                    uint u1 = BitConverter.ToUInt32(sectionData, offset1) ^ decryptionTable[index] ^ seedLow;
                    uint u2 = BitConverter.ToUInt32(sectionData, offset2) ^ decryptionTable[index + 1] ^ seedHigh;
                    Buffer.BlockCopy(BitConverter.GetBytes(u1), 0, sectionData, offset1, sizeof(uint));
                    Buffer.BlockCopy(BitConverter.GetBytes(u2), 0, sectionData, offset2, sizeof(uint));
                }

                int remaining = sectionData.Length % sizeof(ulong);
                for (int i = 0; i < remaining; i++)
                {
                    int offset = blocks * sizeof(long) + i * sizeof(byte);
                    int offsetBlock = offset - (offset % sizeof(long));
                    int index = 2 * (int)((hashLow + seed + (ulong)(offsetBlock / 11)) % 4);
                    int decryptionIndex = offset % sizeof(long);
                    uint xorMask = decryptionIndex < 4 ? decryptionTable[index] : decryptionTable[index + 1];
                    byte xorMaskByte = (byte)((xorMask >> (8 * (decryptionIndex % 4))) & 0xff);
                    uint seedMask = decryptionIndex < 4 ? seedLow : seedHigh;
                    byte seedByte = (byte)((seedMask >> (8 * (decryptionIndex % 4))) & 0xff);
                    sectionData[offset] = (byte)(sectionData[offset] ^ (byte)(xorMaskByte ^ seedByte));
                }
            }
        }

        public static unsafe void Decrypt2(byte[] input, uint key)
        {
            int size = input.Length;
            uint currentKey = key | ((key ^ 25974) << 16);

            byte[] output = input.ToArray();
            fixed (byte* pDestBase = output, pSrcBase = input)
            {
                uint* pDest = (uint*) pDestBase;
                uint* pSrc = (uint*) pSrcBase;
                uint i = 278 * key;
                for (; size >= 64; size -= 64)
                {
                    uint j = 16;
                    do
                    {
                        *pDest = currentKey ^ *pSrc;
                        currentKey = i + 48828125 * currentKey;

                        --j;
                        pDest++;
                        pSrc++;
                    } while (j > 0);
                }

                for (; size >= 16; pSrc += 4)
                {
                    *pDest = currentKey ^ *pSrc;
                    uint v7 = i + 48828125 * currentKey;
                    *(pDest + 1) = v7 ^ *(pSrc + 1);
                    uint v8 = i + 48828125 * v7;
                    *(pDest + 2) = v8 ^ *(pSrc + 2);
                    uint v9 = i + 48828125 * v8;
                    *(pDest + 3) = v9 ^ *(pSrc + 3);

                    currentKey = i + 48828125 * v9;
                    size -= 16;
                    pDest += 4;
                }

                for (; size >= 4; pSrc++)
                {
                    *pDest = currentKey ^ *pSrc;

                    currentKey = i + 48828125 * currentKey;
                    size -= 4;
                    pDest++;
                }
            }

            Buffer.BlockCopy(output, 0, input, 0, input.Length);
        }
    }
}