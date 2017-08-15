using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GitLogger
{
    public static class FileUtil
    {
        public static void SaveAsCsv(IList<Commit> commits, string path)
        {

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (StreamWriter w = File.AppendText(path))
            {
                w.WriteLine("Area, PR, Issue, Commit, Message");
                var area = "";
                foreach (var commit in commits)
                {
                    var line = new StringBuilder();

                    var commitString = $"= HYPERLINK(\"{commit.Link}\", \"Commit\")";
                    var prString = string.Empty;
                    var issueString = string.Empty;

                    if (commit.PR != null)
                    {
                        prString = $"= HYPERLINK(\"{commit.PR.Item2}\", \"{commit.PR.Item2}\")";
                    }

                    if (commit.Issue != null)
                    {
                        issueString = $"= HYPERLINK(\"{commit.Issue.Item2}\", \"Commit\")";
                    }

                    line.Append(area);
                    line.Append(",");
                    line.Append(prString);
                    line.Append(",");
                    line.Append(issueString);
                    line.Append(",");
                    line.Append(commitString);
                    line.Append(",");
                    line.Append(commit.Message);

                    w.WriteLine(line.ToString());
                }
            }
        }
    }
}
