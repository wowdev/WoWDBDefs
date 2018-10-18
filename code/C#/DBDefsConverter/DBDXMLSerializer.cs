using DBDefsLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using static DBDefsLib.Structs;

using FieldLookup = System.Collections.Generic.Dictionary<string, System.Reflection.FieldInfo>;

namespace DBDefsConverter
{
    public class DBDXMLSerializer
    {
        private readonly XmlSerializer _serializer;
        private readonly FieldLookup _fieldLookup;

        public DBDXMLSerializer()
        {
            // create serializer 
            _serializer = new XmlSerializer(typeof(SerializableDBDefinition), CreateOverrides());

            // build the fieldinfo lookup for ColumnDefinition
            // uses reflection to accomodate structure changes
            var fields = typeof(ColumnDefinition).GetFields(BindingFlags.Public | BindingFlags.Instance);
            _fieldLookup = fields.ToDictionary(fi => fi.Name, fi => fi, StringComparer.OrdinalIgnoreCase);
        }


        public void Serialize(string filename, DBDefinition definition)
        {
            using (StreamWriter writer = File.CreateText(filename))
                Serialize(writer, definition);
        }

        public void Serialize(TextWriter textWriter, DBDefinition definition)
        {
            var proxy = new SerializableDBDefinition()
            {
                columnDefinitions = new SerializableColumnDefinition(_fieldLookup, definition.columnDefinitions),
                versionDefinitions = definition.versionDefinitions
            };

            _serializer.Serialize(textWriter, proxy);
        }

        public DBDefinition Deserialize(string filename)
        {
            using (var fs = File.OpenRead(filename))
            {
                var proxy = (SerializableDBDefinition)_serializer.Deserialize(fs);

                return new DBDefinition()
                {
                    columnDefinitions = proxy.columnDefinitions,
                    versionDefinitions = proxy.versionDefinitions
                };
            }
        }


        private XmlAttributeOverrides CreateOverrides()
        {
            // lowercase formatting
            var overrides = new XmlAttributeOverrides();
            overrides.Add(typeof(Build), new XmlAttributes() { XmlType = new XmlTypeAttribute("build") });
            overrides.Add(typeof(BuildRange), new XmlAttributes() { XmlType = new XmlTypeAttribute("buildRange") });
            overrides.Add(typeof(Definition), new XmlAttributes() { XmlType = new XmlTypeAttribute("definition") });
            return overrides;
        }
    }

    [Serializable]
    [XmlRoot("DBDefinition")]
    public struct SerializableDBDefinition
    {
        [XmlElement("columnDefinitions")]
        public SerializableColumnDefinition columnDefinitions;
        [XmlElement("versionDefinitions")]
        public VersionDefinitions[] versionDefinitions;
    }

    [Serializable]
    public class SerializableColumnDefinition : Dictionary<string, ColumnDefinition>, IXmlSerializable
    {
        private readonly FieldLookup _fieldLookup;

        /// <summary>
        /// Serialization requirement.
        /// </summary>
        private SerializableColumnDefinition() { }

        public SerializableColumnDefinition(FieldLookup fieldLookup, Dictionary<string, ColumnDefinition> source) : base(source)
        {
            _fieldLookup = fieldLookup;
        }


        #region IXmlSerializable Members
        public System.Xml.Schema.XmlSchema GetSchema() => null;

        public void ReadXml(XmlReader reader)
        {
            var xmlnamespace = new XmlNamespaceManager(new NameTable());

            // skip empty elements
            if (reader.IsEmptyElement)
            {
                reader.Read();
                return;
            }

            while (reader.Read() && reader.NodeType != XmlNodeType.EndElement)
            {
                // get columnName from parent
                string columnName = reader.Name;
                reader.Read();

                // read each element into the object
                // boxing because structs
                object column = new ColumnDefinition();
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    string fieldName = reader.Name;

                    if (_fieldLookup.TryGetValue(fieldName, out FieldInfo field))
                    {
                        // simple validation for value deserialization
                        // DBDWriter shall handle format compliance checks
                        try
                        {
                            object value = reader.ReadElementContentAs(field.FieldType, xmlnamespace);
                            field.SetValue(column, value);
                        }
                        catch (Exception ex)
                        {
                            if (ex is FormatException || ex.InnerException is FormatException)
                                throw new FormatException($"Invalid value for '{field.Name}' in column '{columnName}'.");

                            throw ex;
                        }
                    }
                    else
                    {
                        // skip unknown fields
                        reader.ReadElementContentAsObject();
                    }
                }

                // add to dictionary
                Add(columnName, (ColumnDefinition)column);
            }

            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (string columnName in Keys)
            {
                // parent renamed to columnName
                writer.WriteStartElement(columnName);

                // write element content as all non-null fields
                foreach (var field in _fieldLookup)
                {
                    // currently unused ergo unimplemented
                    if (field.Value.FieldType.IsArray)
                        throw new NotImplementedException($"Attempted to serialize an array for field '{field.Key}'.");

                    dynamic value = field.Value.GetValue(this[columnName]);
                    if (value != null)
                    {
                        writer.WriteStartElement(field.Key);
                        writer.WriteValue(value);
                        writer.WriteEndElement();
                    }
                }

                writer.WriteEndElement();
            }
        }
        #endregion
    }
}
