namespace SEA.Data
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Xml;

    using NLog;

    internal class Filler
    {
        protected Logger Log { get; private set; }

        protected void AddAttrs(DataTable Table, IEnumerable<string> AttrNames, IEnumerable<string> Values)
        {
            DataRow row = Table.NewRow();

            try
            {
                using (IEnumerator<string> nameEnum = AttrNames.GetEnumerator(), valueEnum = Values.GetEnumerator())
                {
                    while (nameEnum.MoveNext() & valueEnum.MoveNext())
                    {
                        string name = nameEnum.Current;
                        string value = valueEnum.Current;

                        if (GlobalDef.AttrToColumnNameDict.ContainsKey(name))
                        {
                            row[GlobalDef.AttrToColumnNameDict[name]] = ProcessAttr(name, value);
                        }
                    }
                }

                Table.Rows.Add(row);
            }
            catch (ArgumentException exc)
            {
                Log.Warn(exc, Table.TableName);
            }
        }

        protected virtual object ProcessAttr(string Name, string Value)
        {
            return Value;
        }

        public Filler()
        {
            Log = LogManager.GetLogger(GetType().FullName);
        }

        public static Filler GetFillerByTableName(string Name)
        {
            if (Name == null)
            {
                throw new ArgumentNullException(nameof(Name));
            }

            switch (Name)
            {
                case "Posts":
                    {
                        return new PostsFiller();
                    }
                case "Users":
                    {
                        return new UsersFiller();
                    }
                case "PostLinks":
                    {
                        return new PostLinksFiller();
                    }
                default:
                    {
                        return new Filler();
                    }
            }
        }

        public void FillFromXml(string PathToFile, DataTable Table)
        {
            if (Table == null)
            {
                throw new ArgumentNullException(nameof(Table));
            }

            Uri xmlUri = new Uri(PathToFile);

            List<string> attrNames = new List<string>();
            List<string> values = new List<string>();

            Table.BeginLoadData();

            using (XmlReader reader = XmlReader.Create(xmlUri.AbsoluteUri))
            {
                while (reader.Read())
                {
                    if (reader.MoveToContent() == XmlNodeType.Element)
                    {
                        if (reader.Name.Equals("row", StringComparison.OrdinalIgnoreCase))
                        {
                            if (reader.HasAttributes)
                            {
                                while (reader.MoveToNextAttribute())
                                {
                                    attrNames.Add(reader.Name);
                                    values.Add(reader.Value);
                                }

                                AddAttrs(Table, attrNames, values);

                                attrNames.Clear();
                                values.Clear();
                            }
                        }
                    }
                }
            }

            Table.EndLoadData();
        }
    }
}