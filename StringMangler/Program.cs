using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using StringMangler.Ops;

namespace StringMangler
{
    /*
     * Exit codes list:
     *      * 0:  Everything went well. Operation completed
     *      * -1: No command-line args, or not enough of them
     *      * -2: Invalid path in one of the parameters
     *      * -3: Source and destination paths are the same
     *      * -4: Invalid regex pattern in the strings_filter param
     *      * -5: Error while performing the operation
     */

    internal static class Program
    {
        /// <summary>
        ///     Opcodes for the various operations that the program can perform.
        /// </summary>
        private enum Operation
        {
            /// <summary>
            ///     An invalid opcode. This means nothing can be done with the provided
            ///     command-line arguments (bad values, not enough data, etc)
            /// </summary>
            Invalid,

            /// <summary>
            ///     Copy the matching strings from a source Android resources folder
            ///     to a destination Android resources folder.
            ///     All missing files and folders will be created. Won't check for
            ///     merge issues such as duplicate values in the destination.
            /// </summary>
            Copy,

            /// <summary>
            ///     Deletes all occurrencies of a string in an Android resources folder.
            /// </summary>
            Delete
        }

#if (DEBUG)
        /// <summary>
        ///     Constant that indicates wether verbose debugging output is needed.
        /// </summary>
        internal const bool VERBOSE = true;
#else
    /// <summary>
    /// Constant that indicates wether verbose debugging output is needed.
    /// </summary>
        internal const bool VERBOSE = false;
#endif

        private static void Main(string[] args)
        {
            // Check arguments validity
            Operation operation = CheckArguments(args);
            if (operation == Operation.Invalid)
            {
                return;
            }

            bool result = false;
            switch (operation)
            {
                case Operation.Copy:
                    result = PerformCopyOp(args);
                    break;
                case Operation.Delete:
                    result = PerformDeleteOp(args);
                    break;
            }

            if (result)
            {
                Console.WriteLine("\nAll done!\n");
            }
        }

        /// <summary>
        ///     Performs a Copy operation.
        /// </summary>
        /// <param name="args">The command-line args for the Copy operation.</param>
        /// <returns>
        ///     Returns <code>true</code> if the operation succeeds,
        ///     <code>false</code> otherwise.
        /// </returns>
        private static bool PerformCopyOp(string[] args)
        {
            // Use the default regex filter of "*" if none is specified
            string regexPattern = args.Length == 4 ? args[3] : null;

            IOp op = new CopyOp(args[1], args[2], regexPattern);

            return op.PerformOp();
        }

        /// <summary>
        ///     Performs a Delete operation.
        /// </summary>
        /// <param name="args">The command-line args for the Delete operation.</param>
        /// <returns>
        ///     Returns <code>true</code> if the operation succeeds,
        ///     <code>false</code> otherwise.
        /// </returns>
        private static bool PerformDeleteOp(string[] args)
        {
            IOp op = new DeleteOp(args[1], args[2]);

            return op.PerformOp();
        }

        /// <summary>
        ///     Checks the command-line arguments to validate the input.
        ///     If the input is not valid, prints a message to the console
        ///     and returns the <see cref="Operation.Invalid" /> opcode.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        /// <returns>
        ///     Returns the kind of operation represented by the
        ///     <paramref name="args" />, if valid. If no valid operation is
        ///     contained in <paramref name="args" />, returns the
        ///     <see cref="Operation.Invalid" /> opcode.
        /// </returns>
        private static Operation CheckArguments(string[] args)
        {
            String errReason = null;
            var op = Operation.Invalid;

            // Check absolute arguments issues
            if (args == null || args.Length < 3)
            {
                errReason = "not enough parameters (" +
                            (args != null ? args.Length.ToString(CultureInfo.InvariantCulture) : "none") + ")";
                Environment.ExitCode = -1;
            }
            else
            {
                // Detect the operation and check the arguments number
                switch (args[0].ToLowerInvariant())
                {
                    case "copy":
                    case "c":
                        op = Operation.Copy;
                        break;
                    case "delete":
                    case "d":
                        op = Operation.Delete;
                        break;
                    default:
                        op = Operation.Invalid;
                        break;
                }

                // Check the op-specific parameters
                switch (op)
                {
                    case Operation.Copy:
                        errReason = ValidateCopyOpArgs(args);
                        if (!String.IsNullOrEmpty(errReason)) op = Operation.Invalid;
                        break;
                    case Operation.Delete:
                        errReason = ValidateDeleteOpArgs(args);
                        if (!String.IsNullOrEmpty(errReason)) op = Operation.Invalid;
                        break;
                }
            }

            // Print an error message if needed
            if (op == Operation.Invalid)
            {
                Console.WriteLine("\nStringMangler" + "\n" +
                                  "-------------\n" +
                                  "Version: " + Assembly.GetEntryAssembly().GetName().Version + "\n\n" +
                                  "Error: " + errReason + "\n\n" +
                                  "  USAGE:\n" +
                                  "   [mono] smangler.exe c[opy] source_dir dest_dir [strings_filter]\n" +
                                  "      > Copies all Android strings resources matching the strings_filter regex\n" +
                                  "        from the source_dir res dir to dest_dir (creates missing files/dirs)\n" +
                                  "   [mono] smangler.exe d[elete] res_dir strings_filter\n" +
                                  "      > Removes all Android strings resources matching the strings_filter regex\n" +
                                  "        from the res_dir folder\n" +
                                  "\nPlease refer to https://github.com/imspa/StringMangler/blob/master/README.md \n" +
                                  "for further informations.\n\n");

                if (VERBOSE)
                {
                    Console.WriteLine("Command line arguments:");
                    int i = 0;
                    foreach (string s in args)
                    {
                        Console.WriteLine("   * Argument {0}: {1}", i, args[i]);
                        i++;
                    }
                }
            }

            return op;
        }

        /// <summary>
        ///     Validates the provided command-line arguments for usage
        ///     with the <see cref="Operation.Delete" /> opcode.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        /// <returns>
        ///     Returns null if the arguments are valid for using
        ///     with the <see cref="Operation.Delete" /> opcode, or a non-
        ///     null error message if they don't validate.
        /// </returns>
        private static string ValidateDeleteOpArgs(string[] args)
        {
            string errReason = null;

            // Validate source directory
            if (!Directory.Exists(args[1]))
            {
                errReason = "the resources directory does not exist";

                Environment.ExitCode = -2;
            }

            // Validate the regex pattern
            if (errReason == null && args[2] != null)
            {
                try
                {
                    var test = new Regex(args[2], RegexOptions.Compiled);
                }
                catch (Exception e)
                {
                    errReason = String.Format("the specified regex pattern is not valid. {0}", e.Message);

                    Environment.ExitCode = -4;
                }
            }

            return errReason;
        }

        /// <summary>
        ///     Validates the provided command-line arguments for usage
        ///     with the <see cref="Operation.Copy" /> opcode.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        /// <returns>
        ///     Returns null if the arguments are valid for using
        ///     with the <see cref="Operation.Copy" /> opcode, or a non-null
        ///     error message if they don't validate.
        /// </returns>
        private static string ValidateCopyOpArgs(string[] args)
        {
            string errReason = null;

            // Validate source and destination directories
            if (!Directory.Exists(args[1]))
            {
                errReason = "the source directory does not exist";

                Environment.ExitCode = -2;
            }

            if (errReason == null && !Directory.Exists(args[2]))
            {
                errReason = "the destination directory does not exist";

                Environment.ExitCode = -2;
            }

            if (errReason == null && Path.GetFullPath(args[1]).Equals(Path.GetFullPath(args[2])))
            {
                errReason = "the origin and destination directories are the same";

                Environment.ExitCode = -3;
            }

            // Validate the regex pattern
            if (errReason == null && args.Length == 4 && args[3] != null)
            {
                try
                {
                    var test = new Regex(args[3], RegexOptions.Compiled);
                }
                catch (Exception e)
                {
                    errReason = String.Format("the specified regex pattern is not valid. {0}", e.Message);

                    Environment.ExitCode = -4;
                }
            }

            return errReason;
        }
    }
}