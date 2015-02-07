using System.IO;
using System.Text;
using GzsTool.Pftxs.Psub;

namespace GzsTool.Pftxs
{
    public class PftxsFileEntry
    {
        public const int HeaderSize = 8;
        public int FileNameOffset { get; set; }
        public int FileSize { get; set; }
        public string FileName { get; set; }
        public byte[] Data { get; set; }
        public PsubFile PsubFile { get; set; }

        public void Read(Stream input)
        {
            BinaryReader reader = new BinaryReader(input, Encoding.Default, true);
            FileNameOffset = reader.ReadInt32();
            FileSize = reader.ReadInt32();

            long position = input.Position;
            input.Position = FileNameOffset;
            FileName = reader.ReadNullTerminatedString();
            input.Position = position;
        }

        public void Write(Stream output)
        {
            BinaryWriter writer = new BinaryWriter(output, Encoding.Default, true);
            writer.Write(FileNameOffset);
            writer.Write(FileSize);
        }

        public void WriteFileName(Stream output)
        {
            BinaryWriter writer = new BinaryWriter(output, Encoding.Default, true);
            writer.WriteNullTerminatedString(FileName);
        }

        public void WriteData(Stream output)
        {
            output.Write(Data, 0, Data.Length);
        }

        public void WritePsubFile(Stream output)
        {
            PsubFile.Write(output);
        }
    }
}
