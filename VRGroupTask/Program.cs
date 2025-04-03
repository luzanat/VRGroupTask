using VRGroupTask;

/*
 * 1. I am using FileSystemWatcher to monitor a FilesToProcess folder (in the same folder where exe is located,
 *    will be created if not exists) for new files. In this case it won't process files that are already in the folder. 
 *    Potentially it may cause problems if for some reason program won't work for some time, it is better to check for 
 *    existing files and move/delete processed files from the folder.
 * 2. New files are added to the queue. It will start a thread which will work while there are files in the queue.
 * 3. Files are parsed line by line.
 * 4. Parsed records are being saved to SQL Server database in batches using SqlBulkCopy.
 *    I made that Box and all its content will be saved in one transaction, so batch size can be slightly larger to
 *    make sure that all box content is parsed. Connection string is located in DataAccess.cs.
 * 5. SqlBulkCopy saves records to the temp table, then these records will be merged into the main table using
 *    MERGE sql script. I added this to avoid primary key violation error, but it is decreasing performance.
 *
 *    I am assuming that in a ASN file every box has unique Identifier field and that every box can contain only one ISBN. 
 *    If it is not then validation must be added (e.g. using dictionaries to check for duplicate ids and increasing 
 *    quantity if there are more than one ISBN in the box).
 */

if (!DataAccess.IsDbOnline(out string msg))
{
    Console.WriteLine($"DB connection failed: {msg}");
    return;
}

var fileWatcherDirectory = new DirectoryInfo(@"./FilesToProcess");
if (!fileWatcherDirectory.Exists)
    fileWatcherDirectory.Create();

var worker = new AsnFileWorkerService();

var watcher = new FileSystemWatcher(fileWatcherDirectory.FullName, "*.txt");
watcher.NotifyFilter = NotifyFilters.FileName;
watcher.Created += (_, e) =>
{
    Thread.Sleep(1000);
    worker.Enqueue(e.FullPath);
};
watcher.EnableRaisingEvents = true;

Console.WriteLine("Waiting for a file...\n");
Console.Write("Press any key to exit...");

Console.ReadKey();