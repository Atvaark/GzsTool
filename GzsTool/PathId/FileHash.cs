using System.IO;
using System.Text;

namespace GzsTool.PathId
{
    internal class FileHash
    {
        public ulong Hash { get; set; }

        public static FileHash ReadFileHash(Stream input)
        {
            FileHash fileHash = new FileHash();
            fileHash.Read(input);
            return fileHash;
        }

        private void Read(Stream input)
        {
            BigEndianBinaryReader reader = new BigEndianBinaryReader(input, Encoding.Default, true);
            Hash = reader.ReadUInt64();
        }
    }
}
