using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using GzsTool.Pftxs.Psub;

namespace GzsTool.Pftxs
{
    internal static class PftxsUtility
    {
        public static void PackPftxFile(string path)
        {
            string archiveDirectory = Path.GetDirectoryName(path);
            string archiveName = Path.GetFileNameWithoutExtension(path);
            PftxsLogFile logFile = ReadPftxsLogFile(path);
            PftxsFile pftxsFile = ConvertToPftxs(logFile, archiveDirectory);
            using (FileStream output = new FileStream(Path.Combine(archiveDirectory, archiveName), FileMode.Create))
            {
                pftxsFile.Write(output);
            }
        }

        private static PftxsFile ConvertToPftxs(PftxsLogFile logFile, string workingDirectoryPath)
        {
            string archiveInputDirectoryName = string.Format("{0}_pftxs",
                Path.GetFileNameWithoutExtension(logFile.ArchiveName));
            PftxsFile pftxsFile = new PftxsFile();
            string lastDirectory = "";
            foreach (var logEntry in logFile.Entries)
            {
                PftxsFileEntry entry = new PftxsFileEntry();
                string relativePath;
                if (lastDirectory.Equals(logEntry.FileDirectory))
                {
                    entry.FileName = String.Format("@{0}", logEntry.FileName).Replace('\\', '/');
                    relativePath = Path.Combine(archiveInputDirectoryName, lastDirectory, logEntry.FileName);
                }
                else
                {
                    string arelativeFilePath = String.Format("{0}\\{1}", logEntry.FileDirectory,
                        logEntry.FileName);
                    entry.FileName = String.Format("\\{0}", arelativeFilePath).Replace('\\', '/');
                    lastDirectory = logEntry.FileDirectory;
                    relativePath = Path.Combine(archiveInputDirectoryName, arelativeFilePath);
                }
                string relativeFilePath = string.Format("{0}.ftex", relativePath);
                string fullFilePath = Path.Combine(workingDirectoryPath, relativeFilePath);
                entry.Data = File.ReadAllBytes(fullFilePath);
                Console.WriteLine(relativeFilePath);
                entry.FileSize = entry.Data.Length;

                PsubFile psubFile = new PsubFile();
                for (int i = 1; i <= logEntry.SubFileCount; i++)
                {
                    string relativeSubFilePath = String.Format("{0}.{1}.ftexs", relativePath, i);
                    string fullSubFilePath = Path.Combine(workingDirectoryPath, relativeSubFilePath);
                    var psubFileData = File.ReadAllBytes(fullSubFilePath);
                    Console.WriteLine(relativeSubFilePath);
                    PsubFileEntry psubFileEntry = new PsubFileEntry
                    {
                        Data = psubFileData,
                        Size = psubFileData.Length
                    };
                    psubFile.Entries.Add(psubFileEntry);
                }
                entry.PsubFile = psubFile;
                pftxsFile.Entries.Add(entry);
            }
            pftxsFile.FileCount = pftxsFile.Entries.Count();
            return pftxsFile;
        }

        private static PftxsLogFile ReadPftxsLogFile(string logFilePath)
        {
            PftxsLogFile logFile;
            using (FileStream xmlInput = new FileStream(logFilePath, FileMode.Open))
            {
                var xmlSerializer = new XmlSerializer(typeof (PftxsLogFile));
                logFile = xmlSerializer.Deserialize(xmlInput) as PftxsLogFile;
            }
            return logFile;
        }
    }
}
