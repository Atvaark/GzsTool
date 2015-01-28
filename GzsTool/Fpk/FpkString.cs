using System.IO;
using System.Linq;
using System.Text;
using GzsTool.Utility;

namespace GzsTool.Fpk
{
    public class FpkString
    {
        public string Value { get; set; }
        public byte[] EncryptedValue { get; set; }
        public int StringOffset { get; set; }
        public int StringLength { get; set; }
        public bool ValueResolved { get; set; }

        public bool ValueEncrypted
        {
            get { return EncryptedValue != null; }
        }

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

        public void ResolveString(byte[] md5Hash)
        {
            bool resolved;
            byte[] entryNameHash = Hashing.Md5HashText(Value);

            if (entryNameHash.SequenceEqual(md5Hash) == false)
            {
                EncryptedValue = Encoding.Default.GetBytes(Value);
                string resolvedValue;
                resolved = Hashing.TryGetFileNameFromMd5Hash(md5Hash, Value, out resolvedValue);
                Value = resolvedValue;
            }
            else
            {
                resolved = true;
            }

            ValueResolved = resolved;
        }

        public void WriteString(Stream output)
        {
            BinaryWriter writer = new BinaryWriter(output, Encoding.Default, true);
            StringOffset = (int) output.Position;
            string value = ValueEncrypted ? Encoding.Default.GetString(EncryptedValue) : Value;
            StringLength = value.Length;
            writer.WriteNullTerminatedString(value);
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
