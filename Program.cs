using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;

namespace DistroWatchScraper
{
    class Program
    {
        static void Main(string[] args)
        {
            List<LinuxDistro> distros = new();

            var client = new RestClient();
            client.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.141 Safari/537.36";
            var linuxListFile = @"U:\linux.list";
            if (!Directory.Exists("./Out"))
                Directory.CreateDirectory("./Out");
            if (!Directory.Exists("./Out/Web"))
            {
                Directory.CreateDirectory("./Out/Web");
                int i = 0;
                var lines = File.ReadAllLines(linuxListFile);
                foreach (var linux in lines)
                {
                    Console.WriteLine($"{linux} {i}/{lines.Length}");
                    var req = new RestRequest(
                        $"https://distrowatch.com/table.php?distribution={linux.Replace(" ", "").Replace("!", "")}");
                    var res = client.Get(req);
                    File.WriteAllTextAsync($"./Out/Web/{linux.Replace("|", "").Replace("/", "")}.html", res.Content);
                    i++;
                }
            }

            var stopwatch = Stopwatch.StartNew();
            // ReSharper disable InconsistentNaming
            var options = RegexOptions.Compiled;
            var FullNameRegex = new Regex("(?<=<h1>)(.+)(?=</h1>)", options);
            var OsTypeRegex = new Regex("(?<=<li><b>OS Type:</b> <a href=\"(.+)\">)(.+?)(?=<)", options);
            var BasedOnRegex = new Regex("(?<=<a href=\"search.php\\?basedon=)(.+?)(?=#simple\">), options");
            var OriginRegex = new Regex("(?<=<li><b>Origin:</b> <a href=\"(.+)\">)(.+?)(?=</a>)", options);
            var CategoriesRegex =
                new Regex("(?<=<a href=\"search\\.php\\?category=(.+?)#simple\">)(.+?)(?=<)", options);
            var DescriptionRegex = new Regex("(?!\\n)(?<=</ul>\\n)((.|\\n)+?)(?=<)");
            var ArchitecturesRegex = new Regex("(?<=href=\"search\\.php\\?architecture=)(.+?)(?=#simple\">)", options);
            var DesktopsRegex = new Regex("(?<=href=\"search\\.php\\?desktop=)(.+?)(?=#simple\">)", options);
            var DistributionFullNameRegex = new Regex(
                "(?<=<tr class=\"Background\">\\n    <th class=\"Info\">Distribution</th>\\n    <td class=\"Info\">)(.+?)(?=<)",
                options);
            var HomePageRegex = new Regex(
                "(?<=<tr class=\"Background\">\\n    <th class=\"Info\">Home Page</th>\\n    <td class=\"Info\"><a href=\")(.+?)(?=\">)",
                options);
            var StatusRegex = new Regex("(?<=<li><b>Status:</b> <font color=\"(.+)\">)(.+?)(?=</font>)", options);
            var IconUrlRegex = new Regex(
                "(?<=<img src=\")((.)+?)(?=\" border=\"0\" title=\"(.+?)\" vspace=\"(.+?)\" hspace=\"(.+?)\" align=\"left\">)",
                options);
            var ScreenshotSmallUrlRegex = new Regex(
                "(?<=<img src=\")((.)+?)(?=\" border=\"0\" title=\"(.+?)\" vspace=\"(.+?)\" hspace=\"(.+?)\" align=\"right\">)",
                options);
            var ScreenshotUrlRegex = new Regex(
                "(?<=<a href=\")((.)+?)(?=\"><img src=\"?((.)+?)\" border=\"0\" title=\"(.+?)\" vspace=\"(.+?)\" hspace=\"(.+?)\" align=\"right\">)",
                options);
            // ReSharper restore InconsistentNaming
            Parallel.ForEach(Directory.GetFiles("./Out/Web"), file =>
            {
                var text = File.ReadAllText(file);
                var distro = new LinuxDistro()
                {
                    FullName = FullNameRegex.Match(text).ToString(),
                    OsType = OsTypeRegex.Match(text).ToString(),
                    BasedOn = BasedOnRegex.Matches(text)
                        .ToStrings(),
                    Origin = OriginRegex.Match(text).ToString(),
                    Categories = CategoriesRegex
                        .Matches(text).ToStrings(),
                    Description = DescriptionRegex.Match(text).ToString(),
                    Architectures = ArchitecturesRegex
                        .Matches(text).ToStrings(),
                    Desktops = DesktopsRegex.Matches(text)
                        .ToStrings(),
                    DistributionFullName =
                        DistributionFullNameRegex
                            .Match(text).ToString(),
                    HomePage = HomePageRegex
                        .Match(text).ToString(),
                    Status = StatusRegex.Match(text)
                        .ToString(),
                    IconUrl = "https://distrowatch.com/" +
                              IconUrlRegex
                                  .Match(text),
                    ScreenshotSmallUrl = "https://distrowatch.com/" +
                                         ScreenshotSmallUrlRegex
                                             .Match(text),
                    ScreenshotUrl = "https://distrowatch.com/" +
                                    ScreenshotUrlRegex
                                        .Match(text)
                };
                distros.Add(distro);
            });
            Console.WriteLine(stopwatch.ElapsedMilliseconds);
            Console.WriteLine("Hello World!");
            File.WriteAllText("./out.json", JsonConvert.SerializeObject(distros, Formatting.Indented));
        }
    }

    public class LinuxDistro
    {
        public string FullName;
        public string OsType;
        public string[] BasedOn;
        public string Origin;
        public string[] Categories;
        public string Description;
        public string[] Architectures;
        public string[] Desktops;
        public string DistributionFullName;
        public string HomePage;
        public string Status;
        public string IconUrl;
        public string ScreenshotSmallUrl;
        public string ScreenshotUrl;
    }

    public static class ExtensionMethod
    {
        public static string[] ToStrings(this MatchCollection matches)
        {
            List<string> list = new();
            foreach (var match in matches)
                list.Add(match.ToString());
            return list.ToArray();
        }
    }
}