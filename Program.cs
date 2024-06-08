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
        if (log.Extension == ".gz") {
            using (var archive = new GZipStream(streamReader.BaseStream, CompressionMode.Decompress))
            using (var archiveReader = new StreamReader(archive)) {
                ReadFileStream(archiveReader);
            }
        } else {
            ReadFileStream(streamReader);
        }
    }

    void ReadFileStream(StreamReader reader) {
        string line; string timestamp = "";
        bool serverStarted = false; bool serverClosed = false;

        using (StreamWriter writer = new StreamWriter("output.txt", true)) {
            while ((line = reader.ReadLine()) != null) {
                foreach (var pattern in compiledPatterns) {
                    var match = pattern.Match(line);
                    if (match.Success) {
                        string comment = match.Groups["comment"].Value;
                        line = comment == "" ? line : line.Replace(comment, "");
                        writer.WriteLine(line);
                        Console.WriteLine(line);
                        if (pattern.ToString() == regexPattern[0]) {
                            serverStarted = true;
                        } else if (pattern.ToString() == regexPattern[1]) {
                            serverClosed = true;
                        }
                        timestamp = match.Groups["timestamp"].Value;
                    }
                }
            }
            if (!serverClosed) {
                line = $"{timestamp} [Server thread/INFO]: Closing Server";
                writer.WriteLine(line);
                Console.WriteLine(line);
            }
        }
    }

}