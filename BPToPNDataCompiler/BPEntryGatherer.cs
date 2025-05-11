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

    private BPDataEntry? GetEntry(int currentYear, int currentIndex)
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
                entry.title = textNode.InnerText.Trim();
            }
            else if (node.InnerText.Contains("Publication"))
            {
                var textNode = node.SelectNodes(".//font")[0];
                entry.publication = textNode.InnerText.Trim();
            }
            else if (node.InnerText.Contains("Résumé"))
            {
                var textNode = node.SelectNodes(".//font")[0];
                entry.resume = textNode.InnerText.Trim();
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
            else if (node.InnerText.Contains("CR"))
            {
                Console.WriteLine($"Found an CR @ {URL}");
            }
            else if (node.InnerText.Contains("SBandSEG"))
            {
                Console.WriteLine($"Found an SBandSEG @ {URL}");
            }
        }

        return entry;
    }

    private List<BPDataEntry> GetEntriesForYear(int currentYear)
    {
        var entries = new List<BPDataEntry>();
        bool hasFailed = false;

        for (int entryIndex = 1; entryIndex <= 9999; entryIndex++)
        {
            var entry = GetEntry(currentYear, entryIndex);
            if (entry == null)
            {
                if (hasFailed)
                {
                    entryIndex = int.MaxValue - 1;
                }
                else
                {
                    hasFailed = true;
                }
            }
        }

        return entries;
    }

    public List<BPDataEntry> GatherEntries()
    {
        var entries = new List<BPDataEntry>();
        var currentYear = StartYear;

        do
        {
            entries.AddRange(GetEntriesForYear(currentYear));


            currentYear++;
        } while (currentYear <= EndYear);

        return entries;
    }
}