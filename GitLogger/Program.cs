using System;

namespace GitLogger
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var commits = HttpUtil.GetCommits("");
            foreach (var commit in commits)
            {
                commit.PopulateIssueAndPRData();
            }
        }
    }
}
