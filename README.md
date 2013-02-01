#StringMangler

**StringMangler** is an utility that copies all Android strings resources matching a regex from a source <code>res</code> dir to a destination one.

## Requirements
In order to use **StringMangler**, you need a .Net Framework 2.0 Runtime, or Mono. **StringMangler** should work on Windows, Linux and Mac OS X.

## Usage
StringMangler is a command line tool. This is the syntax:

    [mono] smangler.exe source_dir dest_dir [strings_filter]

* <code>source_dir</code> is the full path of the <code>res</code> directory of the source Android project. It will be scanned for files called <code>strings.xml</code> in folders named <code>values</code>, and <code>values\-[a-z]{2}</code>
* <code>dest_dir</code> is the full path of the <code>res</code> directory of the source Android project. Strings will be added to all <code>strings.xml</code> files within the corresponding <code>values</code> folders (they will be created if they don't exist yet)
* <code>strings_filter</code> is a .Net-flavored regular expression that string names will be matched against. This parameter is **optional**; if you don't specify it, the application will copy *all* strings it finds

Please note that the leading <code>[mono]</code> token is only necessary when you are running the application on Mono, for example under Linux or Mac OS X. On Windows, you should already have the Microsoft .Net Framework 2.0 (or later) runtime available on the system.

## Known limitations
At the moment, the application is a "quick&dirty" tool. It doesn't do much error handling for filesystem errors, malformed XML, etc, and it does not check if a string is already present in the destination folder. Also, it only searches for strings into <code>strings.xml</code> files, and only writes them to <code>strings.xml</code> files, without the ability to customize that.