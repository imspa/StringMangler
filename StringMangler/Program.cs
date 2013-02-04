using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace StringMangler
{
    internal static class Program
    {
        private static SortedDictionary<string, XmlDocument> _originFiles, _destFiles;

#if (DEBUG)
        private const bool VERBOSE = true;
#else
        private const bool VERBOSE = false;
#endif

        private static void Main(string[] args)
        {
            // Check arguments validity
            if (args == null || (args.Length != 2 && args.Length != 3))
            {
                Console.WriteLine("\nStringMangler" + "\n" +
                                  "---------------\n\n" +
                                  "Error: incorrect number of parameters (" + args.Length + ")\n\n" +
                                  "\tUSAGE: smangler.exe source_dir dest_dir [strings_filter]\n" +
                                  "\nPlease refer to https://github.com/imspa/StringMangler/blob/master/README.md \n" +
                                  "for further informations.\n\n");

                if (VERBOSE)
                {
                    Console.WriteLine("Command line arguments:");
                    int i = 0;
                    foreach (var s in args)
                    {
                        Console.WriteLine("Argument {0}: {1}", i, args[i]);
                        i++;
                    }
                }

                Environment.ExitCode = -1;
                return;
            }

            // Validate source and destination directories
            if (!Directory.Exists(args[0]))
            {
                Console.WriteLine("The source directory does not exist");

                Environment.ExitCode = -2;
                return;
            }

            if (!Directory.Exists(args[1]))
            {
                Console.WriteLine("The destination directory does not exist");

                Environment.ExitCode = -3;
                return;
            }

            if (Path.GetFullPath(args[0]).Equals(Path.GetFullPath(args[1])))
            {
                Console.WriteLine("The origin and destination directories are the same");

                Environment.ExitCode = -4;
                return;
            }

            // Use the default regex filter of "*" if none is specified
            string regexPattern = args.Length == 3 ? args[2] : null;

            // Validate the Regex
            Regex regex = null;
            if (regexPattern != null)
            {
                try
                {
                    regex = new Regex(regexPattern, RegexOptions.Compiled);
                }
                catch (Exception e)
                {
                    Console.WriteLine("The specified regex pattern is not valid. {0}", e.Message);

                    Environment.ExitCode = -5;
                    return;
                }
            }

            _originFiles = new SortedDictionary<string, XmlDocument>();
            _destFiles = new SortedDictionary<string, XmlDocument>();

            // Get all strings files in the origin directory and load them
            Console.WriteLine("\nLoading strings.xml files in origin dir...");
            LoadStringsFilesInDir(args[0], _originFiles);
            Console.WriteLine("{0} strings file(s) read from origin dir", _originFiles.Count);

            // Get all strings files in the destination directory and load them
            Console.WriteLine("\nLoading strings.xml files in destination dir...");
            LoadStringsFilesInDir(args[1], _destFiles);
            Console.WriteLine("{0} strings file(s) read from destination dir", _destFiles.Count);

            // Enumerate all strings in origin dir
            Console.WriteLine("\nEnumerating strings in origin dir...");
            var originStrings = new SortedDictionary<string, List<string>>();
            EnumerateAllStrings(_originFiles, originStrings);
            Console.WriteLine("{0} strings(s) found in origin dir", originStrings.Count);

            // Copy strings to their correspondent dest file XmlDocs
            Console.WriteLine("\nCopying strings matching {0} from origin dir to dest dirs...", args[2]);
            CopyStrings(originStrings, args[1], regex);
            Console.WriteLine("{0} string(s) copied", originStrings.Count);

            // Save all XmlDocs files
            Console.WriteLine("\nSaving destination XML files...");
            SaveXmlFiles(_destFiles);

            Console.WriteLine("\nAll done!\n");
        }

        private static void SaveXmlFiles(SortedDictionary<string, XmlDocument> filesMap)
        {
            foreach (var fileKvp in filesMap)
            {
                Console.WriteLine("\t> Saving destination strings file \"" + fileKvp.Key + "\"...");
                fileKvp.Value.Save(fileKvp.Key);
            }
        }

        private static void CopyStrings(SortedDictionary<string, List<string>> stringsMap, string destRootDir,
                                        Regex nameRegex)
        {
            foreach (var stringKvp in stringsMap)
            {
                if (nameRegex.IsMatch(stringKvp.Key))
                {
                    Console.WriteLine("Copying string \"" + stringKvp.Key + "\" to destination folder...");
                    CopyString(stringKvp.Key, stringKvp.Value, destRootDir);
                }
            }
        }

        private static void CopyString(string stringName, List<string> originFiles, string destRootDir)
        {
            foreach (string file in originFiles)
            {
                DirectoryInfo directory = new FileInfo(file).Directory;

                if (directory == null)
                {
                    Console.Error.WriteLine("Unable to get the directory for file \"" + file + "\", skipping it");
                    continue;
                }

                Console.WriteLine("\t> Copying string \"{0}\" from directory \"{1}\"", stringName, directory.Name);

                // Retrieve the corresponding destination file
                XmlDocument destXmlDoc = GetDestDocForOriginFile(directory, destRootDir);

                // Find string value in the original file
                string stringValue = GetOriginalStringValue(stringName, file); // QUI
                if (stringValue == null)
                {
                    continue; // Dafuq
                }

                // Add a new node to the destination XmlDocument
                var rootNode = (XmlElement) destXmlDoc.GetElementsByTagName("resources").Item(0);
                XmlElement stringNode = destXmlDoc.CreateElement("string");
                stringNode.SetAttribute("name", stringName);
                stringNode.InnerText = stringValue;
                if (rootNode != null) rootNode.AppendChild(stringNode);
            }
        }

        private static string GetOriginalStringValue(string stringName, string origFile)
        {
            XmlDocument origXmlDoc = _originFiles[origFile];

            XmlNodeList strings = origXmlDoc.GetElementsByTagName("string");

            foreach (object node in strings)
            {
                if (typeof (XmlElement) == node.GetType())
                {
                    var stringNode = (XmlElement) node;

                    if (VERBOSE) Console.WriteLine("\t\t> Parsing string \"{0}\"", stringNode.GetAttribute("name"));

                    if (stringName.Equals(stringNode.GetAttribute("name")))
                    {
                        if (VERBOSE) Console.WriteLine("\t\t\t*** FOUND ***");
                        return stringNode.InnerText;
                    }
                }
            }
            return null;
        }

        private static XmlDocument GetDestDocForOriginFile(DirectoryInfo directory, string destRootDir)
        {
            string destDirPath = Path.Combine(destRootDir, directory.Name);
            if (!Directory.Exists(destDirPath))
            {
                // Ensure the directory exists
                Directory.CreateDirectory(destDirPath);
            }
            string destFilePath = Path.Combine(destDirPath, "strings.xml");

            if (!_destFiles.ContainsKey(destFilePath))
            {
                // Create the new file, which doesn't exist yet   
                _destFiles.Add(destFilePath, CreateNewResourcesDoc());
            }

            return _destFiles[destFilePath];
        }

        private static XmlDocument CreateNewResourcesDoc()
        {
            var doc = new XmlDocument();

            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
            doc.AppendChild(doc.CreateElement("resources"));

            return doc;
        }

        private static void EnumerateAllStrings(SortedDictionary<string, XmlDocument> files,
                                                SortedDictionary<string, List<string>> stringsMap)
        {
            foreach (var fileKvp in files)
            {
                Console.WriteLine("\t> Processing file \"" + fileKvp.Key + "\"...");
                EnumerateStringsInFile(fileKvp.Key, fileKvp.Value, stringsMap);
            }
        }

        private static void EnumerateStringsInFile(string filePath, XmlDocument xmlDoc,
                                                   SortedDictionary<string, List<string>> stringsMap)
        {
            XmlNodeList stringTags = xmlDoc.GetElementsByTagName("string");
            foreach (XmlNode node in stringTags)
            {
                if (typeof (XmlElement) != node.GetType())
                {
                    continue; // Not an XML Element. We don't care about this
                }

                string stringName = ((XmlElement) node).GetAttribute("name");
                List<string> stringFiles;

                if (stringsMap.ContainsKey(stringName))
                {
                    // We already found this string before
                    stringFiles = stringsMap[stringName];

                    if (stringFiles == null)
                    {
                        stringFiles = new List<string>();
                        stringsMap[stringName] = stringFiles;
                    }
                }
                else
                {
                    // New string (not found before): add the KVP to the map
                    stringFiles = new List<string>();
                    stringsMap.Add(stringName, stringFiles);
                }

                // Add this file to the string files list (the list of files the string is found within)
                stringFiles.Add(filePath);
            }
        }


        private static void LoadStringsFilesInDir(string path, SortedDictionary<string, XmlDocument> collection)
        {
            var originDir = new DirectoryInfo(path);

            if (!originDir.Exists)
            {
                throw new DirectoryNotFoundException("The directory \"" + path + "\" does not exist.");
            }

            FileInfo[] originStringFiles = originDir.GetFiles("strings.xml", SearchOption.AllDirectories);

            foreach (FileInfo stringFile in originStringFiles)
            {
                if (!stringFile.Exists ||
                    (stringFile.Directory != null && !stringFile.Directory.Name.StartsWith("values")))
                {
                    continue; // We only want (real) string files!
                }

                Console.WriteLine("\t> Loading file \"" + stringFile.FullName + "\"...");

                var tmpXmlDoc = new XmlDocument();
                tmpXmlDoc.Load(stringFile.FullName);

                collection.Add(stringFile.FullName, tmpXmlDoc);
            }
        }
    }
}