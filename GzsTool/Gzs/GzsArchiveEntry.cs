using System;
using System.IO;
using System.Text;

namespace GzsTool.Gzs
{
    public class GzsArchiveEntry
    {
        public ulong Hash { get; set; }
        public uint Offset { get; set; }
        public uint Size { get; set; }
        public string FileName { get; set; }

        public static GzsArchiveEntry ReadGzArchiveEntry(Stream input)
        {
            GzsArchiveEntry gzsArchiveEntry = new GzsArchiveEntry();
            gzsArchiveEntry.Read(input);
            return gzsArchiveEntry;
        }

        public void Read(Stream input)
        {
            using (BinaryReader reader = new BinaryReader(input, Encoding.Default, true))
            {
                Hash = reader.ReadUInt64();
                Offset = reader.ReadUInt32();
                Size = reader.ReadUInt32();
            }
        }

        public void ExportFile(Stream input, string outputDirectory)
        {
            var data = ReadFile(input);

            int fileExtensionId = (int) (Hash >> 52 & 0xFFFF);
            FileName = Hashing.GetFileNameFromHash(Hash, fileExtensionId); // TODO: Remove dependendy to Program

            if (FileName.StartsWith("/"))
                FileName = FileName.Substring(1, FileName.Length - 1);
            FileName = FileName.Replace("/", "\\");

            string outputFilePath = Path.Combine(outputDirectory, FileName);
            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
            using (FileStream output = new FileStream(outputFilePath, FileMode.Create))
            {
                output.Write(data, 0, data.Length);
            }
        }

        private byte[] ReadFile(Stream input)
        {
            BinaryReader reader = new BinaryReader(input, Encoding.Default, true);
            uint dataOffset = 16*Offset;
            input.Seek(dataOffset, SeekOrigin.Begin);
            byte[] data = reader.ReadBytes((int) Size);
            data = Encryption.DeEncryptQar(data, Offset);
            const uint keyConstant = 0xA0F8EFE6;
            uint peekData = BitConverter.ToUInt32(data, 0);
            if (peekData == keyConstant)
            {
                uint key = BitConverter.ToUInt32(data, 4);
                Size -= 8;
                byte[] data2 = new byte[data.Length - 8];
                Array.Copy(data, 8, data2, 0, data.Length - 8);
                data = Encryption.DeEncrypt(data2, key);
            }
            return data;
        }
    }
}
