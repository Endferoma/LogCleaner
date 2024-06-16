using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

string folderPath = "logs";
string[] regexPattern = File.ReadAllLines("patterns.txt");

List<FileInfo> logs = new DirectoryInfo(folderPath).GetFiles().ToList();
Regex[] compiledPatterns = regexPattern.Select(p => new Regex(p, RegexOptions.Compiled)).ToArray();

if (File.Exists("output.txt")) {
    File.Delete("output.txt");
}

foreach (var log in logs) {
    using (var streamReader = new StreamReader(log.FullName)) {
        DateOnly logDate = DateOnly.ParseExact(log.CreationTimeUtc.Date.ToString("yyyy-MM-dd"), "yyyy-MM-dd", null);
        if (log.Extension == ".gz") {
            using (var archive = new GZipStream(streamReader.BaseStream, CompressionMode.Decompress))
            using (var archiveReader = new StreamReader(archive)) {
                
                ReadFileStream(logDate, archiveReader);
            }
        } else {
            ReadFileStream(logDate, streamReader);
        }
    }

    void ReadFileStream(DateOnly date, StreamReader reader) {
        string line; string datetimestamp = "";
        bool serverStarted = false; bool serverClosed = false;

        using (StreamWriter writer = new StreamWriter("output.txt", true)) {
            while ((line = reader.ReadLine()) != null) {
                foreach (var pattern in compiledPatterns) {
                    var match = pattern.Match(line);
                    if (match.Success) {
                        string comment = match.Groups["comment"].Value;
                        string timestamp = match.Groups["timestamp"].Value;
                        datetimestamp = date.ToString("yyyy':'MM':'dd-") + timestamp;

                        line = line.Replace(timestamp, datetimestamp);

                        line = comment == "" ? line : line.Replace(comment, "");
                        writer.WriteLine(line);
                        Console.WriteLine(line);
                        if (pattern.ToString() == regexPattern[0]) {
                            serverStarted = true;
                        } else if (pattern.ToString() == regexPattern[1]) {
                            serverClosed = true;
                        }
                    }
                }
            }
            if (!serverClosed) {
                line = $"[{datetimestamp}] [Server thread/INFO]: Closing Server";
                writer.WriteLine(line);
                Console.WriteLine(line);
            }
        }
    }

}