using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;

namespace StringMangler.Ops
{
    internal class DeleteOp : IOp
    {
        private readonly Regex _regex;
        private readonly string _regexPattern;
        private readonly SortedDictionary<string, XmlDocument> _srcFiles;
        private readonly string _srcPath;

        public DeleteOp(string sourcePath, string regexPattern)
        {
            _regexPattern = regexPattern;
            _regex = (!String.IsNullOrEmpty(regexPattern)
                          ? new Regex(regexPattern, RegexOptions.Compiled)
                          : null);

            _srcPath = sourcePath;
            _srcFiles = new SortedDictionary<string, XmlDocument>();
        }

        public bool PerformOp()
        {
            try
            {
                // Get all strings files in the work directory and load them
                Console.WriteLine("\nLoading strings.xml files in work dir...");
                Utils.LoadStringsFilesInDir(_srcPath, _srcFiles);
                Console.WriteLine("{0} strings file(s) read from work dir", _srcFiles.Count);

                // Copy strings to their correspondent dest file XmlDocs
                Console.WriteLine("\nRemoving strings matching {0} in work dir...", _regexPattern);
                DeleteStrings(_srcFiles, _regex);
                Console.WriteLine("String(s) removed from {0} file(s)", _srcFiles.Count);

                // Save all XmlDocs files
                Console.WriteLine("\nSaving XML files...");
                Utils.SaveXmlFiles(_srcFiles);

                return true;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error! {0}", e.Message);
                Environment.ExitCode = -5;
                return false;
            }
        }

        /// <summary>
        ///     Deletes all strings matching the specified regex in a set of XmlDocuments.
        /// </summary>
        /// <param name="srcDocs">
        ///     The mapping of <code>&lt;filePath, XMLDoc&gt;</code> with
        ///     the <see cref="XmlDocument" />s to remove the strings from.
        /// </param>
        /// <param name="regex">The string name regex</param>
        private void DeleteStrings(IDictionary<string, XmlDocument> srcDocs, Regex regex)
        {
            foreach (var kvp in srcDocs)
            {
                if (Program.VERBOSE)
                    Console.Write("   > Removing matching strings from file \"{0}\"...", kvp.Key);

                XmlDocument doc = kvp.Value;
                var nodesToDeleteList = new SortedList<XmlElement, XmlNode>();

                foreach (XmlNode node in doc.GetElementsByTagName("string"))
                {
                    if (typeof (XmlElement) != node.GetType())
                    {
                        continue;
                    }

                    string name = ((XmlElement) node).GetAttribute("name");
                    if (System.Diagnostics.Debugger.IsAttached) Console.Write("\n      Checking string name {0}", name);

                    if (regex.IsMatch(name))
                    {
                        if (node.ParentNode != null) nodesToDeleteList.Add((XmlElement) node, node.ParentNode);
                    }
                }

                if (Program.VERBOSE)
                {
                    Console.WriteLine("Strings to delete: {0}", nodesToDeleteList.Count);
                }
                else
                {
                    Console.Write("\n");
                }

                // Ugly double-cycle to avoid long and boring refactoring the loop above
                foreach (var nodeKvp in nodesToDeleteList)
                {
                    if (Program.VERBOSE)
                    {
                        Console.WriteLine("      > Removing string {0}...", nodeKvp.Key.GetAttribute("name"));
                    }

                    nodeKvp.Value.RemoveChild(nodeKvp.Key);
                }
            }
        }
    }
}