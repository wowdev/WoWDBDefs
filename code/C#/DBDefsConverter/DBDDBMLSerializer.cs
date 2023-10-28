using System;
using System.Collections.Generic;
using System.IO;
using static DBDefsLib.Structs;

namespace DBDefsConverter;

public class DBDDBMLSerializer
{
    public void Serialize(TextWriter textWriter, DBMLDocument document)
    {
        WriteProjectDefinition(textWriter, document.Project);
        foreach (var table in document.Tables)
        {
            WriteTableDefinition(textWriter, table);
        }
        foreach (var @enum in document.Enums)
        {
            WriteEnumDefinition(textWriter, @enum);
        }
    }
    
    public void Serialize(string filename, DBDefinition definition)
    {
        throw new NotImplementedException();
    }
    
    public DBDefinition Deserialize(string filename)
    {
        throw new NotImplementedException();
    }

    private void WriteProjectDefinition(TextWriter textWriter, DBMLProject project)
    {
        textWriter.WriteLine($"Project {project.Name} {{");
        if (!string.IsNullOrEmpty(project.DatabaseType))
            textWriter.WriteLine($"    database_type: {project.DatabaseType}");
        if (!string.IsNullOrEmpty(project.Note))
            textWriter.WriteLine($"    Note: '{project.Note}'");
        textWriter.WriteLine("}");
    }

    private void WriteTableDefinition(TextWriter textWriter, DBMLTable table)
    {
        var schema = !string.IsNullOrEmpty(table.Schema) ? $"\"{table.Schema}\"." : string.Empty;
        var alias = !string.IsNullOrEmpty(table.Alias) ? $"as {table.Alias} " : string.Empty;
        var headerColor = !string.IsNullOrEmpty(table.Settings.HeaderColor) ? $"[headercolor: {table.Settings.HeaderColor}] " : string.Empty;
        textWriter.WriteLine($"Table {schema}\"{table.Name}\" {alias}{headerColor}{{");
        foreach (var column in table.Columns)
        {
            WriteColumnDefinition(textWriter, column, table);
        }
        if (!string.IsNullOrEmpty(table.Note))
            textWriter.WriteLine($"    Note: '{table.Note}'");
        textWriter.WriteLine("}");
    }

    private void WriteColumnDefinition(TextWriter textWriter, DBMLColumn column, DBMLTable table)
    {
        var options = "";
        string OptionsSeperator()
        {
            return !string.IsNullOrEmpty(options) ? ", " : string.Empty;
        }
        
        var schema = !string.IsNullOrEmpty(table.Schema) ? $"\"{table.Schema}\"." : string.Empty;
        
        textWriter.Write($"    \"{column.Name}\" {column.Type}");

        if (column.Settings.IsPrimaryKey)
            options += "primary key";
        
        if (column.Settings.IsNullable.HasValue)
            if (column.Settings.IsNullable.Value)
                options += $"{OptionsSeperator()}null";
            else
                options += $"{OptionsSeperator()}not null";

        if (column.Settings.IsUnique)
            options += $"{OptionsSeperator()}unique";

        switch (column.Settings.DefaultValueType)
        {
            case DBMLColumnDefaultValueType.Number:
            case DBMLColumnDefaultValueType.Boolean:
                options += $"{OptionsSeperator()}default: {column.Settings.DefaultValue}";
                break;
            case DBMLColumnDefaultValueType.String:
                options += $"{OptionsSeperator()}default: '{column.Settings.DefaultValue}'";
                break;
            case DBMLColumnDefaultValueType.Expression: 
                options += $"{OptionsSeperator()}default: `{column.Settings.DefaultValue}`";
                break;
            case DBMLColumnDefaultValueType.None:
            default:
                break;
        }
        
        switch (column.Settings.RelationshipType)
        {
            case DBMLColumnRelationshipType.OneToOne:
                options += $"{OptionsSeperator()}ref: - {schema}\"{column.Settings.RelationshipTable}\".\"{column.Settings.RelationshipColumn}\"";
                break;
            case DBMLColumnRelationshipType.OneToMany:
                options += $"{OptionsSeperator()}ref: < {schema}\"{column.Settings.RelationshipTable}\".\"{column.Settings.RelationshipColumn}\"";
                break;
            case DBMLColumnRelationshipType.ManyToOne:
                options += $"{OptionsSeperator()}ref: > {schema}\"{column.Settings.RelationshipTable}\".\"{column.Settings.RelationshipColumn}\"";
                break;
            case DBMLColumnRelationshipType.ManyToMany:
                options += $"{OptionsSeperator()}ref: <> {schema}\"{column.Settings.RelationshipTable}\".\"{column.Settings.RelationshipColumn}\"";
                break;
            case DBMLColumnRelationshipType.None:
            default:
                break;
        }
        
        if (!string.IsNullOrEmpty(options))
            textWriter.Write($" [{options}]");
        textWriter.WriteLine();
    }

    private void WriteEnumDefinition(TextWriter textWriter, DBMLEnum @enum)
    {
        textWriter.WriteLine($"enum {@enum.Name} {{");
        foreach (var value in @enum.Values)
        {
            textWriter.WriteLine($"    {value.Value}{(!string.IsNullOrEmpty(value.Note) ? $" [note: '{value.Note}']" : string.Empty)}");
        }
        textWriter.WriteLine("}}");
    }
}

public class DBMLDocument
{
    public DBMLProject Project { get; set; } = new();
    public List<DBMLTable> Tables { get; set; } = new();
    public List<DBMLEnum> Enums { get; set; } = new();
    public List<DBMLTableGroup> TableGroups { get; set; } = new();
}

public class DBMLProject
{
    public string Name { get; set; }
    public string DatabaseType { get; set; }
    public string Note { get; set; }
}

public class DBMLTable
{
    public string Schema { get; set; }
    public string Name { get; set; }
    public string Alias { get; set; }
    public List<DBMLColumn> Columns { get; set; } = new();
    public string Note { get; set; }
    public DBMLTableSettings Settings { get; set; } = new();
}

public class DBMLTableSettings
{
    public string HeaderColor { get; set; } = null;
}

public class DBMLColumn
{
    public string Name { get; set; }
    public string Type { get; set; }
    public DBMLColumnSettings Settings { get; set; } = new();
}

public class DBMLColumnSettings
{
    public string Note { get; set; } = string.Empty;
    public bool IsPrimaryKey { get; set; } = false;
    public bool? IsNullable { get; set; } = null;
    public bool IsUnique { get; set; } = false;
    public string DefaultValue { get; set; } = string.Empty;
    public DBMLColumnDefaultValueType DefaultValueType { get; set; } = DBMLColumnDefaultValueType.None;
    public bool IsIncrement { get; set; } = false;
    public string RelationshipTable { get; set; }
    public string RelationshipColumn { get; set; }
    public DBMLColumnRelationshipType RelationshipType { get; set; }
}

public enum DBMLColumnDefaultValueType
{
    None,
    Number,
    String,
    Expression,
    Boolean
}

public enum DBMLColumnRelationshipType
{
    None,
    OneToMany,
    ManyToOne,
    OneToOne,
    ManyToMany
}

public class DBMLEnum
{
    public string Name { get; set; }
    public List<DBMLEnumValue> Values { get; set; } = new();
}

public class DBMLEnumValue
{
    public string Value { get; set; }
    public string Note { get; set; }
}

public class DBMLTableGroup
{
    public string Name { get; set; }
    public List<DBMLTable> Tables { get; set; } = new();
}