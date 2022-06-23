## Updating DBDs with newer builds
The following is a short step-by-step tutorial on how to update DBDs in a best-case scenario where nothing breaks. If something does, it is likely the pattern or the dumping itself that will need updating.
1. Grab the projects in `code/C#/DBDefsDumper`.
2. Grab the WoW executable you want to generate definitions out of.
3. Make sure the pattern set in DBDefsDumper/PatternBuilder.cs is still accurate for the executable's build.
4. Run `dotnet DBDefsDumper.dll <exepath> <outputdir> (build)`, build is an optional x.x.x.xxxxx formatted build in case auto-detection fails (like for 1.14).
5. Run `dotnet DBDefsMerge.dll <dbddir> <rawdir> <outputdir>`, `dbddir` and `outputdir` can be the same directory (e.g. the definitions directory of this repo).
6. Name any newly added definitions that are still unknown (e.g. columns that start with `Field_x_x_x_xxxxx`).
7. If not already available, open a pull request so everyone else can enjoy the updated definitions as well! 😃
