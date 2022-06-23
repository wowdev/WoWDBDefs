## Updating DBDs with newer builds (short version)
The following is a short step-by-step tutorial on how to update DBDs in a best-case scenario where nothing breaks. If something does, it is likely the pattern or the dumping itself that will need updating.
1. Grab the projects in `code/C#/DBDefsDumper`.
2. Grab the WoW executable you want to generate definitions out of.
3. Make sure the pattern set in DBDefsDumper/PatternBuilder.cs is still accurate for the executable's build.
4. Compile and run `dotnet DBDefsDumper.dll <exepath> <outputdir> (build)`, build is an optional x.x.x.xxxxx formatted build in case auto-detection fails (like for 1.14).
5. Compile and run `dotnet DBDefsMerge.dll <dbddir> <rawdir> <outputdir>`, `dbddir` and `outputdir` can be the same directory (e.g. the definitions directory of this repo).
6. Name any newly added definitions that are still unknown (e.g. columns that start with `Field_x_x_x_xxxxx`).
7. If not already available, open a pull request so everyone else can enjoy the updated definitions as well! 😃

## Updating DBDs with newer builds (long version)
### Dumping DBMeta to DBD
#### What is DBMeta?
Metadata for WoW's database tables (DB2s in modern WoW) containing field types, sizes, flags and more are contained in the WoW executable and are commonly referred to as "DBMeta" or "DB2Meta". 

#### Generating DBD files from DBMeta
The DBDefsDumper project (which can be found in the `code/C#/DBDefsDumper` directory) reads these metadata sections from the executable and generates a human readable DBD formatted file for each DB2 available/used in the client. 

1. Compile the project in `code/C#/DBDefsDumper`.
2. Grab the WoW executable you want to generate definitions out of.
3. Run `dotnet DBDefsDumper.dll <exepath> <outputdir> (build)`, build is an optional x.x.x.xxxxx formatted build in case auto-detection fails (like for 1.14).

These dumps do not contain names (unless the dumper is ran on the extremely rare binaries that contain field names), which is where the merger comes in.

If dumping goes according to plan and the patterns work, you can skip the rest of this chapter.

DBMeta entries are located based on patterns that each DB2Meta entry conforms to. These patterns are defined in PatternBuilder.cs for a specific set of builds/patches. Patterns for older builds might be inaccurate/missing, this is something that needs work so we can support wider ranges of builds. However, most importantly, the currently used pattern at time of writing has been in use since 8.0.1, but this may break at any time, likely during the start of a new expansion/patch testing phase. When it does, someone knowledgable will need to reverse engineer the changes to the DB2Meta structure and create a new pattern for that specific patch (and forward). Historically they add new fields every now and then or shift stuff around a bit, so one might be able to get away by simply diffing the structures for the same DBMeta for a single DB2 between two builds and seeing if anything got obviously shifted around. If the changes are more complex, it will likely need proper analysis/reversing.

### Merging "raw" DBD dumps with existing DBDs
After raw definitions are dumped from DBMeta, it's time to merge these up with existing DBDs. Raw definitions have no names and are only for a single build, while existing DBDs have all kinds of builds and, hopefully, contain named fields. 

To merge raw DBDs with existing DBDs:
1. Compile the project in `code/C#/DBDefsMerge`.
2. Run `dotnet DBDefsMerge.dll <dbddir> <rawdir> <outputdir>`, `dbddir` and `outputdir` can be the same directory (e.g. the definitions directory of this repo).

Merging is primarily done based on layouthash (when available), if not available it'll try builds, if those aren't available it'll try build ranges. The merger -should- work fine when ran between raw DBDs and the DBDs from this repo, but things might get funky when working with older builds/raw definitions, especially if layouthashes aren't available.

### Validating definitions
There is a DBDefsValidator project to check the validity of all DBDs format-wise as well as to check if referenced foreign key definitions are valid. 

To run it:
1. Compile the project in `code/C#/DBDefsValidator`.
2. Run `dotnet DBDefsValidator.dll <definitionsdir> (rewrite when done: bool, default false) (verbose: bool, default true) (rawRepoDir: location of WoWDBDefsRaw repository, default none)`

It will output whether or not each definition is valid. 
Optionally, if ran with `true` as second argument it will rewrite definitions (getting rid of excess whitespace/lines as well reordering definitions properly). This is highly recommended after manually editing definitions.
Setting the third (also optional) argument to `true` will output more verbose output. 
The final optional argument is experimental, but can be the raw definition directory to check for any copy-paste errors (e.g. bitsize being wrong). 

### Defining unknown fields
Unknown fields are named `Field_x_x_x_xxxxx_yyy` where x is the build and y is the field index. Usually the changes are minor with a field either being added, resized or removed. Sometimes Blizzard "reshuffles" many fields at the same time which is more annoying to map. Sometimes it's pretty easy to tell what changed based on the field types/sizes, sometimes you might need to look at the content of fields through one of the available tools to see what went where. For entirely new fields, it might be useful looking at the structures defined in Blizzard's Lua files or other DB2s that changed in the same build or features that Blizzard is currently working on. After defining fields manually, I suggest running the validator with rewrites on to make sure no orphaned column definitions or useless whitespace stays around.
