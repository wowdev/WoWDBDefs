# WoWDBDefs
This repository has up to date column/field definitions for database files used in World of Warcraft.

Features:
- Updated definitions for all World of Warcraft builds between 7.3.5.26654 and current
- New builds added soon after release (as long as there are no major DBC format changes)
- Human readable
- Machine readable ([C#](https://github.com/wowdev/WoWDBDefs/tree/master/code/C%23) and [Python](https://github.com/wowdev/WoWDBDefs/tree/master/code/Python3) code available as well as [a tool to convert DBD to JSON/XML](https://github.com/wowdev/WoWDBDefs/tree/master/code/C%23/DBDefsConverter))

Project goals:
- Updated database definitions for all versions of World of Warcraft (work ongoing, some already available)

Cool stuff we might end up doing if this gets enough traction:
- Repository will feed automated updates on WoWDev.wiki
- Pull requests are automatically checked for validity
- More? Open an issue if you have any ideas

## DBD Format
If you have any suggestions for changes or additions to the format, feel free to open an issue. The DBD format is currently specified as follows:

### Column definitions
List of column definitions at the start of the file. This is defined once per column at the start to help keep column names the same across the file.

Starts with ```COLUMNS```, followed by the following:

Regular: ```type ColName```

Foreign keys: ```type<ForeignDB::ForeignCol> ColName```

Localized strings: ```locstring ColName``` (see [this](https://wowdev.wiki/Common_Types#langstringref) and [this](https://wowdev.wiki/Localization) page on Wiki, same as "string" type as of 4.0+ but still localized in locale specific files)

You can also add a comment to a column definition by adding ```// Comment goes here ``` at the end of the line.

Valid types that parsers should support: ```int/string/float/locstring```

Unverified columns (guessed, etc) have a ```?``` at the end of ```ColName```.

### Version definitions

```BUILD``` is required. ```LAYOUT``` is required for versions that have it. Can be both.

#### LAYOUT
Line starts with ```LAYOUT``` followed by a list of layouthashes separated by a comma and a space. Can appear only once.

#### BUILD
Line starts with ```BUILD``` followed by a range, multiple exact builds separated by a comma and a space or a single exact build. Can appear multiple times.

#### COMMENT
Line starts with ```COMMENT```, only for humans. Can appear only once.

##### Ranges
```BUILD 7.2.0.23436-7.2.0.23514```.

Ranges for current expansions should be specified per minor version to not conflict with other branches. Example:
```
BUILD 7.2.0.23436-7.2.0.23514
BUILD 7.1.5.23038-7.1.5.23420
BUILD 7.1.0.22578-7.1.0.22996
BUILD 7.0.3.21846-7.0.3.22747
```

As no more builds/branch conflicts are expected for anything older than the current expansion, ranges are allowed to span a full expansion. Example:
```
BUILD 4.0.0.11792-4.3.4.15595
BUILD 3.0.1.8622-3.3.5.12340
```

When using ranges, please confirm that the range is correct by verifying the version definition for all public builds included in it.

##### Multiple exact builds
```BUILD 0.7.0.3694, 0.7.1.3702, 0.7.6.3712```

##### Single exact build
```BUILD 0.9.1.3810```

#### Columns
```ColName``` refers to exactly the same name as specified in the column definitions.

No size (floats, (loc)strings, non-inline IDs): ```ColName```

Size (8, 16, 32 or 64, prefixed by ```u``` if unsigned int): ```ColName<Size>```

Array: ```ColName[Length]```

Both: ```ColName<Size>[Length]```

With comment (for humans): ```ColName // This is a comment, yo!```

#### Column annotations

```ColName``` can be prefixed with annotations to indicate that this is a special kind of column in this version.

Annotations start with a ```$``` and end with a ```$``` and are comma separated when there's more than one. Current annotations:

**id** this column is a primary key. Example: (inline) ```$id$ColName``` (non-inline) ```$noninline,id$```

**relation** this column is a relationship. Has ```noninline``` when stored in relationship table. Examples: (inline) ```$relation$ColName``` (non-inline) ```$noninline,relation$```

**noninline** this column is **non-inline** (currently only used for  ```$id$``` and ```$relation$```). See non-inline examples above.

## File handling
Files will be saved with DBName.dbd filenames where DBName is the exact name of the DBC/DB2. Every file has multiple definitions for each different structure that has been encountered for that file. Version structures are separated by an empty new line. All line endings should be in Unix format (\n).

## Example definition file
You can view a sample definition [here](https://github.com/wowdev/WoWDBDefs/blob/master/definitions/Map.dbd).

All feedback is welcome!
