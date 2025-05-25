using HtmlAgilityPack;

namespace DefaultNamespace;

public class BPEntryGatherer
{
    //TODO restore to proper numbers
    private const int ENTRY_START = 80; // 0
    private const int ENTRY_END = 80; //9999
    private int EndYear;
    private int StartYear;


    public BPEntryGatherer(int startYear, int endYear)
    {
        StartYear = startYear;
        EndYear = endYear;
    }

    private BPDataEntry? GetEntry(int currentYear, int currentIndex)
    {
        try
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
            var htmlDoc = web.Load(URL);


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
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return null;
    }

    private List<BPDataEntry> GetEntriesForYear(int currentYear)
    {
        bool hasFailed = false;
        var Entries = new List<BPDataEntry>();

        //TODO fix entryIndex <= to 9999
        for (int entryIndex = ENTRY_START; entryIndex <= ENTRY_END; entryIndex++)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Gathering entry: {currentYear}-{entryIndex}");
            BPDataEntry? entry = null;
            try
            {
                entry = GetEntry(currentYear, entryIndex);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

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
                WriteEntry(entry);
                Entries.Add(entry);
            }
        }

        return Entries;
    }

    private async Task WriteEntry(BPDataEntry entry)
    {
        var fileName = Directory.GetCurrentDirectory() + $"/{entry.BPNumber}.xml";
        var xml = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                  $"<bibl xmlns=\"http://www.tei-c.org/ns/1.0\" xml:id=\"{fileName}\" type=\"book\">\n" +
                  $"{entry.ToXML()}" +
                  $"\n</bibl>";

        await File.WriteAllTextAsync(fileName, xml);
    }

    private string SetDirectory()
    {
        var currentDir = Directory.GetCurrentDirectory();
        if (currentDir.ToLower().Contains("biblio"))
        {
            Directory.SetCurrentDirectory("..");
        }

        if (currentDir.ToLower().Contains("BPXMLFiles"))
        {
            Directory.SetCurrentDirectory("..");
        }

        if (Directory.Exists(currentDir + "/BPXMLFiles"))
        {
            Directory.SetCurrentDirectory(Directory.GetCurrentDirectory() + "/BPXMLFiles");
            //currentDir = Directory.GetCurrentDirectory() + "/BPXMLFiles";
        }
        else
        {
            Directory.CreateDirectory("BPXMLFiles");
            Directory.SetCurrentDirectory(Directory.GetCurrentDirectory() + "/BPXMLFiles");
            //currentDir = Directory.GetCurrentDirectory() + "/BPXMLFiles";
        }

        return currentDir;
    }

    public List<BPDataEntry> GatherEntries()
    {
        var entries = new List<BPDataEntry>();
        try
        {
            var currentYear = StartYear;

            var oldDir = SetDirectory();


            do
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"Gathering BP Entries for year: {currentYear}.");

                foreach (var entry in GetEntriesForYear(currentYear))
                {
                    entries.Add(entry);
                }

                currentYear++;
            } while (currentYear <= EndYear);

            Directory.SetCurrentDirectory(oldDir);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return entries;
    }
}