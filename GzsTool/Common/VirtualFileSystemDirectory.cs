using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GzsTool.Common.Interfaces;

namespace GzsTool.Common
{
    internal class VirtualFileSystemDirectory : IDirectory
    {
        public const string DirectorySeparator = "\\";
        private readonly List<IDirectory> _directories;
        private readonly List<IFile> _files;
        private readonly string _name;

        public VirtualFileSystemDirectory(string name)
        {
            _name = name;
            _files = new List<IFile>();
            _directories = new List<IDirectory>();
        }

        public string Name
        {
            get { return _name; }
        }

        public byte[] ReadFile(string filePath)
        {
            int index = filePath.IndexOf(DirectorySeparator, StringComparison.Ordinal);
            if (index == -1)
            {
                return _files.Single(f => f.Name == filePath).Content;
            }
            string subDirectory = filePath.Substring(0, index);
            string subDirectoryFilePath = filePath.Substring(index + DirectorySeparator.Length,
                filePath.Length - index - DirectorySeparator.Length);
            return _directories.Single(d => d.Name == subDirectory).ReadFile(subDirectoryFilePath);
        }

        public void WriteFile(string filePath, Stream fileContentStream)
        {
            int index = filePath.IndexOf(DirectorySeparator, StringComparison.Ordinal);
            if (index == -1)
            {
                VirtualFileSystemFile file = new VirtualFileSystemFile(filePath, fileContentStream.ToArray());
                AddFile(file);
                return;
            }
            string subDirectory = filePath.Substring(0, index);
            string subDirectoryFilePath = filePath.Substring(index + DirectorySeparator.Length,
                filePath.Length - index - DirectorySeparator.Length);
            IDirectory existingSubDirectory = _directories.SingleOrDefault(d => d.Name == subDirectory);
            if (existingSubDirectory == null)
            {
                existingSubDirectory = new VirtualFileSystemDirectory(subDirectory);
                _directories.Add(existingSubDirectory);
            }
            existingSubDirectory.WriteFile(subDirectoryFilePath, fileContentStream);
        }

        public IEnumerable<IFileSystemEntry> Entries
        {
            get
            {
                foreach (var directory in _directories)
                {
                    yield return directory;
                }
                foreach (var file in _files)
                {
                    yield return file;
                }
            }
        }

        public void AddDirectory(VirtualFileSystemDirectory directory)
        {
            _directories.Add(directory);
        }

        public void AddFile(VirtualFileSystemFile file)
        {
            _files.Add(file);
        }
    }
}
