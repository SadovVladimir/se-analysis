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

    /// <summary>
    /// Класс, который хранит несколько <see cref="FilesWithData"/> для обработки в оперативной памяти. 
    /// </summary>
    internal class DataBatch
    {
        private LinkedList<FilesWithData> _dirs;

        private long _totalBytes;

        public IReadOnlyCollection<FilesWithData> Dirs => _dirs;

        public long TotalBytesBatchSize => _totalBytes;

        public DataBatch()
        {
            _dirs = new LinkedList<FilesWithData>();
            _totalBytes = 0L;
        }

        public void Add(FilesWithData File)
        {
            _dirs.AddLast(File);
            _totalBytes = checked(_totalBytes + File.TotalBytesFilesSize);
        }
    }

    internal class DataForTask
    {
        private List<FilesWithData> _filesGroups;

        public IReadOnlyCollection<FilesWithData> FilesGroups => _filesGroups;

        public DataForTask()
        {
            _filesGroups = new List<FilesWithData>();
        }

        public void Add(FilesWithData Data)
        {
            _filesGroups.Add(Data);
        }
    }

    /// <summary>
    /// Класс, который хранит пути к файлам для загрузки какого-то раздела Stack Exchange. 
    /// </summary>
    internal class FilesWithData
    {
        private long _totalSize;

        public string DirectoryName { get; private set; }

        public LinkedList<string> FilesFullPaths { get; private set; }

        public long TotalBytesFilesSize => _totalSize;

        public FilesWithData(string DirName, IEnumerable<string> FullPaths)
        {
            this.DirectoryName = DirName;
            this.FilesFullPaths = new LinkedList<string>(FullPaths);
            _totalSize = checked(FullPaths.Select(file => new FileInfo(file).Length).Sum());
        }
    }

    internal class Program
    {
        // Размер в GB
        private const long MAX_BYTES_PER_BATCH = checked(200 * 1024L * 1024);

        private static readonly string[] _acceptedNames =
        {
            "Posts",
            "Users",
            "Tags",
            "Comments"
        };

        private static Logger Log = LogManager.GetLogger("Main");

        private static IEnumerable<DataBatch> CreateBatches(string PathToDir)
        {
            List<DataBatch> batches = new List<DataBatch>();

            DirectoryInfo direct = new DirectoryInfo(PathToDir);

            LinkedList<string> _paths = new LinkedList<string>();

            foreach (DirectoryInfo childDir in direct.GetDirectories())
            {
                foreach (string path in Directory.GetFiles(childDir.FullName, "*.xml"))
                {
                    string name = Path.GetFileNameWithoutExtension(path);

                    if (_acceptedNames.Contains(name))
                    {
                        _paths.AddLast(path);
                    }
                }

                if (_paths.Count != 0)
                {
                    FilesWithData files = new FilesWithData(childDir.Name, _paths);

                    if (batches.Count == 0)
                    {
                        DataBatch batch = new DataBatch();
                        batch.Add(files);

                        batches.Add(batch);
                    }
                    else
                    {
                        bool isAdded = false;

                        for (int i = 0; i < batches.Count; i++)
                        {
                            if (checked(batches[i].TotalBytesBatchSize + files.TotalBytesFilesSize) <= MAX_BYTES_PER_BATCH)
                            {
                                batches[i].Add(files);
                                isAdded = true;
                                break;
                            }
                        }

                        if (!isAdded)
                        {
                            DataBatch batch = new DataBatch();
                            batch.Add(files);

                            batches.Add(batch);
                        }
                    }
                }

                _paths.Clear();
            }

            return batches;
        }

        private static List<List<DataSet>> GetDataSets(IReadOnlyList<DataForTask> DataSetsPerTask)
        {
            List<List<DataSet>> dataSetsPerTask = new List<List<DataSet>>(DataSetsPerTask.Count);

            for (int i = 0; i < DataSetsPerTask.Count; i++)
            {
                dataSetsPerTask.Add(new List<DataSet>(DataSetsPerTask[i].FilesGroups.Count));
            }

            for (int j = 0; j < DataSetsPerTask.Count; j++)
            {
                foreach (FilesWithData files in DataSetsPerTask[j].FilesGroups)
                {
                    Log.Info($"Filling tables from {files.DirectoryName}");

                    DataSet set = SETables.CreateDataSet(files.DirectoryName);

                    Log.Info("Filling:");

                    foreach (string path in files.FilesFullPaths)
                    {
                        string tableName = Path.GetFileNameWithoutExtension(path);
                        Log.Info($"\t{tableName}");
                        Filler filler = Filler.GetFillerByTableName(tableName);
                        DataTable table = set.Tables[tableName];
                        filler.FillFromXml(path, table);
                    }

                    dataSetsPerTask[j].Add(set);
                }
            }

            return dataSetsPerTask;
        }

        private static Task[] GetTasks(IReadOnlyCollection<IEnumerable<DataSet>> DataSetsPerTask, string FullPathToSaveRes)
        {
            Task[] tasks = new Task[DataSetsPerTask.Count];
            int i = 0;

            foreach (var DataSets in DataSetsPerTask)
            {
                var paramForTask = (FullPathToSaveRes, DataSets);

                tasks[i++] = new Task(Stat.CollectStat, paramForTask);
            }

            return tasks;
        }

        private static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Log.Info("\n\tMust be a single argument.\n\tPath to directories which contains data.");
                return 1;
            }

            string pathToDir = args[0];

            if (!Directory.Exists(pathToDir))
            {
                Log.Info($"\n\t{pathToDir} does not exist.");
                return 1;
            }

            string resDir = Path.Combine(pathToDir, "Stat");

            if (!Directory.Exists(resDir))
            {
                Directory.CreateDirectory(resDir);
            }

            Log.Info($"Start processing batch...");

            foreach (DataBatch batch in CreateBatches(pathToDir))
            {
                if (batch.TotalBytesBatchSize > MAX_BYTES_PER_BATCH)
                {
                    Log.Info("Skipped:");

                    foreach (var item in batch.Dirs)
                    {
                        Log.Info($"\t{item.DirectoryName}");
                    }
                }
                else
                {
                    Log.Info("\nA new batch:");
                    Log.Info($"\tCount of directories {batch.Dirs.Count}.");
                    Log.Info($"\tSize in MB {batch.TotalBytesBatchSize / (1024 * 1024F)}.");

                    IReadOnlyList<DataForTask> dataPerTask = SplitBatch(batch);

                    IReadOnlyCollection<IEnumerable<DataSet>> dataSetsPerTask = GetDataSets(dataPerTask);

                    Task[] tasks = GetTasks(dataSetsPerTask, resDir);

                    Log.Info("Tasks are running.");

                    for (int i = 0; i < tasks.Length; i++)
                    {
                        tasks[i].Start();
                    }

                    Task.WaitAll(tasks);
                }
            }

            return 0;
        }

        /// <summary>
        /// Разделение. 
        /// </summary>
        /// <param name="Batch"></param>
        /// <returns></returns>
        private static IReadOnlyList<DataForTask> SplitBatch(DataBatch Batch)
        {
            int cpuCount = Environment.ProcessorCount;

            List<DataForTask> dataPerThread = new List<DataForTask>(cpuCount);

            if (Batch.Dirs.Count <= cpuCount)
            {
                foreach (FilesWithData data in Batch.Dirs)
                {
                    DataForTask dataForTask = new DataForTask();
                    dataForTask.Add(data);
                    dataPerThread.Add(dataForTask);
                }
            }
            else
            {
                List<FilesWithData> allDirs = Batch.Dirs.ToList();

                allDirs.Sort((a, b) => -a.TotalBytesFilesSize.CompareTo(b.TotalBytesFilesSize));

                for (int i = 0; i < cpuCount; i++)
                {
                    dataPerThread.Add(new DataForTask());
                }

                int j = 0;

                foreach (FilesWithData data in allDirs)
                {
                    if (j < cpuCount)
                    {
                        dataPerThread[j].Add(data);
                        j++;
                    }
                    else
                    {
                        int minIndex = 0;
                        long minTotalSize = checked(dataPerThread[minIndex].FilesGroups.Sum(a => a.TotalBytesFilesSize));

                        for (int k = 1; k < cpuCount; k++)
                        {
                            long totalSize = checked(dataPerThread[k].FilesGroups.Sum(a => a.TotalBytesFilesSize));

                            if (totalSize < minTotalSize)
                            {
                                minTotalSize = totalSize;
                                minIndex = k;
                            }
                        }

                        dataPerThread[minIndex].Add(data);
                    }
                }
            }

            return dataPerThread;
        }
    }
}