namespace SEA.Data
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Text;

    using CsvHelper;
    using CsvHelper.Configuration;
    using CsvHelper.TypeConversion;

    internal static class Stat
    {
        private static IEnumerable<object> GetAges(DataSet DataSet)
        {
            DataTable ageTable = DataSet.Tables["Users"];

            var ages = from row in ageTable.AsEnumerable()
                       let age = row.Field<short?>(GlobalDef.AttrToColumnNameDict["Age"])
                       let ageStr = age.HasValue ? age.Value.ToString() : "None"
                       group ageStr by ageStr into ageGroup
                       select new { Age = ageGroup.Key, Count = ageGroup.Count() };

            return ages;
        }

        private static IEnumerable<object> GetCountCrDateQuestionPostsByYearsAndMonths(DataSet DataSet)
        {
            DataTable postsTable = DataSet.Tables["Posts"];

            var crQuestions = from row in postsTable.AsEnumerable()
                              where row.Field<PostType>(GlobalDef.AttrToColumnNameDict["PostTypeId"]) == PostType.Question
                              group row.Field<DateTime>(GlobalDef.AttrToColumnNameDict["CreationDate"]) by row.Field<DateTime>(GlobalDef.AttrToColumnNameDict["CreationDate"]).Year into groupByYears
                              from groupByMonth in (from groupByYear in groupByYears
                                                    group groupByYear by groupByYear.Month)
                              group groupByMonth by groupByYears.Key into allGroups
                              from rec in allGroups
                              select new { Year = allGroups.Key, Month = rec.Key, Count = rec.Count() };

            return crQuestions;
        }

        private static IEnumerable<object> GetCountUsersRegByYearsAndMonths(DataSet DataSet)
        {
            DataTable usersTable = DataSet.Tables["Users"];

            var regUsers = from row in usersTable.AsEnumerable()
                           group row.Field<DateTime>(GlobalDef.AttrToColumnNameDict["CreationDate"]) by row.Field<DateTime>(GlobalDef.AttrToColumnNameDict["CreationDate"]).Year into groupByYears
                           from groupByMonth in (from groupByYear in groupByYears
                                                 group groupByYear by groupByYear.Month)
                           group groupByMonth by groupByYears.Key into allGroups
                           from rec in allGroups
                           select new { Year = allGroups.Key, Month = rec.Key, Count = rec.Count() };

            return regUsers;
        }

        private static IEnumerable<object> GetNPopTags(DataSet DataSet, int N)
        {
            DataTable tagsTable = DataSet.Tables["Tags"];

            var popTags = tagsTable.AsEnumerable()
                .GroupBy(row => row.Field<uint>(GlobalDef.AttrToColumnNameDict["Count"]))
                .OrderByDescending(group => group.Key)
                .Take(N)
                .SelectMany(group => group.Select(row => new { Tag = row.Field<string>(GlobalDef.AttrToColumnNameDict["TagName"]), Count = group.Key }));

            return popTags;
        }

        private static IEnumerable<object> GetNUnpopTags(DataSet DataSet, int N)
        {
            DataTable tagsTable = DataSet.Tables["Tags"];

            var unpopTags = tagsTable.AsEnumerable()
                .GroupBy(row => row.Field<uint>(GlobalDef.AttrToColumnNameDict["Count"]))
                .OrderBy(group => group.Key)
                .Take(N)
                .SelectMany(group => group.Select(row => new { Tag = row.Field<string>(GlobalDef.AttrToColumnNameDict["TagName"]), Count = group.Key }));

            return unpopTags;
        }

        private static IEnumerable<object> GetNUsersHaveHighestRep(DataSet DataSet, int N)
        {
            Html2Text htmlConv = new Html2Text();

            DataTable usersTable = DataSet.Tables["Users"];

            var users = usersTable.AsEnumerable()
                        .GroupBy(row => row.Field<int>(GlobalDef.AttrToColumnNameDict["Reputation"]))
                        .OrderByDescending(group => group.Key)
                        .Take(N)
                        .SelectMany(group => group.Select(row =>
                            new
                            {
                                Reputation = group.Key,
                                Name = row.Field<string>(GlobalDef.AttrToColumnNameDict["DisplayName"]),
                                Location = row.Field<string>(GlobalDef.AttrToColumnNameDict["Location"]),
                                AboutMe = htmlConv.ToText(row.Field<string>(GlobalDef.AttrToColumnNameDict["AboutMe"])),
                                Age = row.Field<short?>(GlobalDef.AttrToColumnNameDict["Age"])
                            })
                        );

            return users;
        }

        private static void SaveToCSV(string PathToSave, IEnumerable<object> Collection)
        {
            using (CsvWriter writer = new CsvWriter(new StreamWriter(PathToSave, false, Encoding.UTF8)))
            {
                writer.Configuration.TypeConverterCache.AddConverter(typeof(object), new NullValueConverter<object>());
                writer.WriteRecords(Collection);
            }
        }

        public static void CollectStat(object Param)
        {
            (string pathToSave, IEnumerable<DataSet> dataSets) = (ValueTuple<string, IEnumerable<DataSet>>)Param;

            foreach (DataSet dataSet in dataSets)
            {
                string saveDir = Path.Combine(pathToSave, dataSet.DataSetName);

                if (!Directory.Exists(saveDir))
                {
                    Directory.CreateDirectory(saveDir);
                }

                var ages = GetAges(dataSet);

                SaveToCSV(Path.Combine(saveDir, "Ages.csv"), ages);

                var popTags = GetNPopTags(dataSet, 20);

                SaveToCSV(Path.Combine(saveDir, "PopTags.csv"), popTags);

                var unpopTags = GetNUnpopTags(dataSet, 20);

                SaveToCSV(Path.Combine(saveDir, "UnpopTags.csv"), unpopTags);

                var users = GetNUsersHaveHighestRep(dataSet, 20);

                SaveToCSV(Path.Combine(saveDir, "UsersHighestRep.csv"), users);

                var usersReg = GetCountUsersRegByYearsAndMonths(dataSet);

                SaveToCSV(Path.Combine(saveDir, "UsersReg.csv"), usersReg);

                var posts = GetCountCrDateQuestionPostsByYearsAndMonths(dataSet);

                SaveToCSV(Path.Combine(saveDir, "PostsCrDate.csv"), posts);
            }
        }
    }

    internal class NullValueConverter<T> : DefaultTypeConverter
    {
        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            if (value == null)
            {
                return "None";
            }

            var converter = row.Configuration.TypeConverterCache.GetConverter<T>();

            return converter.ConvertToString(value, row, memberMapData);
        }
    }
}