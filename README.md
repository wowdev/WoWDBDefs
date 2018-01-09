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
The format in which these files will be built up is still up for discussion. Current proposal is as follows:

Inline ID: ```<uint32 ID>```

Regular: ```type ColName```

Arrays: ```type[length] ColName```

Foreign keys: ```type<DBName> ColName```

Localized strings: ```locstring ColName``` (see [this](https://wowdev.wiki/Common_Types#langstringref) and [this](https://wowdev.wiki/Localization) page on Wiki, same as"string" type as of 4.0+ but still localized in locale specific files)

Valid types that parsers should support: ```(u)int{8/16/32/64}/string/float/locstring```

## File handling
Files will be saved with DBName.dbd filenames. Every file has multiple definitions for each different structure that has been encountered for that file. Version structures are separated by an empty new line. All line endings should be in Unix format (\n).

## Example definition file
You can view a sample definition [here](https://github.com/Marlamin/WoWDBDefs/blob/master/definitions/Map.dbd).

Discussions regarding the format proposal should be done in the relevant issue [here](https://github.com/Marlamin/WoWDBDefs/issues/1).

All feedback is welcome!
