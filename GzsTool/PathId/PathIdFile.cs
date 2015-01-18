using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GzsTool.PathId
{
    internal class PathIdFile
    {
        private readonly List<string> _fileNames;
        private readonly List<StringIndex> _fileStringIndices;
        private readonly List<FolderInfo> _folderInfos;
        private readonly List<string> _folderNames;
        private readonly List<StringIndex> _folderStringIndices;
        private readonly List<FileHash> _hashes;

        public PathIdFile()
        {
            _hashes = new List<FileHash>();
            _folderInfos = new List<FolderInfo>();
            _folderStringIndices = new List<StringIndex>();
            _fileStringIndices = new List<StringIndex>();
            _folderNames = new List<string>();
            _fileNames = new List<string>();
        }

        public void Read(Stream input)
        {
            BigEndianBinaryReader reader = new BigEndianBinaryReader(input, Encoding.Default, true);
            int unknown1 = reader.ReadInt32();
            int hashCount = reader.ReadInt32();
            int folderCount = reader.ReadInt32();
            int fileCount = reader.ReadInt32();
            int unknown2 = reader.ReadInt32();
            int unknown3 = reader.ReadInt32();
            int unknown4 = reader.ReadInt32();
            int unknown5 = reader.ReadInt32();
            for (int i = 0; i < hashCount; i++)
            {
                _hashes.Add(FileHash.ReadFileHash(input));
            }
            input.AlignRead(16);
            for (int i = 0; i < hashCount; i++)
            {
                _folderInfos.Add(FolderInfo.ReadFolderInfo(input));
            }
            input.AlignRead(16);
            for (int i = 0; i < folderCount; i++)
            {
                _folderStringIndices.Add(StringIndex.ReadStringIndex(input));
            }
            input.AlignRead(16);
            for (int i = 0; i < fileCount; i++)
            {
                _fileStringIndices.Add(StringIndex.ReadStringIndex(input));
            }
            input.AlignRead(16);

            long stringTableOffset = input.Position;
            foreach (var folderStringIndex in _folderStringIndices)
            {
                input.Position = stringTableOffset + folderStringIndex.Offset;
                string folderName = reader.ReadNullTerminatedString();
                _folderNames.Add(folderName);
            }
            foreach (var fileStringIndex in _fileStringIndices)
            {
                input.Position = stringTableOffset + fileStringIndex.Offset;
                string fileName = reader.ReadNullTerminatedString();
                _fileNames.Add(fileName);
            }
        }
    }
}
