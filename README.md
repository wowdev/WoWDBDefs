# WoWDBDefs 
This repository will have an up to date location for column/field definitions for database files used in World of Warcraft.

Project goals:
- Updated database definitions for all versions of World of Warcraft
- Human readable
- Machine readable

Cool stuff we might end up doing if this gets enough traction:
- Repository will feed automated updates on WoWDev.wiki
- Pull requests are automatically checked for validity

## Format
The format in which these files will be built up [is still up for discussion](https://github.com/Marlamin/WoWDBDefs/issues/1). Current proposal is as follows:

### Column definitions
List of column definitions at the start of the file. This is defined once per column at the start to help keep column names the same across the file. 

Starts with ```COLUMNS```, followed by the following:

Regular: ```type ColName```

Foreign keys: ```type<DBName> ColName```

Localized strings: ```locstring ColName``` (see [this](https://wowdev.wiki/Common_Types#langstringref) and [this](https://wowdev.wiki/Localization) page on Wiki, same as"string" type as of 4.0+ but still localized in locale specific files)

Format currently also supports comments by adding ```// Comment goes here ``` at the end of the column definition line, but this is still up for debate.

Valid types that parsers should support: ```(u)int{8/16/32/64}/string/float/locstring```

### Version definitions

Starts with either ```LAYOUT```, ```BUILD``` or both. 
#### LAYOUT
Line starts with ```LAYOUT``` followed by a list of layouthashes separated by a comma and a space.

#### BUILD
Line starts with ```BUILD``` and can appear multiple times per version definition. It supports ranges, multiple exact builds and single exact builds through the following format:

#### COMMENT
Line starts with ```COMMENT```, only for humans.

##### Ranges
```BUILD 7.2.0.23436-7.2.0.23514```. 

Ranges should be specified per minor version to not conflict with other branches. Example:
```
BUILD 7.2.0.23436-7.2.0.23514
BUILD 7.1.5.23038-7.1.5.23420
BUILD 7.1.0.22578-7.1.0.22996
BUILD 7.0.3.21846-7.0.3.22747
```

##### Multiple exact builds 
```BUILD 0.7.0.3694, 0.7.1.3702, 0.7.6.3712```

##### Single build
```BUILD 0.9.1.3810```

#### Columns
```ColName``` refers to exactly the same name as specified in the column definitions. 

No size (floats, (loc)strings, non-inline IDs): ```ColName```

Size (integers): ```ColName<Size>```

Array: ```ColName[Length]```

Both: ```ColName<Size>[Length]```

With comment (for humans): ```ColName // This is a comment, yo!```

#### Column annotations

```ColName``` can be prefixed with an annotation to indicate that this is a special kind of column in this version.

Annotations start with a ```$``` and end with a ```$```. Currently used annotations:

**id** this column is a primary key. Example: ```$id$ColName```

**relation** this column is stored in the relationship table. Example: ```$relation$ColName```

## File handling
Files will be saved with DBName.dbd filenames. Every file has multiple definitions for each different structure that has been encountered for that file. Version structures are separated by an empty new line. All line endings should be in Unix format (\n).

## Example definition file
You can view a sample definition [here](https://github.com/Marlamin/WoWDBDefs/blob/master/definitions/Map.dbd).

Discussions regarding the format proposal should be done in the relevant issue [here](https://github.com/Marlamin/WoWDBDefs/issues/1).

All feedback is welcome!
