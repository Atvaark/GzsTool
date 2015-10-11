using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using GzsTool.Common;
using GzsTool.Common.Interfaces;
using GzsTool.Fpk;
using GzsTool.Pftxs;
using GzsTool.Qar;
using GzsTool.Sbp;
using GzsTool.Utility;

namespace GzsTool
{
    public static class Program
    {
        private static readonly XmlSerializer ArchiveSerializer = new XmlSerializer(
            typeof(ArchiveFile),
            new[] { typeof(FpkFile), typeof(PftxsFile), typeof(QarFile), typeof(SbpFile) });

        private static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                ReadDictionaries();
                string path = args[0];
                if (File.Exists(path))
                {
                    string extension = Path.GetExtension(path);
                    switch (extension)
                    {
                        case ".dat":
                            ReadQarFile(path);
                            return;
                        case ".fpk":
                        case ".fpkd":
                            ReadFpkArchive(path);
                            return;
                        case ".pftxs":
                            ReadPftxsArchive(path);
                            return;
                        case ".sbp":
                            ReadSbpArchive(path);
                            return;
                        case ".xml":
                            WriteArchive(path);
                            return;
                    }
                }
                else if (Directory.Exists(path))
                {
                    ReadFpkArchives(path);
                    return;
                }
            }
            ShowUsageInfo();
        }

        public static void ReadDictionaries()
        {
            string executingAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            const string gzsDictionaryName = "qar_dictionary.txt";
            const string fpkDictionaryName = "fpk_dictionary.txt";
            try
            {
                Console.WriteLine("Reading {0}", gzsDictionaryName);
                Hashing.ReadDictionary(Path.Combine(executingAssemblyLocation, gzsDictionaryName));
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading {0}: {1}", gzsDictionaryName, e.Message);
            }
            try
            {
                Console.WriteLine("Reading {0}", fpkDictionaryName);
                Hashing.ReadMd5Dictionary(Path.Combine(executingAssemblyLocation, fpkDictionaryName));
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading {0}: {1}", fpkDictionaryName, e.Message);
            }
        }

        private static void ShowUsageInfo()
        {
            Console.WriteLine("GzsTool by Atvaark\n" +
                              "  A tool for unpacking and repacking qar, fpk, fpkd, pftxs and sbp files\n" +
                              "Usage:\n" +
                              "  GzsTool file_path|folder_path\n" +
                              "Examples:\n" +
                              "  GzsTool file_path.dat      - Unpacks the qar file\n" +
                              "  GzsTool file_path.fpk      - Unpacks the fpk file\n" +
                              "  GzsTool file_path.fpkd     - Unpacks the fpkd file\n" +
                              "  GzsTool file_path.pftxs    - Unpacks the pftxs file\n" +
                              "  GzsTool file_path.sbp      - Unpacks the sbp file\n" +
                              "  GzsTool folder_path        - Unpacks all fpk and fpkd files in the folder\n" +
                              "  GzsTool file_path.dat.xml  - Repacks the qar file\n" +
                              "  GzsTool file_path.fpk.xml  - Repacks the fpk file\n" +
                              "  GzsTool file_path.fpkd.xml - Repacks the fpkd file\n" +
                              "  GzsTool file_path.pftxs.xml- Repacks the pftxs file");
        }

        private static void ReadQarFile(string path)
        {
            string fileDirectory = Path.GetDirectoryName(path);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string outputDirectoryPath = Path.Combine(fileDirectory, fileNameWithoutExtension);
            string xmlOutputPath = Path.Combine(fileDirectory,
                string.Format("{0}.xml", Path.GetFileName(path)));
            IDirectory outputDirectory = new FileSystemDirectory(outputDirectoryPath);

            using (FileStream input = new FileStream(path, FileMode.Open))
            using (FileStream xmlOutput = new FileStream(xmlOutputPath, FileMode.Create))
            {
                QarFile qarFile = QarFile.ReadQarFile(input);
                qarFile.Name = Path.GetFileName(path);
                foreach (var exportedFile in qarFile.ExportFiles(input))
                {
                    Console.WriteLine(exportedFile.FileName);
                    outputDirectory.WriteFile(exportedFile.FileName, exportedFile.DataStream);
                }
                ArchiveSerializer.Serialize(xmlOutput, qarFile);
            }
        }

        private static void ReadFpkArchives(string path)
        {
            var extensions = new List<string>
            {
                ".fpk",
                ".fpkd"
            };
            var files = GetFilesWithExtension(new DirectoryInfo(path), extensions);
            foreach (var file in files)
            {
                ReadFpkArchive(file.FullName);
            }
        }

        private static void ReadFpkArchive(string path)
        {
            string fileDirectory = Path.GetDirectoryName(path);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path).Replace(".", "");
            string outputDirectoryPath = string.Format("{0}\\{1}_{2}", fileDirectory, fileNameWithoutExtension, extension);
            string xmlOutputPath = Path.Combine(fileDirectory,
                string.Format("{0}.xml", Path.GetFileName(path)));
            IDirectory outputDirectory = new FileSystemDirectory(outputDirectoryPath);

            using (FileStream input = new FileStream(path, FileMode.Open))
            using (FileStream xmlOutput = new FileStream(xmlOutputPath, FileMode.Create))
            {
                FpkFile fpkFile = FpkFile.ReadFpkFile(input);
                fpkFile.Name = Path.GetFileName(path);
                foreach (var exportedFile in fpkFile.ExportFiles(input))
                {
                    Console.WriteLine(exportedFile.FileName);
                    outputDirectory.WriteFile(exportedFile.FileName, exportedFile.DataStream);
                }
                ArchiveSerializer.Serialize(xmlOutput, fpkFile);
            }
        }

        private static void ReadPftxsArchive(string path)
        {
            string fileDirectory = Path.GetDirectoryName(path);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string outputDirectoryPath = string.Format("{0}\\{1}_pftxs", fileDirectory, fileNameWithoutExtension);
            string xmlOutputPath = Path.Combine(fileDirectory,
                string.Format("{0}.xml", Path.GetFileName(path)));
            IDirectory outputDirectory = new FileSystemDirectory(outputDirectoryPath);

            using (FileStream input = new FileStream(path, FileMode.Open))
            using (FileStream xmlOutput = new FileStream(xmlOutputPath, FileMode.Create))
            {
                PftxsFile pftxsFile = PftxsFile.ReadPftxsFile(input);
                pftxsFile.Name = Path.GetFileName(path);
                foreach (var exportedFile in pftxsFile.ExportFiles(input))
                {
                    Console.WriteLine(exportedFile.FileName);
                    outputDirectory.WriteFile(exportedFile.FileName, exportedFile.DataStream);
                }
                ArchiveSerializer.Serialize(xmlOutput, pftxsFile);
            }
        }

        private static void ReadSbpArchive(string path)
        {
            string fileDirectory = Path.GetDirectoryName(path);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string outputDirectoryPath = string.Format("{0}\\{1}_sbp", fileDirectory, fileNameWithoutExtension);
            string xmlOutputPath = Path.Combine(fileDirectory,
                string.Format("{0}.xml", Path.GetFileName(path)));
            IDirectory outputDirectory = new FileSystemDirectory(outputDirectoryPath);

            using (FileStream input = new FileStream(path, FileMode.Open))
            using (FileStream xmlOutput = new FileStream(xmlOutputPath, FileMode.Create))
            {
                SbpFile sbpFile = SbpFile.ReadSbpFile(input, fileNameWithoutExtension);
                sbpFile.Name = Path.GetFileName(path);
                foreach (var exportedFile in sbpFile.ExportFiles(input))
                {
                    Console.WriteLine(exportedFile.FileName);
                    outputDirectory.WriteFile(exportedFile.FileName, exportedFile.DataStream);
                }

                ArchiveSerializer.Serialize(xmlOutput, sbpFile);
            }
        }

        private static void WriteArchive(string path)
        {
            var directory = Path.GetDirectoryName(path);
            using (FileStream xmlInput = new FileStream(path, FileMode.Open))
            {
                ArchiveFile file = ArchiveSerializer.Deserialize(xmlInput) as ArchiveFile;
                WriteArchive((dynamic)file, directory);
            }
        }

        private static void WriteArchive(ArchiveFile archiveFile, string workingDirectory)
        {
            if (archiveFile == null)
            {
                Console.WriteLine("Error: Unknown archive type");
            }
            else
            {
                Console.WriteLine("Error: Repacking archives of type " + archiveFile.GetType().Name + " is not supported");
            }
        }
        
        private static void WriteArchive(QarFile qarFile, string workingDirectory)
        {
            string outputPath = Path.Combine(workingDirectory, qarFile.Name);
            string fileSystemInputDirectory = Path.Combine(workingDirectory,
                Path.GetFileNameWithoutExtension(qarFile.Name));
            IDirectory inputDirectory = new FileSystemDirectory(fileSystemInputDirectory);


            using (FileStream output = new FileStream(outputPath, FileMode.Create))
            {
                qarFile.Write(output, inputDirectory);
            }
        }

        private static void WriteArchive(FpkFile fpkFile, string workingDirectory)
        {
            string outputPath = Path.Combine(workingDirectory, fpkFile.Name);
            string fileSystemInputDirectory = string.Format("{0}\\{1}_{2}", workingDirectory,
                Path.GetFileNameWithoutExtension(fpkFile.Name), Path.GetExtension(fpkFile.Name).Replace(".", ""));
            IDirectory inputDirectory = new FileSystemDirectory(fileSystemInputDirectory);
            using (FileStream output = new FileStream(outputPath, FileMode.Create))
            {
                fpkFile.Write(output, inputDirectory);
            }
        }

        private static void WriteArchive(PftxsFile pftxsFile, string workingDirectory)
        {
            string outputPath = Path.Combine(workingDirectory, pftxsFile.Name);
            string fileSystemInputDirectory = string.Format("{0}\\{1}_pftxs", workingDirectory,
                Path.GetFileNameWithoutExtension(pftxsFile.Name));
            IDirectory inputDirectory = new FileSystemDirectory(fileSystemInputDirectory);
            using (FileStream output = new FileStream(outputPath, FileMode.Create))
            {
                pftxsFile.Write(output, inputDirectory);
            }
        }

        private static IEnumerable<FileInfo> GetFilesWithExtension(DirectoryInfo fileDirectory,
            ICollection<string> extensions)
        {
            foreach (var file in fileDirectory.GetFiles("*", SearchOption.AllDirectories))
            {
                string extension = Path.GetExtension(file.FullName);
                if (extensions.Contains(extension, StringComparer.InvariantCultureIgnoreCase))
                    yield return file;
            }
        }
    }
}
