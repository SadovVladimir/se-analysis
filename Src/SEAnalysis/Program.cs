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


    internal class Program
    {
        // Максимальный суммарный размер файлов для одного раздела Stack Exchange.
        private const long MAX_FILES_SIZE_IN_BYTES = checked((3 * 1024 + 600) * 1024L * 1024);

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

        private static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                _log.Info("\n\tMust be a single argument (path to directories which contains data).");
                return 1;
            }

            string pathToDir = args[0];

            if (!Directory.Exists(pathToDir))
            {
                _log.Info($"\n\t{pathToDir} does not exist.");
                return 1;
            }

            string resDir = "Stat";

            if (Directory.Exists(resDir))
            {
                Directory.Delete(resDir, true);
            }

            Directory.CreateDirectory(resDir);

            DirectoryInfo datasetsDir = new DirectoryInfo(pathToDir);

            LinkedList<FileInfo> files = new LinkedList<FileInfo>();

            foreach (DirectoryInfo datasetDir in datasetsDir.GetDirectories())
            {
                _log.Info($"Filling tables from {datasetDir.Name}.");

                long totalSize = 0;

                foreach (FileInfo file in datasetDir.GetFiles("*.xml"))
                {
                    if (_acceptedNames.Contains(Path.GetFileNameWithoutExtension(file.Name)))
                    {
                        totalSize += file.Length;

                        if (totalSize > MAX_FILES_SIZE_IN_BYTES)
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

            return 0;
        }
    }
}