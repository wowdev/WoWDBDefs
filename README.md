The eventual goal of this repository is to have an up to date location for column/field definitions for database files used in World of Warcraft.

Project goals:
- Updated database definitions for all versions of World of Warcraft
- Human readable
- Machine readable

Cool stuff we might end up doing if this gets enough traction:
- Repository will feed automated updates on WoWDev.wiki
- Pull requests are automatically checked for validity

The format in which these files will be built up is still up for discussion. Current proposal is as follows:

Inline ID: ```<uint32 ID>```

Regular: ```type ColName```

Arrays: ```type[length] ColName```

Foreign keys: ```type<DBName> ColName```

```
<uint32 ID>
string Directory
uint32[2] Flags
float MinimapIconScale
float[2] CorpseCoordinates
string ZoneName
string HordeDescription
string AllianceDescription
string PvpObjective
string PvpDescription
uint16<AreaTable> AreaTableID
int16<LoadingScreens> LoadingScreenID
int16<Map> CorpseMapID
int16 TimeOfDayOverride
int16<Map> ParentMapID
int16<Map> CosmeticParentMapID
int16<WindSettings> WindSettingsID
uint8 InstanceType
uint8 MapType
uint8 ExpansionID
uint8 MaxPlayers
uint8 TimeOffset
```

Files will be saved with DBName.??? (maybe .dbd?) filenames. Every file has multiple definitions for each different structure that has been encountered for that file.  

Discussions regarding the format proposal should be done in the relevant issue [here](https://github.com/Marlamin/WoWDBDefs/issues/1).

All feedback is welcome!
