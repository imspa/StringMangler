using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace StringMangler
{
    /// <summary>
    /// A class that provides utilities for OpCodes.
    /// </summary>
    internal static class Utils
    {

        /// <summary>
        /// Saves all modifications to all the files in the specified 
        /// <code>&lt;filePath, XMLDoc&gt;</code> mapping.
        /// </summary>
        /// <param name="filesMap">The <code>&lt;filePath, XMLDoc&gt;</code> 
        /// mapping</param>
        internal static void SaveXmlFiles(SortedDictionary<string, XmlDocument> filesMap)
        {
            foreach (var fileKvp in filesMap)
            {
                Console.WriteLine("\t> Saving destination strings file \"" + fileKvp.Key + "\"...");
                fileKvp.Value.Save(fileKvp.Key);
            }
        }

        /// <summary>
        /// Creates the base XML Document skeleton for an Android resources file.
        /// </summary>
        /// <returns>Returns the newly created XML Document.</returns>
        internal static XmlDocument CreateNewResourcesDoc()
        {
            var doc = new XmlDocument();

            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
            doc.AppendChild(doc.CreateElement("resources"));

            return doc;
        }

        /// <summary>
        ///     Enumerates and loads the <see cref="XmlDocument" /> representation
        ///     of all the <code>strings.xml</code> files in the specified folder.
        /// </summary>
        /// <param name="path">The path of the directory to load stuff from.</param>
        /// <param name="collection">
        ///     The <code>&lt;filePath, XmlDoc&gt;</code> mapping to
        ///     fill in
        /// </param>
        internal static void LoadStringsFilesInDir(string path, SortedDictionary<string, XmlDocument> collection)
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

        /// <summary>
        ///     Enumerates all strings contained in a files list.
        /// </summary>
        /// <param name="files">
        ///     The <code>&lt;filePath, XmlDoc&gt;</code> mapping to
        ///     enumerate the strings for
        /// </param>
        /// <param name="stringsMap">
        ///     The strings <code>&lt;stringName, &lt;filePath&gt;&gt;</code>
        ///     mapping to fill in
        /// </param>
        internal static void EnumerateAllStrings(SortedDictionary<string, XmlDocument> files,
                                                SortedDictionary<string, List<string>> stringsMap)
        {
            foreach (var fileKvp in files)
            {
                if (Program.VERBOSE) Console.WriteLine("\t> Processing file \"" + fileKvp.Key + "\"...");
                EnumerateStringsInFile(fileKvp.Key, fileKvp.Value, stringsMap);
            }
        }

        /// <summary>
        ///     Enumerates all strings contained in an Android resources file.
        /// </summary>
        /// <param name="filePath">The path of the file to enumerate all strings of</param>
        /// <param name="xmlDoc">
        ///     The source file's <see cref="XmlDocument" /> representation
        /// </param>
        /// <param name="stringsMap">
        ///     The strings <code>&lt;stringName, &lt;filePath&gt;&gt;</code>
        ///     mapping to fill in
        /// </param>
        private static void EnumerateStringsInFile(string filePath, XmlDocument xmlDoc,
                                                   SortedDictionary<string, List<string>> stringsMap)
        {
            XmlNodeList stringTags = xmlDoc.GetElementsByTagName("string");
            foreach (XmlNode node in stringTags)
            {
                if (typeof(XmlElement) != node.GetType())
                {
                    continue; // Not an XML Element. We don't care about this
                }

                string stringName = ((XmlElement)node).GetAttribute("name");
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
    }
}
