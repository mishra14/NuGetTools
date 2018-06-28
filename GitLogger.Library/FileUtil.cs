﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Excel = Microsoft.Office.Interop.Excel;


namespace GitLogger.Library
{
    public static class FileUtil
    {
        public static void SaveAsCsv(IList<Commit> commits, string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (var w = File.AppendText(path))
            {
                w.WriteLine("Area, PR, Issues, Commit, Author, Commit Message");
                var area = "";
                foreach (var commit in commits)
                {
                    var line = new StringBuilder();

                    var commitString = commit.Link;
                    var prString = string.Empty;
                    var issueStringBuilder = new StringBuilder();

                    if (commit.PR != null)
                    {
                        prString = commit.PR.Item2;
                    }

                    if (commit.Issues != null)
                    {
                        foreach (var issue in commit.Issues)
                        {
                            issueStringBuilder.Append(issue.Item2 + " ");
                        }
                    }

                    line.Append(area);
                    line.Append(",");
                    line.Append(prString);
                    line.Append(",");
                    line.Append(issueStringBuilder.ToString());
                    line.Append(",");
                    line.Append(commitString);
                    line.Append(",");
                    line.Append(commit.Author);
                    line.Append(",");
                    line.Append(commit.SanitizedMessage);

                    w.WriteLine(line.ToString());
                }
            }

            Console.WriteLine($"Saving results file: {path}");
        }

        // Source sample - https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/interop/how-to-access-office-onterop-objects
        public static void SaveAsExcel(IList<Commit> commits, string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var excelApp = new Excel.Application
            {
                // Dont need to see the app.
                Visible = false
            };

            // Create a new, empty workbook and add it to the collection returned 
            // by property Workbooks. The new workbook becomes the active workbook.
            // Add has an optional parameter for specifying a praticular template. 
            // Because no argument is sent in this example, Add creates a new workbook. 
            excelApp.Workbooks.Add();

            // This example uses a single workSheet. 
            Excel._Worksheet workSheet = (Excel.Worksheet)excelApp.ActiveSheet;

            workSheet.Cells[1, "A"] = "Area";
            workSheet.Cells[1, "B"] = "PR";
            workSheet.Cells[1, "C"] = "Issues";
            workSheet.Cells[1, "D"] = "Commit";
            workSheet.Cells[1, "E"] = "Author";
            workSheet.Cells[1, "F"] = "Commit Message";

            var row = 1;
            var area = "";
            foreach (var commit in commits)
            {
                row++;
                var prString = string.Empty;
                var issueStringBuilder = new StringBuilder();
                var commitString = $"= HYPERLINK(\"{commit.Link}\", \"Commit\")";

                if (commit.PR != null)
                {
                    prString = $"= HYPERLINK(\"{commit.PR.Item2}\", \"{commit.PR.Item1}\")";
                }

                if (commit.Issues != null)
                {
                    if (commit.Issues.Count > 1)
                    {
                        foreach (var issue in commit.Issues)
                        {
                            issueStringBuilder.Append(issue.Item2 + "\r\n");
                        }
                    }
                    else
                    {
                        foreach (var issue in commit.Issues)
                        {
                            issueStringBuilder.Append($"= HYPERLINK(\"{issue.Item2}\", \"{issue.Item1}\")");
                        }
                    }

                }


                workSheet.Cells[row, "A"] = area;
                workSheet.Cells[row, "B"] = prString;
                workSheet.Cells[row, "C"] = issueStringBuilder.ToString();
                workSheet.Cells[row, "D"] = commitString;
                workSheet.Cells[row, "E"] = commit.Author;
                workSheet.Cells[row, "F"] = commit.SanitizedMessage;

            }

            workSheet.Range["A1", $"E1"].AutoFormat(
                Excel.XlRangeAutoFormat.xlRangeAutoFormatClassic2);

            workSheet.Range["A1", $"E1"].WrapText = true;

            workSheet.SaveAs(path);

            excelApp.ActiveWorkbook.Close();
            excelApp.Quit();

            Console.WriteLine($"Saving results file: {path}");
        }

        public static void SaveAsHtml(IList<Commit> commits, string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (var w = File.AppendText(path))
            {
                w.WriteLine("<!DOCTYPE html>");
                w.WriteLine("<html lang=\"en\" xmlns=\"http://www.w3.org/1999/xhtml\">");
                w.WriteLine("<head>");
                w.WriteLine("<meta charset=\"utf-8\" />");
                w.WriteLine("<title>GitLogger Result</title>");
                w.WriteLine("<style>");
                w.WriteLine("table, th, td {");
                w.WriteLine("    border: 1px solid black;");
                w.WriteLine("    border-collapse: collapse;");
                w.WriteLine("    padding: 15px;");
                w.WriteLine("    text-align: left;");
                w.WriteLine("}");
                w.WriteLine("</style>");
                w.WriteLine("</head>");

                w.WriteLine("<body>");

                w.WriteLine("<table style=\"width:100%\">");
                w.WriteLine("<tr>");
                w.WriteLine("<th>Area</th>");
                w.WriteLine("<th>PR</th>");
                w.WriteLine("<th>Issue(s)</th>");
                w.WriteLine("<th>Commit</th>");
                w.WriteLine("<th>Author</th>");
                w.WriteLine("<th>Commit Message</th>");
                w.WriteLine("<tr>");

                // add a table of all commits here

                foreach (var commit in commits)
                {
                    var commitString = commit.Link;
                    var prString = string.Empty;
                    var issueStringBuilder = new StringBuilder();

                    if (commit.PR != null)
                    {
                        prString = $"<a href=\"{commit.PR.Item2}\">{commit.PR.Item1}</a>";
                    }

                    if (commit.Issues != null)
                    {
                        var issues = commit.Issues.ToList();
                        for (var i = 0; i < issues.Count; i++)
                        {
                            var issue = issues[i];
                            issueStringBuilder.Append($"<a href=\"{issue.Item2}\">{issue.Item1}</a>");

                            if (i < issues.Count - 1)
                            {
                                issueStringBuilder.Append($"</br>");
                            }
                        }
                    }

                    w.WriteLine("<tr>");
                    w.WriteLine($"<td></td>");
                    w.WriteLine($"<td>{prString}</td>");
                    w.WriteLine($"<td>{issueStringBuilder.ToString()}</td>");
                    w.WriteLine($"<td><a href=\"{commit.Link}\">{commit.Sha}</a></td>");
                    w.WriteLine($"<td>{commit.Author}</td>");
                    w.WriteLine($"<td>{commit.SanitizedMessage}</td>");
                    w.WriteLine("<tr>");
                }

                w.WriteLine("</body>");
                w.WriteLine("</html>");
            }

            Console.WriteLine($"Saving results file: {path}");
        }

        public static void ClearLogFiles(string resultCsvPath, string resultExcelPath, string cachePath, bool useCache)
        {
            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);
            }

            if (!File.Exists(resultCsvPath))
            {
                File.Delete(resultCsvPath);
            }

            if (!File.Exists(resultExcelPath))
            {
                File.Delete(resultExcelPath);
            }

            if (!useCache)
            {
                var di = new DirectoryInfo(cachePath);

                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    dir.Delete(recursive: true);
                }
            }


        }

        public static void CacheResponse(string path, string data)
        {
            Console.WriteLine($"Caching File: {path}");
            //open file stream
            using (StreamWriter file = File.CreateText(path))
            {
                file.Write(data);
            }
        }

        public static string GetCachedResponse(string path)
        {
            var result = string.Empty;

            if (File.Exists(path))
            {
                Console.WriteLine($"Using cached file: {path}");
                result = File.ReadAllText(path);
            }

            return result;
        }

        public static Tuple<string, string> GetAppCredentials(string path)
        {
            Tuple<string, string> result = null;

            if (File.Exists(path))
            {
                var lines = File.ReadAllLines(path);
                result = Tuple.Create(lines[0], lines[1]);
            }

            return result;
        }
    }
}
