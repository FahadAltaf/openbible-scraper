using CsvHelper;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;

namespace openbible_scraper
{
    public class DataModel
    {
        public string Letter { get; set; }
        public string Topic { get; set; }
        public string SourceReference { get; set; }
        public string UpVotes { get; set; }
        public string Quotation { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            List<string> letters = new List<string> {
           /* "A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V",*/"W","X","Y","Z"
            };

            List<DataModel> entries = new List<DataModel>();
            foreach (var letter in letters)
            {

                try
                {
                    HtmlWeb web = new HtmlWeb();
                    var mainDoc = web.Load($"https://www.openbible.info/topics/{letter}");

                    var section = mainDoc.DocumentNode.SelectSingleNode("//div[@id='suggest']");
                    if (section != null)
                    {
                        var allNodes = section.SelectNodes("//li");
                        foreach (var node in allNodes)
                        {
                            if (node.ChildNodes != null && node.ChildNodes.Count == 1)
                            {
                                var category = HttpUtility.HtmlDecode(node.ChildNodes[0].InnerText);
                                var categoryUrl = "https://www.openbible.info/" + node.ChildNodes[0].Attributes.FirstOrDefault(x => x.Name == "href").Value;
                                Console.WriteLine(categoryUrl);
                                HtmlWeb loadCategory = new HtmlWeb();
                                var doc = loadCategory.Load(categoryUrl);

                                var detailsSection = doc.DocumentNode.SelectSingleNode("//form[@id='vote']");
                                if (detailsSection != null)
                                {
                                    var recordSections = detailsSection.ChildNodes.Where(x => x.Name == "div");
                                    if (recordSections != null && recordSections.Count() > 0)
                                    {
                                        foreach (var recordSection in recordSections)
                                        {
                                            HtmlDocument subDoc = new HtmlDocument();
                                            subDoc.LoadHtml(recordSection.InnerHtml);
                                            DataModel entry = new DataModel { Letter = letter, Topic = category };
                                            var reference = HttpUtility.HtmlDecode(subDoc.DocumentNode.SelectSingleNode("/h3[1]/a[1]").InnerText.Replace("\t", "").Replace("\r", "").Replace("\n", "").Trim());
                                            entry.SourceReference = reference;
                                            var upvote = HttpUtility.HtmlDecode(subDoc.DocumentNode.SelectSingleNode("/h3[1]/span[1]").InnerText.Replace("\t", "").Replace("ESV / ", "").Replace("\r", "").Replace("\n", "").Replace("helpful votes", "").Trim());
                                            entry.UpVotes = upvote;
                                            var quotation = HttpUtility.HtmlDecode(subDoc.DocumentNode.SelectSingleNode("/p[1]").InnerText.Replace("\t", "").Replace("\r", "").Replace("\n", "").Trim());
                                            entry.Quotation = quotation;
                                            entries.Add(entry);
                                        }

                                    }
                                    else
                                        Console.WriteLine($"No record found for {category}");
                                }
                                else
                                    Console.WriteLine($"Section not found for {category}");

                                using (var writer = new StreamWriter($"{letter}.csv"))
                                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                                {
                                    csv.WriteRecords(entries);
                                }
                            }
                            else
                                Console.WriteLine("Category node doesnt have required structure.");
                        }
                    }
                    else
                        Console.WriteLine("Section not found");

                    using (var writer = new StreamWriter($"{letter}.csv"))
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        csv.WriteRecords(entries);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            using (var writer = new StreamWriter($"final.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(entries);
            }
            Console.ReadLine();
        }
    }
}
