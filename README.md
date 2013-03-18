#StringMangler

**StringMangler** is an utility that performs various Android string resources manipulations.

## Requirements
In order to use **StringMangler**, you need a .Net Framework 2.0 Runtime, or Mono. **StringMangler** should work on Windows, Linux and Mac OS X.

## Usage
StringMangler is a command line tool. It has two commands, **copy** and **delete**.

### Copy
The **copy** command copies all Android strings resources matching a regex from a source <code>res</code> dir to a destination one.
This is the syntax:

    [mono] smangler.exe c[opy] source_dir dest_dir [strings_filter]

* <code>source_dir</code> is the full path of the <code>res</code> directory of the source Android project. It will be scanned for files called <code>strings.xml</code> in folders named <code>values</code>, and <code>values\-[a-z]{2}</code>
* <code>dest_dir</code> is the full path of the <code>res</code> directory of the source Android project. Strings will be added to all <code>strings.xml</code> files within the corresponding <code>values</code> folders (they will be created if they don't exist yet)
* <code>strings_filter</code> is a .Net-flavored regular expression that string names will be matched against. This parameter is **optional**; if you don't specify it, the application will copy *all* strings it finds

### Delete
The **delete** command removes all Android strings resources matching a regex in a source <code>res</code> dir.
This is the syntax:

    [mono] smangler.exe d[elete] res_dir strings_filter

* <code>res_dir</code> is the full path of the <code>res</code> directory of the Android project to work on. It will be scanned for files called <code>strings.xml</code> in folders named <code>values</code>, and <code>values\-[a-z]{2}</code>
* <code>strings_filter</code> is a .Net-flavored regular expression that string names will be matched against.

#### Notes
Please note that the leading <code>[mono]</code> token is only necessary when you are running the application on Mono, for example under Linux or Mac OS X. On Windows, you should already have the Microsoft .Net Framework 2.0 (or later) runtime available on the system.

## Known limitations
At the moment, the application is a "quick&dirty" tool. It doesn't do much error handling for filesystem errors, malformed XML, etc, and it does not check if a string is already present in the destination folder when using *copy*. Also, it only searches for strings into <code>strings.xml</code> files, and only writes them to <code>strings.xml</code> files, without the ability to customize that.

## License
This source code is provided under the BSD 3-clause license:

<pre>
Copyright (c) 2013, i'm Spa
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
* Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright
  notice, this list of conditions and the following disclaimer in the
  documentation and/or other materials provided with the distribution.
* Neither the name of the <organization> nor the
  names of its contributors may be used to endorse or promote products
  derived from this software without specific prior written permission.
  
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL i'm Spa BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
</pre>
