using System;
using System.IO;
using System.Xml.Serialization;
using GzsTool.Common;

namespace GzsTool.Sbp
{
    [XmlType("Entry", Namespace = "Sbp")]
    public class SbpEntry
    {
        [XmlAttribute("Name")]
        public string FileName { get; set; }

        [XmlIgnore]
        public string Magic { get; set; }

        [XmlIgnore]
        public uint Offset { get; set; }

        [XmlIgnore]
        public int Size { get; set; }
        
        public void Read(BinaryReader reader)
        {
            Magic = reader.ReadString(4); // bnk, stp or sab
            Offset = reader.ReadUInt32();
            Size = reader.ReadInt32();

            FileName = FileName + "." + Magic.TrimEnd('\0');
        }

        public FileDataStreamContainer Export(Stream input)
        {
            FileDataStreamContainer fileDataStreamContainer = new FileDataStreamContainer
            {
                DataStream = ReadDataLazy(input),
                FileName = FileName
            };
            return fileDataStreamContainer;
        }


        private Func<Stream> ReadDataLazy(Stream input)
        {
            return () =>
            {
                lock (input)
                {
                    return ReadData(input);
                }
            };
        }

        private Stream ReadData(Stream input)
        {
            byte[] data = new byte[Size];
            input.Position = Offset;
            input.Read(data, 0, Size);
            return new MemoryStream(data);
        }
    }
}