using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GzsTool.Fpk
{
    internal class FpkFile
    {
        private readonly List<FpkEntry> _entries;
        private readonly List<FpkReference> _references;

        public FpkFile()
        {
            _entries = new List<FpkEntry>();
            _references = new List<FpkReference>();
        }

        public ICollection<FpkEntry> Entries
        {
            get { return _entries; }
        }

        public static FpkFile ReadFpkFile(Stream input)
        {
            FpkFile fpkFile = new FpkFile();
            fpkFile.Read(input);
            return fpkFile;
        }

        public void Read(Stream input)
        {
            BinaryReader reader = new BinaryReader(input, Encoding.Default, true);

            uint magicNumber1 = reader.ReadUInt32(); // foxf
            uint magicNumber2 = reader.ReadUInt32(); // pk_x pk_p
            uint magicNumber3 = reader.ReadUInt32(); // 3s__ 63__
            reader.Skip(24);
            uint fileCount = reader.ReadUInt32();
            uint referenceCount = reader.ReadUInt32();
            reader.Skip(4);

            for (int i = 0; i < fileCount; i++)
            {
                _entries.Add(FpkEntry.ReadFpkEntry(input));
            }

            for (int i = 0; i < referenceCount; i++)
            {
                _references.Add(FpkReference.ReadFpkReference(input));
            }
        }
    }
}
