### db-by-checksum.json
DBs grouped by <filename, MD5 checksum> then sorted by <expansion, build>.
```csharp
public class ChecksumGroup
{
  public string Name;
  public Dictionary<string, string[]> Builds; // <checksum, builds>
}
```

### db-row-match
(In progress) Comparison of matching rows (at byte level) between two builds. DBs grouped by filename, sorted by <expansion, build>. <Legion only.
```csharp
public class DBDMatchGroup
{
  public string BuildPrev;
  public string BuildCur;
  public int RecordCountPrev;
  public int RecordCountCur;
  public int MatchCount;
  public float MatchCountPercent;
}
```
