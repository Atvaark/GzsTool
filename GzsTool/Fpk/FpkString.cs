using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using GzsTool.Utility;

namespace GzsTool.Fpk
{
    public class FpkString
    {
        [XmlAttribute("Value")]
        public string Value { get; set; }

        [XmlIgnore]
        public int StringOffset { get; set; }

        [XmlIgnore]
        public int StringLength { get; set; }

        [XmlIgnore]
        public bool NameFound { get; set; }

        public static FpkString ReadFpkString(Stream input)
        {
            FpkString fpkString = new FpkString();
            fpkString.Read(input);
            return fpkString;
        }

        private void Read(Stream input)
        {
            BinaryReader reader = new BinaryReader(input, Encoding.Default, true);
            StringOffset = reader.ReadInt32();
            reader.Skip(4);
            StringLength = reader.ReadInt32();
            reader.Skip(4);

            long endPosition = input.Position;
            input.Position = StringOffset;
            Value = reader.ReadString(StringLength);
            input.Position = endPosition;
        }

        public override string ToString()
        {
            return Value;
        }

        public bool ResolveString(byte[] md5Hash)
        {
            bool resolved;
            byte[] entryNameHash = Hashing.Md5HashText(Value);

            if (entryNameHash.SequenceEqual(md5Hash) == false)
            {
                string fileName;
                resolved = Hashing.TryGetFileNameFromMd5Hash(md5Hash, Value, out fileName);
                Value = fileName;
            }
            else
            {
                resolved = true;
            }

            NameFound = resolved;
            return resolved;
        }

        public void WriteString(Stream output)
        {
            BinaryWriter writer = new BinaryWriter(output, Encoding.Default, true);
            StringOffset = (int) output.Position;
            StringLength = Value.Length;
            writer.WriteNullTerminatedString(Value);
        }

        public void Write(Stream output)
        {
            BinaryWriter writer = new BinaryWriter(output, Encoding.Default, true);
            writer.Write(StringOffset);
            writer.WriteZeros(4);
            writer.Write(StringLength);
            writer.WriteZeros(4);
        }
    }
}
