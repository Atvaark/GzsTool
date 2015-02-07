using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GzsTool.Pftxs.Psub;

namespace GzsTool.Pftxs
{
    public class PftxsFile
    {
        private const int HeaderSize = 20;
        private const int PftxMagicNumber = 0x58544650; //PFTX
        private const int MagicNumber2 = 0x3F800000; // float 1
        private const int EndOfPackFileMagicNumber = 0x46504F45; //EOPF
        private readonly List<PftxsFileEntry> _filesEntries;

        public PftxsFile()
        {
            _filesEntries = new List<PftxsFileEntry>();
        }

        public int Size { get; set; }
        public int FileCount { get; set; }
        public int DataOffset { get; set; }

        public IEnumerable<PftxsFileEntry> FilesEntries
        {
            get { return _filesEntries; }
        }

        public static PftxsFile ReadPftxsFile(Stream input)
        {
            PftxsFile pftxsFile = new PftxsFile();
            pftxsFile.Read(input);
            return pftxsFile;
        }

        public void Read(Stream input)
        {
            BinaryReader reader = new BinaryReader(input, Encoding.Default, true);
            int magicNumber1 = reader.ReadInt32();
            int magicNumber2 = reader.ReadInt32();
            Size = reader.ReadInt32();
            FileCount = reader.ReadInt32();
            DataOffset = reader.ReadInt32();
            for (int i = 0; i < FileCount; i++)
            {
                PftxsFileEntry pftxsFileEntry = new PftxsFileEntry();
                pftxsFileEntry.Read(input);
                AddPftxsFileEntry(pftxsFileEntry);
            }
            input.Position = DataOffset;
            foreach (var file in FilesEntries)
            {
                file.Data = reader.ReadBytes(file.FileSize);
                file.PsubFile = PsubFile.ReadPsubFile(input);
            }
            int magicNumber3 = reader.ReadInt32();
        }

        public void AddPftxsFileEntry(PftxsFileEntry pftxsFileEntry)
        {
            _filesEntries.Add(pftxsFileEntry);
        }

        public void Write(Stream output)
        {
            BinaryWriter writer = new BinaryWriter(output, Encoding.Default, true);
            long headerPosition = output.Position;
            output.Position += HeaderSize;
            long fileIndicesHeaderSize = PftxsFileEntry.HeaderSize*FilesEntries.Count();
            output.Position += fileIndicesHeaderSize;
            output.AlignWrite(16, 0xCC);

            foreach (var fileEntry in FilesEntries)
            {
                fileEntry.FileNameOffset = Convert.ToInt32(output.Position);
                fileEntry.WriteFileName(output);
            }
            output.AlignWrite(16, 0xCC);
            DataOffset = Convert.ToInt32(output.Position);
            foreach (var fileEntry in FilesEntries)
            {
                fileEntry.WriteData(output);
                fileEntry.WritePsubFile(output);
            }
            writer.Write(EndOfPackFileMagicNumber);
            output.AlignWrite(2048, 0xCC);
            long endPosition = output.Position;
            Size = Convert.ToInt32(endPosition);
            output.Position = headerPosition;
            writer.Write(PftxMagicNumber);
            writer.Write(MagicNumber2);
            writer.Write(Size);
            writer.Write(FileCount);
            writer.Write(DataOffset);
            foreach (var fileEntry in FilesEntries)
            {
                fileEntry.Write(output);
            }
            output.Position = endPosition;
        }
    }
}
