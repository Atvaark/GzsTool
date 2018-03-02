namespace GzsTool.Core.Crypto
{
    internal class Cryptography
    {
        public const uint Magic1 = 0xA0F8EFE6;
        public const uint Magic2 = 0xE3F8EFE6;

        public static int GetHeaderSize(uint encryption)
        {
            int headerSize = 0;
            switch (encryption)
            {
                case Magic1:
                    headerSize = 8;
                    break;
                case Magic2:
                    headerSize = 16;
                    break;
            }

            return headerSize;
        }
    }
}