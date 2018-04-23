namespace SEA
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Data;

    using NLog;

    using CommandLine;
    using CommandLine.Text;



    class Options
    {
        private static readonly Example[] _examples = new Example[2]
        {
            new Example("Example", new UnParserSettings() { UseEqualToken = true }, new Options { InputDir = "Path_to_input_dir"}),
            new Example("Constrain size of files", new Options {InputDir = "Path_to_input_dir", MaxFilesSize = 209715200 })
        };

        [Value(0, MetaName = "Input dir.", HelpText = "A path to a directory which contains all directories of sections Stack Exchange.", Required = true)]
        public string InputDir { get; set; }

        [Option('s', "size", HelpText = "A maximum size of all files for loading to RAM (in bytes) in the directory. If files have a size is greater than maximum then it is skipped.", Default = 3 * 1024 * 1024L * 1024)]
        public long MaxFilesSize { get; set; }

        [Usage]
        public static IEnumerable<Example> Examples => _examples;
    }


    internal class Program
    {
        private static readonly string[] _acceptedNames =
        {
            "Posts",
            "Users",
            "Tags",
            "Comments"
        };

        private static Logger _log = LogManager.GetLogger("Main");

        private static DataSet LoadDataSet(IEnumerable<FileInfo> Files, string DataSetName)
        {
            DataSet set = SETables.CreateDataSet(DataSetName);

            _log.Info("Filling:");

            foreach (FileInfo file in Files)
            {
                string tableName = Path.GetFileNameWithoutExtension(file.Name);

                _log.Info($"\t{tableName}");

                Filler filler = Filler.GetFillerByTableName(tableName);
                DataTable table = set.Tables[tableName];
                filler.FillFromXml(file.FullName, table);
            }

            return set;
        }

        private static int Run(Options Options)
        {
            string pathToDir = Options.InputDir;

            long maxFilesSizeInBytes = Options.MaxFilesSize;

            if (!Directory.Exists(pathToDir))
            {
                _log.Info($"\n\tThe directory '{pathToDir}' does not exist.");
                return 1;
            }

            string resDir = "Stat";

            if(Directory.Exists(resDir))
            {
                string[] subEntryies = Directory.GetFileSystemEntries(resDir);

                char answer = 'n';

                if (subEntryies.Length != 0)
                {
                    _log.Info($"The directory '{Path.Combine(".", resDir)}' is not empty. Need to clear '{resDir}'.\nDo you want to delete all subdirectories and files?");

                    do
                    {
                        _log.Info("\nEnter 'Y/y' or 'N/n'.");

                        answer = Console.ReadKey().KeyChar;

                        answer = char.ToLowerInvariant(answer);

                    } while (answer != 'y' && answer != 'n');

                    _log.Info("\n");

                    if (answer == 'n')
                    {
                        _log.Info($"The program need to clear '{resDir}' for further work. Save all files from '{resDir}' and rerun program.");
                        return 0;
                    }
                    else if (answer == 'y')
                    {
                        Directory.Delete(resDir, true);
                    }
                }
                else
                {
                    Directory.Delete(resDir);
                }

               
            }

            Directory.CreateDirectory(resDir);

            DirectoryInfo datasetsDir = new DirectoryInfo(pathToDir);

            LinkedList<FileInfo> files = new LinkedList<FileInfo>();

            foreach (DirectoryInfo datasetDir in datasetsDir.GetDirectories())
            {
                _log.Info($"Filling tables from '{datasetDir.Name}'.");

                long totalSize = 0;

                foreach (FileInfo file in datasetDir.GetFiles("*.xml"))
                {
                    if (_acceptedNames.Contains(Path.GetFileNameWithoutExtension(file.Name)))
                    {
                        totalSize += file.Length;

                        if (totalSize > maxFilesSizeInBytes)
                        {
                            _log.Info("\tExceeded maximum size of files.\n\tThe directory skipped.");
                            files.Clear();
                            break;
                        }
                        else
                        {
                            files.AddLast(file);
                        }
                    }
                }

                if (files.Count != 0)
                {
                    DataSet dataSet = LoadDataSet(files, datasetDir.Name);
                    files.Clear();
                    Stat.CollectStat(dataSet, resDir);
                }
            }

            _log.Info($"\nAll files are saved in '{Path.Combine(".", resDir)}'.");

            return 0;
        }

        private static int Main(string[] args)
        {
            ParserResult<Options> result = Parser.Default.ParseArguments<Options>(args);

            return result.MapResult(options => Run(options), _ => 1);
        }
    }
}