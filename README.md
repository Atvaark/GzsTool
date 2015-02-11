# GzsTool
C# Fox Engine g0s, fpk, fpkd and pftxs unpacker/repacker
 
Requirements
--------
```
Microsoft .NET Framework 4.5
```
 
Usage
--------
 
```
GzsTool file_path|folder_path
```
 
Examples
--------

Unpacking a g0s file. This will unpack all files to the folder called "file_name" and will create a "file_name.g0s.xml" file.
```
GzsTool file_path.g0s
```
 
Unpacking an fpk/fpkd file. This will unpack all files to the folder called "file_name_fpk/file_name_fpkd" and will create a "file_name.fpk.xml/file_name.fpkd.xml" file.
```
GzsTool file_path.fpk
GzsTool file_path.fpkd
```

Unpacking a pftxs file. This will unpack all files to the folder called "file_name_pftxs" and will create a "file_name.pftxs.xml" file.
```
GzsTool file_path.pftxs
```
 
Unpacking all fpk and fpkd files in a folder. This will unpack all files to their respective folders and create the respective xml files. 
```
GzsTool folder_path
```

Repacking a g0s file. This will create the "file_name.g0s" archive.
```
GzsTool file_path.g0s.xml
```

Repacking an fpk/fpkd file. This will create the "file_name.fpk/file_name.fpkd" archive.
```
GzsTool file_path.fpk.xml
GzsTool file_path.fpkd.xml
```

Repacking a pftxs file. This will create the "file_name.pftxs" archive.
```
GzsTool file_path.pftxs.xml
```

Remarks
--------
* Repacking a g0s file without changes will result in a smaller file. This is due to the tool not reencrypting formerly encrypted files and thereby not requiring to store the decryption keys. 
