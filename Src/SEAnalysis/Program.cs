namespace SEA
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Threading;

    using Data;

    /// <summary>
    /// Класс, который хранит пути к файлам для загрузки какого-то раздела Stack Exchange.
    /// </summary>
    class FilesWithData
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

    /// <summary>
    /// Класс, который хранит несколько <see cref="FilesWithData"/> для обработки в оперативной памяти.
    /// </summary>
    class DataBatch
    {
        private LinkedList<FilesWithData> _dirs;

        private long _totalBytes;

        public long TotalBytesBatchSize => _totalBytes;

        public IReadOnlyCollection<FilesWithData> Dirs => _dirs;

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


    class Program
    {
        // Размер в GB
        const long MAX_BYTES_PER_BATCH = checked(1 * 1024L * 1024 * 1024);

        private static readonly string[] _acceptedNames =
        {
            "Posts",
            "Users",
            "Tags",
            "Comments"
        };

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

                        if(!isAdded)
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

        private static List<List<FilesWithData>> SplitBatch(DataBatch Batch)
        {
            int cpuCount = Environment.ProcessorCount;

            List<List<FilesWithData>> dataPerThread = new List<List<FilesWithData>>(cpuCount);

            if(Batch.Dirs.Count <= cpuCount)
            {
                foreach (FilesWithData data in Batch.Dirs)
                {
                    dataPerThread.Add(new List<FilesWithData>(1) { data });
                }
            }
            else
            {
                List<FilesWithData> allDirs = Batch.Dirs.ToList();

                allDirs.Sort((a, b) => -a.TotalBytesFilesSize.CompareTo(b.TotalBytesFilesSize));

                for (int i = 0; i < cpuCount; i++)
                {
                    dataPerThread.Add(new List<FilesWithData>());
                }

                int j = 0;

                foreach (FilesWithData data in allDirs)
                {
                    if(j < cpuCount)
                    {
                        dataPerThread[j].Add(data);
                        j++;
                    }
                    else
                    {
                        int minIndex = 0;
                        long minTotalSize = checked(dataPerThread[minIndex].Sum(a => a.TotalBytesFilesSize));

                        for (int k = 1; k < cpuCount; k++)
                        {
                            long totalSize = checked(dataPerThread[k].Sum(a => a.TotalBytesFilesSize));

                            if(totalSize < minTotalSize)
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

        private static int Main(string[] args)
        {
            if(args.Length != 1)
            {
                Console.WriteLine("\n\tMust be a single argument.");
                return 1;
            }

            string pathToDir = args[0];

   
            if(!Directory.Exists(pathToDir))
            {
                Console.WriteLine($"\n\t{pathToDir} does not exist.");
                return 1;
            }

            if(!Directory.Exists(Path.Combine(pathToDir, "Stat")))
            {
                Directory.CreateDirectory(Path.Combine(pathToDir, "Stat"));
            }

            Console.WriteLine($"Start processing batch...");

            foreach (DataBatch batch in CreateBatches(pathToDir))
            { 
                if(batch.TotalBytesBatchSize > MAX_BYTES_PER_BATCH)
                {
                    Console.WriteLine("Skipped:");

                    foreach (var item in batch.Dirs)
                    {
                        Console.WriteLine($"\t{item.DirectoryName}");
                    }
                }
                else
                {
                    Console.WriteLine("\nA new batch.");
                    Console.WriteLine($"\tCount of directories {batch.Dirs.Count}.");
                    Console.WriteLine($"\tSize in MB {batch.TotalBytesBatchSize / (1024 * 1024F)}.");

                    var dataPerTask = SplitBatch(batch);

                    List<List<DataSet>> dataSetsPerTask = new List<List<DataSet>>(dataPerTask.Count);

                    for (int i = 0; i < dataPerTask.Count; i++)
                    {
                        dataSetsPerTask.Add(new List<DataSet>(dataPerTask[i].Count));
                    }

                    for (int j = 0; j < dataPerTask.Count; j++)
                    {
                        for (int k = 0; k < dataPerTask[j].Count; k++)
                        {
                            FilesWithData files = dataPerTask[j][k];

                            Console.WriteLine($"Filling tables from {files.DirectoryName}");

                            DataSet set = SETables.CreateDataSet(files.DirectoryName);

                            Console.WriteLine("Filling:");

                            foreach (string path in files.FilesFullPaths)
                            {
                                string tableName = Path.GetFileNameWithoutExtension(path);
                                Console.WriteLine($"\t{tableName}");
                                Filler filler = Filler.GetFillerByTableName(tableName);
                                DataTable table = set.Tables[tableName];
                                filler.FillFromXml(path, table);
                            }

                            dataSetsPerTask[j].Add(set);
                        }
                    }
                }
            }

            Console.ReadKey();

            return 0;
        }   
    }
}