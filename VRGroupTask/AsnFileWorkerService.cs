using System.Diagnostics;
using VRGroupTask.Models;

namespace VRGroupTask
{
    public class AsnFileWorkerService
    {
        private const int BatchSize = 10_000;

        private readonly Queue<string> _queue = new();
        private Thread? _workerThread;

        private readonly List<Box> _boxes = new();
        private readonly List<BoxContent> _boxContent = new();

        private string _currentBoxIdentifier = string.Empty;

        private Stopwatch? _stopwatch;

        /// <summary>
        /// Add file path to <c>Queue</c> and start a new thread if it is not alive
        /// </summary>
        /// <param name="path">Path to the ASIN file</param>
        public void Enqueue(string path)
        {
            _queue.Enqueue(path);
            if (_workerThread is null || !_workerThread.IsAlive)
            {
                _workerThread = new Thread(Work);
                _workerThread.Start();
            }
        }

        #region private

        private void Work()
        {
            Console.Write(new string(' ', Console.WindowWidth));
            var (_, top) = Console.GetCursorPosition();
            Console.SetCursorPosition(0, top - 2);
            Console.Write(new string(' ', Console.WindowWidth));

            while (_queue.Count > 0)
            {
                _stopwatch = new Stopwatch();
                _stopwatch.Start();

                var filePath = _queue.Dequeue();
                Console.WriteLine($"File {Path.GetFileName(filePath)}: processing started\n");

                ProcessFile(filePath);

                _stopwatch.Stop();
                var ts = _stopwatch.Elapsed;
                var elapsedTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
                Console.WriteLine($"Time: {elapsedTime}\n\n");
            }

            Console.WriteLine("Waiting for a file...\n");
            Console.Write("\rPress any key to exit...");
        }
        
        private void ProcessFile(string path)
        {
            using var reader = new StreamReader(path);
            Console.WriteLine($"\rFile processing in progress...\n");
            var currentLine = 0;
            try
            {
                var recordsCount = 0;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    currentLine++;
                    if (string.IsNullOrEmpty(line))
                        continue;

                    var parsedLine = ParseLine(line);

                    if (parsedLine is Box box)
                    {
                        if (_boxContent.Count >= BatchSize)
                        {
                            recordsCount += _boxes.Count;
                            recordsCount += _boxContent.Count;
                            Save();
                        }
                        _boxes.Add(box);
                    }
                    else if (parsedLine is BoxContent content)
                        _boxContent.Add(content);
                }

                recordsCount += _boxes.Count;
                recordsCount += _boxContent.Count;

                Save();

                Console.WriteLine($"\rFile {Path.GetFileName(path)}: processing finished\n");
                Console.WriteLine($"\rProcessed {currentLine} lines of which {recordsCount} were records\n");
            }
            catch (Exception e)
            {
                Console.WriteLine($"\rFailed to process file: {e.Message}\nLine: {currentLine}");
                throw;
            }
            finally
            {
                reader.Close();
            }
        }

        private object? ParseLine(string line)
        {
            var data = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            switch (data[0])
            {
                case "HDR":
                {
                    var boxId = data[2];
                    _currentBoxIdentifier = boxId;

                    return new Box()
                    {
                        SupplierIdentifier = data[1],
                        Identifier = boxId,
                    };
                }
                case "LINE":
                    return new BoxContent() 
                    {
                        PoNumber = data[1],
                        Isbn = data[2],
                        Quantity = int.Parse(data[3]),
                        BoxIdentifier = _currentBoxIdentifier,
                    };
                default:
                    return null;
            }
        }

        private void Save()
        {
            DataAccess.BoxBulkInsert(_boxes, _boxContent);
            _boxes.Clear();
            _boxContent.Clear();
        }

        #endregion
    }
}
