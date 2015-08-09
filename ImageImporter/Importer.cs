using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ImageImporter
{
    // attempts to import all files from source and save them in destination
    // creates a new folder with today's date in destination
    // consults a hashfile to determine if a file should be copied
    // if the file is copied, it is added to the hash file in memory
    // after all files have been copied, the hash file is written to disk
    class Importer
    {
        private string sourcePath = "";
        private string destinationPath = "";
        private string fullDestinationPathWithDateFolder = "";

        private const string hashFileName = ".imported";
        private ArrayList existingHashes = new ArrayList();

        public void ImportFiles(string source, string destination)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(destination))
            {
                Console.WriteLine("Please supply a valid source and destination");
                return;
            }

            sourcePath = FixUpPaths(source);
            destinationPath = FixUpPaths(destination);
            fullDestinationPathWithDateFolder = destinationPath + DateTime.Now.ToString("yyyy-MM-dd");

            ReadHashFile();

            DateTime start = DateTime.Now;
            Console.WriteLine("Scan started at {0}", start);

            int count = ImportFiles(sourcePath);
            TimeSpan duration = DateTime.Now - start;

            string durationString;
            if (duration.Minutes > 0)
            {
                durationString = string.Format("{0} {1} and {2} {3}", duration.Minutes, (duration.Minutes == 1 ? "minute" : "minutes"), duration.Seconds, (duration.Seconds == 1 ? "second" : "seconds"));
            }
            else
            {
                durationString = string.Format("{0} {1}", duration.Seconds, (duration.Seconds == 1 ? "second" : "seconds"));
            }

            Console.WriteLine("Scan took {0}", durationString);
            WriteHashFile();

            if (count == 0)
            {
                Console.WriteLine("No new images in {0} to import.", source);
            }
            else
            {
                Console.WriteLine("Successfully imported {0} {1}.", count, (count == 1 ? "image" : "images"));
            }
        }

        private string FixUpPaths(string path)
        {
            if (!path.EndsWith("\\"))
            {
                return path + "\\";
            }

            return path;
        }

        private bool FileHashExists(string filePath)
        {
            string fileHash = ComputeHashForFile(filePath);

            if (!existingHashes.Contains(fileHash))
            {
                existingHashes.Add(fileHash);
                return false;
            }

            return true;
        }

        private bool ImportFile(string filePathToImport)
        {

            if (FileHashExists(filePathToImport))
            {
                return false;
            }

            // we are going to import a file!
            // this might be our first, so make sure we have the destinationPath made
            // TODO: might be faster to track whether we created this and not try if we already have
            Directory.CreateDirectory(fullDestinationPathWithDateFolder);

            string[] parts = filePathToImport.Split('\\');
            string fileName = parts[parts.Length - 1];
            string[] fileNameParts = fileName.Split('.');

            // probably a hidden file, ignore it
            if (fileNameParts.Length < 2)
            {
                return false;
            }

            // lets figure out the file name for the destination file
            // if a file name already exists, we'll put in (1) or (N)
            // to uniquify it. We'll need to separate the filename and
            // file extension to be able to do this
            string fileNameNoExtension = "";
            for (int i = 0; i < fileNameParts.Length - 1; i++)
            {
                fileNameNoExtension += fileNameParts[i];
            }

            string fileExtension = fileNameParts[fileNameParts.Length - 1];

            string fullDestinationFilePath = string.Format("{0}\\{1}", fullDestinationPathWithDateFolder, fileName);

            int existingCount = 0;
            while (File.Exists(fullDestinationFilePath))
            {
                existingCount++;
                fullDestinationFilePath = string.Format("{0}\\{1}({2}).{3}", fullDestinationPathWithDateFolder, fileNameNoExtension, existingCount, fileExtension);
            }

            try
            {
                File.Copy(filePathToImport, fullDestinationFilePath);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        // this will recursively call ImportFile on every file in the source as long as it isn't:
        // a) System Volume Information
        // b) a directory we have already imported
        // we detect directories we have already imported by noting the hash of the directory in our hash database
        // Nikon cameras seem to write 200 files into folders that are in ascending order. By reversing the list
        // of directories, and only caring about the first, we are able to cut our search space dramatically
        private int ImportFiles(string dir)
        {
            int count = 0;

            try
            {
                foreach (string f in Directory.GetFiles(dir))
                {
                    if (ImportFile(f))
                    {
                        count++;
                    }
                }

                string[] directories = Directory.GetDirectories(dir);
                Array.Reverse(directories);

                bool firstDir = true;

                foreach (string d in directories)
                {
                    if (d.Contains("System Volume Information"))
                    {
                        continue;
                    }

                    string dirHash = ComputeHashForDirectory(d);

                    if (!existingHashes.Contains(dirHash) || firstDir)
                    {
                        firstDir = false;
                        existingHashes.Add(dirHash);

                        Console.WriteLine("Scanning {0}", d);
                        count += ImportFiles(d);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return count;
        }

        private void WriteHashFile()
        {
            string hashFileText = "";
            foreach (string hash in existingHashes)
            {
                hashFileText += hash + "\n";
            }

            using (FileStream fs = new FileStream(destinationPath + hashFileName, FileMode.Open))
            {
                Byte[] hashFileBytes = new UTF8Encoding(true).GetBytes(hashFileText);
                fs.Write(hashFileBytes, 0, hashFileBytes.Length);
            }
        }

        private void ReadHashFile()
        {
            try
            {
                using (StreamReader sr = File.OpenText(destinationPath + hashFileName))
                {
                    string hash = "";
                    while ((hash = sr.ReadLine()) != null)
                    {
                        existingHashes.Add(hash);
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("No existing imported images found, creating import database...");
                using (FileStream fs = File.Create(destinationPath + hashFileName))
                {
                    FileAttributes attributes = File.GetAttributes(destinationPath + hashFileName) | FileAttributes.Hidden;
                    File.SetAttributes(destinationPath + hashFileName, attributes);
                }
            }
        }

        private Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private string ComputeHashForDirectory(string directory)
        {
            string hashString;
            using (Stream s = GenerateStreamFromString(directory))
            {
                using (SHA1Managed sha1 = new SHA1Managed())
                {
                    byte[] hash = sha1.ComputeHash(s);
                    StringBuilder formatted = new StringBuilder(2 * hash.Length);
                    foreach (byte b in hash)
                    {
                        formatted.AppendFormat("{0:X2}", b);
                    }

                    hashString = formatted.ToString().ToLower();
                }
            }

            return hashString;
        }

        private string ComputeHashForFile(string filePath)
        {
            string hashString;
            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            using (BufferedStream bs = new BufferedStream(fs))
            {
                using (SHA1Managed sha1 = new SHA1Managed())
                {
                    byte[] hash = sha1.ComputeHash(bs);
                    StringBuilder formatted = new StringBuilder(2 * hash.Length);
                    foreach (byte b in hash)
                    {
                        formatted.AppendFormat("{0:X2}", b);
                    }

                    hashString = formatted.ToString().ToLower();
                }
            }

            return hashString;
        }
    }
}
