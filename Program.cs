using SharpCompress.Common;
using SharpCompress.Compressors.Xz;
using System;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;

string folderPath = "logs";
string[] regexPattern = File.ReadAllLines("patterns.txt");

List<FileInfo> logs = new DirectoryInfo(folderPath).GetFiles().ToList();
Regex[] compiledPatterns = regexPattern.Select(p => new Regex(p, RegexOptions.Compiled)).ToArray();

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
        string line;
        
        while ((line = reader.ReadLine()) != null) {
            foreach (var pattern in compiledPatterns) {
                var match = pattern.Match(line);
                if (match.Success) {
                    //Console.WriteLine($"Matched: {match.Groups["player1"].Value}");
                    string comment = match.Groups["comment"].Value;
                    line = comment == "" ? line : line.Replace(comment, "");
                    Console.WriteLine(line);
                }
            }
        }
    }
}

