using HtmlAgilityPack;

namespace DefaultNamespace;

public class BPEntryGatherer
{
    private int EndYear;
    private int StartYear;


    public BPEntryGatherer(int startYear, int endYear)
    {
        StartYear = startYear;
        EndYear = endYear;
    }

    private async Task<BPDataEntry?> GetEntry(int currentYear, int currentIndex)
    {
        BPDataEntry? entry = null;
        var yearText = Convert.ToString(currentYear);
        var indexText = Convert.ToString(currentIndex);
        if (indexText.Length < 4)
        {
            for (int i = indexText.Length; i < 4; i++)
            {
                indexText = "0" + indexText;
            }
        }

        var URL = $"https://bibpap.be/BP_enl/?fs=2&n={yearText}-{indexText}";
        var web = new HtmlWeb();
        var htmlDoc = await web.LoadFromWebAsync(URL);

        var table = htmlDoc.DocumentNode.SelectSingleNode("//table[@class='scheda']");

        if (table != null)
        {
            entry = new BPDataEntry($"{yearText}-{indexText}");

            var rowNodes = table.SelectNodes(".//tr");
            rowNodes.RemoveAt(0); //remove the first node, which is the Imprimer cette fiche
            foreach (var node in rowNodes)
            {
                if (node.InnerText.Contains("Indexbis"))
                {
                    Console.WriteLine($"Found an Indexbis @ {URL}");
                }
                else if (node.InnerText.Contains("Index"))
                {
                    var textNode = node.SelectNodes(".//span")[0];
                    entry.Index = textNode.InnerText.Trim();
                }
                else if (node.InnerText.Contains("Titre"))
                {
                    var textNode = node.SelectNodes(".//font")[0];
                    entry.Title = textNode.InnerText.Trim();
                }
                else if (node.InnerText.Contains("Publication"))
                {
                    var textNode = node.SelectNodes(".//font")[0];
                    entry.Publication = textNode.InnerText.Trim();
                }
                else if (node.InnerText.Contains("Résumé"))
                {
                    var textNode = node.SelectNodes(".//font")[0];
                    entry.Resume = textNode.InnerText.Trim();
                }
                else if (node.InnerText.Contains("N°"))
                {
                    var textNode = node.SelectNodes(".//span")[0];
                    entry.No = textNode.InnerText.Trim();
                }
                else if (node.InnerText.Contains("internet"))
                {
                    Console.WriteLine($"Found an internet @ {URL}");
                }
                else if (node.InnerText.Contains("C.R."))
                {
                    var textNode = node.SelectNodes(".//font")[0];
                    entry.CR = textNode.InnerText.Trim();
                }
                else if (node.InnerText.Contains("SBandSEG"))
                {
                    Console.WriteLine($"Found an SBandSEG @ {URL}");
                }
            }
        }
        else
        {
            entry = null;
        }

        return entry;
    }

    private async Task<List<BPDataEntry>> GetEntriesForYear(int currentYear)
    {
        var entries = new List<BPDataEntry>();
        bool hasFailed = false;

        for (int entryIndex = 1; entryIndex <= 9999; entryIndex++)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Gathering entry: {currentYear}-{entryIndex}");
            var entry = await GetEntry(currentYear, entryIndex);

            if (entry == null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(
                    $"Entry {currentYear}-{entryIndex} could not be found. Will try next entry: {!hasFailed}");
                Console.ForegroundColor = ConsoleColor.Gray;
                if (hasFailed)
                {
                    entryIndex = int.MaxValue - 1;
                }
                else
                {
                    hasFailed = true;
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write($"Entry {currentYear}-{entryIndex} was found. ");
                Console.ForegroundColor = ConsoleColor.Gray;
                await WriteEntry(entry);
            }
        }

        return entries;
    }

    private async Task WriteEntry(BPDataEntry entry)
    {
        var fileName = Directory.GetCurrentDirectory() + $"{entry.BPNumber}";
        var xml = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                  $"<bibl xmlns=\"http://www.tei-c.org/ns/1.0\" xml:id=\"{fileName}\" type=\"book\">" +
                  $"{entry.ToXML()}" +
                  $"</bibl>";

        await File.WriteAllTextAsync(fileName, xml);
    }

    private string SetDirectory()
    {
        var currentDir = Directory.GetCurrentDirectory();
        if (currentDir.ToLower().Contains("biblio"))
        {
            Directory.SetCurrentDirectory("..");
        }

        Directory.CreateDirectory("BPXMLFiles");
        Directory.SetCurrentDirectory(Directory.GetCurrentDirectory() + "/BPXMLFiles");
        return currentDir;
    }

    public async Task<List<BPDataEntry>> GatherEntries()
    {
        var entries = new List<BPDataEntry>();
        var currentYear = StartYear;

        var oldDir = SetDirectory();

        var yearRanges = new List<Task<List<BPDataEntry>>>();

        do
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"Gather BP Entries for year: {currentYear}.");
            yearRanges.Add(GetEntriesForYear(currentYear));

            currentYear++;
        } while (currentYear <= EndYear);

        foreach (var range in yearRanges)
        {
            entries.AddRange(await range);
        }

        Directory.SetCurrentDirectory(oldDir);
        return entries;
    }
}