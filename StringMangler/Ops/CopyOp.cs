using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace StringMangler.Ops
{
    internal class CopyOp : IOp
    {
        private readonly SortedDictionary<string, XmlDocument> _destFiles;
        private readonly string _destPath;
        private readonly Regex _regex;
        private readonly string _regexPattern;
        private readonly SortedDictionary<string, XmlDocument> _srcFiles;
        private readonly string _srcPath;

        public CopyOp(string sourcePath, string destPath, string regexPattern)
        {
            _regexPattern = regexPattern;
            _regex = (!String.IsNullOrEmpty(regexPattern)
                          ? new Regex(regexPattern, RegexOptions.Compiled)
                          : null);

            _srcPath = sourcePath;
            _destPath = destPath;

            _srcFiles = new SortedDictionary<string, XmlDocument>();
            _destFiles = new SortedDictionary<string, XmlDocument>();
        }

        public bool PerformOp()
        {
            try
            {
                // Get all strings files in the source directory and load them
                Console.WriteLine("\nLoading strings.xml files in source dir...");
                Utils.LoadStringsFilesInDir(_srcPath, _srcFiles);
                Console.WriteLine("{0} strings file(s) read from source dir", _srcFiles.Count);

                // Get all strings files in the destination directory and load them
                Console.WriteLine("\nLoading strings.xml files in destination dir...");
                Utils.LoadStringsFilesInDir(_destPath, _destFiles);
                Console.WriteLine("{0} strings file(s) read from destination dir", _destFiles.Count);

                // Enumerate all strings in source dir
                Console.WriteLine("\nEnumerating strings in source dir...");
                var srcStrings = new SortedDictionary<string, List<string>>();
                Utils.EnumerateAllStrings(_srcFiles, srcStrings);
                Console.WriteLine("{0} strings(s) found in source dir", srcStrings.Count);

                // Copy strings to their correspondent dest file XmlDocs
                Console.WriteLine("\nCopying strings matching {0} from source dir to dest dirs...", _regexPattern);
                CopyStrings(srcStrings, _destPath, _regex);
                Console.WriteLine("{0} string(s) copied", srcStrings.Count);

                // Save all XmlDocs files
                Console.WriteLine("\nSaving destination XML files...");
                Utils.SaveXmlFiles(_destFiles);

                return true;
            }
            catch (Exception)
            {
                Environment.ExitCode = -5;
                return false;
            }
        }

        /// <summary>
        ///     Copies all the string from a source <code>&lt;stringName, &lt;filePaths&gt;&gt;</code>
        ///     mapping that match the specified regex to the correponding destination directory.
        /// </summary>
        /// <param name="stringsMap">
        ///     The <code>&lt;stringName, &lt;filePaths&gt;&gt;</code> source
        ///     mapping
        /// </param>
        /// <param name="destPath">The path of the destination resources directory</param>
        /// <param name="nameRegex">The string name regex</param>
        private void CopyStrings(SortedDictionary<string, List<string>> stringsMap, string destPath,
                                 Regex nameRegex)
        {
            foreach (var stringKvp in stringsMap)
            {
                if (nameRegex == null || nameRegex.IsMatch(stringKvp.Key))
                {
                    if (Program.VERBOSE)
                        Console.WriteLine("Copying string \"" + stringKvp.Key + "\" to destination folder...");
                    CopyString(stringKvp.Key, stringKvp.Value, destPath);
                }
            }
        }

        /// <summary>
        ///     Copies a single string resource from the specified files to the destination
        ///     directory, creating the missing files and folders as needed.
        /// </summary>
        /// <param name="stringName">The name of the string to copy</param>
        /// <param name="sourceFiles">A list of source files paths to copy the string from</param>
        /// <param name="destPath">The path of the destination resources directory</param>
        private void CopyString(string stringName, List<string> sourceFiles, string destPath)
        {
            foreach (string file in sourceFiles)
            {
                DirectoryInfo directory = new FileInfo(file).Directory;

                if (directory == null)
                {
                    Console.Error.WriteLine("Unable to get the directory for file \"" + file + "\", skipping it");
                    continue;
                }

                if (Program.VERBOSE)
                    Console.WriteLine("\t> Copying string \"{0}\" from directory \"{1}\"", stringName, directory.Name);

                // Retrieve the corresponding destination file
                XmlDocument destXmlDoc = GetDestXmlDocForSrcFile(directory, destPath);

                // Find string value in the source file
                string stringValue = GetStringValueInFile(stringName, file); // QUI
                if (stringValue == null)
                {
                    continue; // Dafuq -- shouldn't ever get here
                }

                // Add a new node to the destination XmlDocument
                var rootNode = (XmlElement) destXmlDoc.GetElementsByTagName("resources").Item(0);
                XmlElement stringNode = destXmlDoc.CreateElement("string");
                stringNode.SetAttribute("name", stringName);
                stringNode.InnerText = stringValue;
                if (rootNode != null) rootNode.AppendChild(stringNode);
            }
        }

        /// <summary>
        ///     Gets the specified string value from a file.
        /// </summary>
        /// <param name="stringName">The string name</param>
        /// <param name="filePath">The path of the file to get the value from</param>
        /// <returns></returns>
        private string GetStringValueInFile(string stringName, string filePath)
        {
            XmlDocument origXmlDoc = _srcFiles[filePath];

            XmlNodeList strings = origXmlDoc.GetElementsByTagName("string");

            foreach (object node in strings)
            {
                if (typeof (XmlElement) == node.GetType())
                {
                    var stringNode = (XmlElement) node;

                    if (Program.VERBOSE)
                        Console.WriteLine("\t\t> Parsing string \"{0}\"", stringNode.GetAttribute("name"));

                    if (stringName.Equals(stringNode.GetAttribute("name")))
                    {
                        if (Program.VERBOSE) Console.WriteLine("\t\t\t*** FOUND ***");
                        return stringNode.InnerText;
                    }
                }
            }
            return null;
        }

        /// <summary>
        ///     Get the XML Document from the destination directory that corresponds
        ///     to the specified source file (indicated by its directory)
        /// </summary>
        /// <param name="directory">
        ///     The Directory containing the source
        ///     <code>strings.xml</code> file
        /// </param>
        /// <param name="destPath">The path of the destination resources directory</param>
        /// <returns>Returns the destination XML Document, creating it if necessary.</returns>
        private XmlDocument GetDestXmlDocForSrcFile(DirectoryInfo directory, string destPath)
        {
            string destDirPath = Path.Combine(destPath, directory.Name);
            if (!Directory.Exists(destDirPath))
            {
                // Ensure the directory exists
                Directory.CreateDirectory(destDirPath);
            }
            string destFilePath = Path.Combine(destDirPath, "strings.xml");

            if (!_destFiles.ContainsKey(destFilePath))
            {
                // Create the new file, which doesn't exist yet   
                _destFiles.Add(destFilePath, Utils.CreateNewResourcesDoc());
            }

            return _destFiles[destFilePath];
        }
    }
}