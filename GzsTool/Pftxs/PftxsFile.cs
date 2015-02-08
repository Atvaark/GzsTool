using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using GzsTool.Common;
using GzsTool.Common.Interfaces;
using GzsTool.Pftxs.Psub;

namespace GzsTool.Pftxs
{
    [XmlType("PftxsFile")]
    public class PftxsFile : ArchiveFile
    {
        private const int HeaderSize = 20;
        private const int PftxMagicNumber = 0x58544650; //PFTX
        private const int MagicNumber2 = 0x3F800000; // float 1
        private const int EndOfPackFileMagicNumber = 0x46504F45; //EOPF

        public PftxsFile()
        {
            Entries = new List<PftxsFileEntry>();
        }

        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlArray("Entries")]
        public List<PftxsFileEntry> Entries { get; set; }

        [XmlIgnore]
        public int Size { get; set; }

        [XmlIgnore]
        public int FileCount { get; set; }

        [XmlIgnore]
        public int DataOffset { get; set; }

        public static PftxsFile ReadPftxsFile(Stream input)
        {
            PftxsFile pftxsFile = new PftxsFile();
            pftxsFile.Read(input);
            return pftxsFile;
        }

        public override void Read(Stream input)
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
                Entries.Add(pftxsFileEntry);
            }
            input.Position = DataOffset;
            foreach (var entry in Entries)
            {
                entry.DataOffset = input.Position;
                input.Skip(entry.FileSize);
                entry.PsubFile = PsubFile.ReadPsubFile(input);
            }
            int magicNumber3 = reader.ReadInt32();
        }

        public override IEnumerable<FileDataStreamContainer> ExportFiles(Stream input)
        {
            string fileDirectory = "";
            foreach (var file in Entries)
            {
                var fileNameWithoutExtension = "";
                if (file.FileName.StartsWith("@"))
                {
                    fileNameWithoutExtension = file.FileName.Remove(0, 1);
                }
                else if (file.FileName.StartsWith("/"))
                {
                    fileDirectory = Path.GetDirectoryName(file.FileName.Remove(0, 1));
                    fileNameWithoutExtension = Path.GetFileName(file.FileName);
                }

                string fileName = String.Format("{0}.ftex", fileNameWithoutExtension);
                string relativeFilePath = Path.Combine(fileDirectory, fileName);
                file.FilePath = relativeFilePath;
                FileDataStreamContainer ftexContainer = new FileDataStreamContainer
                {
                    DataStream = new MemoryStream(file.ReadData(input)),
                    FileName = relativeFilePath
                };
                yield return ftexContainer;

                int subFileNumber = 1;
                foreach (var psubFileEntry in file.PsubFile.Entries)
                {
                    string subpFileEntryName = String.Format("{0}.{1}.ftexs", fileNameWithoutExtension, subFileNumber);
                    string relativeSubFilePath = Path.Combine(fileDirectory, subpFileEntryName);
                    psubFileEntry.FilePath = relativeSubFilePath;
                    FileDataStreamContainer ftexsContainer = new FileDataStreamContainer
                    {
                        DataStream = new MemoryStream(psubFileEntry.ReadData(input)),
                        FileName = relativeSubFilePath
                    };
                    yield return ftexsContainer;
                    subFileNumber += 1;
                }
            }
        }

        public override void Write(Stream output, IDirectory inputDirectory)
        {
            UpdateFileNames();
            BinaryWriter writer = new BinaryWriter(output, Encoding.Default, true);
            long headerPosition = output.Position;
            output.Position += HeaderSize;
            long fileIndicesHeaderSize = PftxsFileEntry.HeaderSize*Entries.Count();
            output.Position += fileIndicesHeaderSize;
            output.AlignWrite(16, 0xCC);

            foreach (var fileEntry in Entries)
            {
                fileEntry.FileNameOffset = Convert.ToInt32(output.Position);
                fileEntry.WriteFileName(output);
            }
            output.AlignWrite(16, 0xCC);

            DataOffset = Convert.ToInt32(output.Position);
            foreach (var fileEntry in Entries)
            {
                fileEntry.WriteData(output, inputDirectory);
                fileEntry.WritePsubFile(output, inputDirectory);
            }
            writer.Write(EndOfPackFileMagicNumber);
            output.AlignWrite(2048, 0xCC);
            long endPosition = output.Position;

            Size = Convert.ToInt32(endPosition);
            FileCount = Entries.Count();
            output.Position = headerPosition;
            writer.Write(PftxMagicNumber);
            writer.Write(MagicNumber2);
            writer.Write(Size);
            writer.Write(FileCount);
            writer.Write(DataOffset);
            foreach (var fileEntry in Entries)
            {
                fileEntry.Write(output);
            }
            output.Position = endPosition;
        }

        private void UpdateFileNames()
        {
            string lastDirectory = "";
            foreach (var entry in Entries)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(entry.FilePath);
                string fileDirectory = Path.GetDirectoryName(entry.FilePath);
                if (fileDirectory == lastDirectory)
                {
                    entry.FileName = string.Format("@{0}", fileNameWithoutExtension);
                }
                else
                {
                    entry.FileName = string.Format("\\{0}\\{1}", fileDirectory, fileNameWithoutExtension)
                        .Replace('\\', '/');
                    lastDirectory = fileDirectory;
                }
            }
        }
    }
}
