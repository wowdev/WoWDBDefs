# DBD Meta format
## Purpose
As per the discussion [here](https://github.com/wowdev/WoWDBDefs/discussions/207) we've wanted to have field metadata information inside the DBD format for ages, but instead of modifying the DBD format to allow for this, we're adding an extra set of formats on top of it.

## Mapping file
The mapping file (extra/mapping.dbdm) defines what columns are flags, enums or colors, 1 per line. Preferably they also name the flag/enum which points to a file of the same name containing definitions. Additionally, a third qualifier can be given that makes it conditional based on another field value.
### Format
#### Flags
`FLAGS TableName::ColumnName (Name) (ConditionTable::ConditionColumn=ConditionalValue)`
Indicates that a column should be treated as a flags field. Both `Name` and `Condition` are optional. If `Name` exists, also check for a file containing definitions at `meta/flags/<Name>.dbdf`. If condition is set, these flags only apply to the specified field if that condition is met.
#### Enums
`ENUM TableName::ColumnName (Name) (ConditionTable::ConditionColumn=ConditionalValue)`
Indicates that a column should be treated as an enum field. Both `Name` and `Condition` are optional. If `Name` exists, also check for a file containing definitions at `meta/enums/<Name>.dbde`. If condition is set, these enums only apply to the specified field if that condition is met.
#### Colors
`COLOR TableName::ColumnName`
Indicates that a column should be treated as a color field.

## DBDF (DBD Flags) & DBDE (DBD Enum) formats
### Difference
The only difference between these files is that enum files (DBDE) use numerical/decimal values to refer to map enums. Flag files (DBDF) use hexadecimal.
### Format
`Value Name // Comment`
This is very similar to DBD column definitions, but instead of a field type it has a decimal (enums) or hexadecimal (flags) value. Another difference is that only `Value` is required as there can be unnamed flags.
Just like DBD column definitions, `Name` can not contain spaces but can be followed by a `?` for unverified/placeholder names. It can also be followed by a comment starting after the name at `//`. Given both `Name` and `Comment` are optional, it is possible for a comment to start immediately after a name.
#### Versioning
TODO