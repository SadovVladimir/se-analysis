namespace SEA.Data
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using CsvHelper;
    using CsvHelper.Configuration;
    using CsvHelper.TypeConversion;

    using AngleSharp.Dom.Html;
    using AngleSharp.Extensions;
    using AngleSharp.Parser.Html;

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
            HtmlDocGen gen = new HtmlDocGen();

            DataTable usersTable = DataSet.Tables["Users"];

            var users = usersTable.AsEnumerable()
                        .GroupBy(row => row.Field<int>(GlobalDef.AttrToColumnNameDict["Reputation"]))
                        .OrderByDescending(group => group.Key)
                        .Take(N)
                        .SelectMany(group => group.Select(row =>
                        {
                            short? age = row.Field<short?>(GlobalDef.AttrToColumnNameDict["Age"]);
                            return new
                            {
                                Reputation = group.Key,
                                Name = row.Field<string>(GlobalDef.AttrToColumnNameDict["DisplayName"]),
                                Location = row.Field<string>(GlobalDef.AttrToColumnNameDict["Location"]),
                                AboutMe = gen.GetFullHtmlDocAsString(row.Field<string>(GlobalDef.AttrToColumnNameDict["AboutMe"])),
                                Age = age.HasValue ? age.Value.ToString() : "None"
                            };
                        })
                        );

            return users;
        }

        private static IEnumerable<object> GetNMostViewsQuestion(DataSet DataSet, int N)
        {
            DataTable postsTable = DataSet.Tables["Posts"];

            HtmlDocGen gen = new HtmlDocGen();

            var mostViewsQuestions = from row in postsTable.AsEnumerable()
                                     where row.Field<PostType>(GlobalDef.AttrToColumnNameDict["PostTypeId"]) == PostType.Question
                                     orderby row.Field<uint>(GlobalDef.AttrToColumnNameDict["ViewCount"]) descending
                                     select new
                                     {
                                         ViewCount = row.Field<uint>(GlobalDef.AttrToColumnNameDict["ViewCount"]),
                                         Score = row.Field<int>(GlobalDef.AttrToColumnNameDict["Score"]),
                                         Title = row.Field<string>(GlobalDef.AttrToColumnNameDict["Title"]),
                                         Body = gen.GetFullHtmlDocAsString(row.Field<string>(GlobalDef.AttrToColumnNameDict["Body"])),
                                         Tags = row.Field<string>(GlobalDef.AttrToColumnNameDict["Tags"])
                                     };

            return mostViewsQuestions.Take(N);
        }

        private static IEnumerable<object> GetNQuestionsByScore(DataSet DataSet, int N)
        {
            DataTable postsTable = DataSet.Tables["Posts"];

            HtmlDocGen gen = new HtmlDocGen();

            var mostViewsQuestions = from row in postsTable.AsEnumerable()
                                     where row.Field<PostType>(GlobalDef.AttrToColumnNameDict["PostTypeId"]) == PostType.Question
                                     orderby row.Field<int>(GlobalDef.AttrToColumnNameDict["Score"]) descending
                                     select new
                                     {
                                         ViewCount = row.Field<uint>(GlobalDef.AttrToColumnNameDict["ViewCount"]),
                                         Score = row.Field<int>(GlobalDef.AttrToColumnNameDict["Score"]),
                                         Title = row.Field<string>(GlobalDef.AttrToColumnNameDict["Title"]),
                                         Body = gen.GetFullHtmlDocAsString(row.Field<string>(GlobalDef.AttrToColumnNameDict["Body"])),
                                         Tags = row.Field<string>(GlobalDef.AttrToColumnNameDict["Tags"])
                                     };

            return mostViewsQuestions.Take(N);
        }

        private static void SaveToCSV(string PathToSave, IEnumerable<object> Collection)
        {
            using (CsvWriter writer = new CsvWriter(new StreamWriter(PathToSave, false, Encoding.UTF8)))
            {
                writer.WriteRecords(Collection);
            }
        }

        public static void CollectStat(DataSet Data, string SaveDir)
        {
            string saveDir = Path.Combine(SaveDir, Data.DataSetName);

            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir);
            }

            var ages = GetAges(Data);

            var popTags = GetNPopTags(Data, 30);

            var unpopTags = GetNUnpopTags(Data, 30);

            var users = GetNUsersHaveHighestRep(Data, 20);

            var usersReg = GetCountUsersRegByYearsAndMonths(Data);

            var posts = GetCountCrDateQuestionPostsByYearsAndMonths(Data);

            var mostViewQuestions = GetNMostViewsQuestion(Data, 50);

            var questionsHighestScore = GetNQuestionsByScore(Data, 50);

            SaveToCSV(Path.Combine(saveDir, "Ages.csv"), ages);
            SaveToCSV(Path.Combine(saveDir, "PopTags.csv"), popTags);
            SaveToCSV(Path.Combine(saveDir, "UnpopTags.csv"), unpopTags);
            SaveToCSV(Path.Combine(saveDir, "UsersHighestRep.csv"), users);
            SaveToCSV(Path.Combine(saveDir, "UsersReg.csv"), usersReg);
            SaveToCSV(Path.Combine(saveDir, "PostsCrDate.csv"), posts);
            SaveToCSV(Path.Combine(saveDir, "MostViewQuestions.csv"), mostViewQuestions);
            SaveToCSV(Path.Combine(saveDir, "QuestionsHighestScore.csv"), questionsHighestScore);
        }
    }
}