namespace SEA.Data
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Text;

    using CsvHelper;

    internal static class Stat
    {
        private static IEnumerable<short?> GetAges(DataSet DataSet)
        {
            DataTable ageTable = DataSet.Tables["Users"];

            var ages = from row in ageTable.AsEnumerable()
                       select row.Field<short?>(GlobalDef.AttrToColumnNameDict["Age"]);

            return ages;
        }

        private static IEnumerable<(int Year, int Month, int Count)> GetCountCrDateQuestionPostsByYearsAndMonths(DataSet DataSet)
        {
            DataTable postsTable = DataSet.Tables["Posts"];

            var crQuestions = from row in postsTable.AsEnumerable()
                              where row.Field<PostType>(GlobalDef.AttrToColumnNameDict["PostTypeId"]) == PostType.Question
                              group row.Field<DateTime>(GlobalDef.AttrToColumnNameDict["CreationDate"]) by row.Field<DateTime>(GlobalDef.AttrToColumnNameDict["CreationDate"]).Year into groupByYears
                              from groupByMonth in (from groupByYear in groupByYears
                                                    group groupByYear by groupByYear.Month)
                              group groupByMonth by groupByYears.Key into allGroups
                              from rec in allGroups
                              // Год, месяц, количество регистраций.
                              select (allGroups.Key, rec.Key, rec.Count());

            return crQuestions;
        }

        private static IEnumerable<(int Year, int Month, int Count)> GetCountUsersRegByYearsAndMonths(DataSet DataSet)
        {
            DataTable usersTable = DataSet.Tables["Users"];

            var regUsers = from row in usersTable.AsEnumerable()
                           group row.Field<DateTime>(GlobalDef.AttrToColumnNameDict["CreationDate"]) by row.Field<DateTime>(GlobalDef.AttrToColumnNameDict["CreationDate"]).Year into groupByYears
                           from groupByMonth in (from groupByYear in groupByYears
                                                 group groupByYear by groupByYear.Month)
                           group groupByMonth by groupByYears.Key into allGroups
                           from rec in allGroups
                           // Год, месяц, количество регистраций.
                           select (allGroups.Key, rec.Key, rec.Count());

            return regUsers;
        }

        private static IEnumerable<(string, uint)> GetNPopTags(DataSet DataSet, int N)
        {
            DataTable tagsTable = DataSet.Tables["Tags"];

            var popTags = from row in tagsTable.AsEnumerable()
                          orderby row.Field<uint>(GlobalDef.AttrToColumnNameDict["Count"]) descending
                          group row.Field<string>(GlobalDef.AttrToColumnNameDict["TagName"]) by row.Field<uint>(GlobalDef.AttrToColumnNameDict["Count"]) into countTagsGroup
                          from tag in countTagsGroup
                          select (tag, countTagsGroup.Key);

            return popTags.Take(N);
        }

        private static IEnumerable<(string, uint)> GetNUnpopTags(DataSet DataSet, int N)
        {
            DataTable unpopTags = DataSet.Tables["Tags"];

            var query = from row in unpopTags.AsEnumerable()
                        orderby row.Field<uint>(GlobalDef.AttrToColumnNameDict["Count"]) ascending
                        group row.Field<string>(GlobalDef.AttrToColumnNameDict["TagName"]) by row.Field<uint>(GlobalDef.AttrToColumnNameDict["Count"]) into countTagsGroup
                        from tag in countTagsGroup
                        select (tag, countTagsGroup.Key);

            return query.Take(N);
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

                GetNPopTags(dataSet, 10);
                GetNUnpopTags(dataSet, 10);

                var ages = GetAges(dataSet);

                GetCountCrDateQuestionPostsByYearsAndMonths(dataSet);

                GetCountUsersRegByYearsAndMonths(dataSet);

                using (CsvWriter writer = new CsvWriter(new StreamWriter(Path.Combine(saveDir, "Ages.csv"), false, Encoding.UTF8)))
                {
                    foreach (short? age in ages)
                    {
                        if (age.HasValue)
                        {
                            writer.WriteField(age.Value);
                        }
                        else
                        {
                            writer.WriteField("None");
                        }

                        writer.NextRecord();
                    }
                }
            }
        }
    }
}