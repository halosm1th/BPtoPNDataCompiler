using DefaultNamespace;

namespace BPtoPNDataCompiler;

class DataMatcher
{
    public DataMatcher(List<XMLDataEntry> xmlEntries, List<BPDataEntry> bpEntries)
    {
        Console.WriteLine("Creating Data matcher?");
        XmlEntries = xmlEntries;
        BpEntries = bpEntries;
        Console.WriteLine("Saved the entry lists?");
    }

    private List<XMLDataEntry> XmlEntries { get; set; }
    private List<(List<BPDataEntry>, List<XMLDataEntry>)>? ProblemMultipleEntries { get; set; }
    private List<BPDataEntry> BpEntries { get; set; }
    private List<BPDataEntry> NewXmlEntriesToAdd { get; set; } = new List<BPDataEntry>();
    private List<BPDataEntry> BpEntriesToUpdate { get; } = new List<BPDataEntry>(); // New list for BP updates
    private List<XMLDataEntry> PnEntriesToUpdate { get; } = new List<XMLDataEntry>(); // New list for PN updates


    public void MatchEntries()
    {
        Console.WriteLine("Starting Entry Checker");
        Console.WriteLine($"Parsing: {BpEntries.Count} entries");

        foreach (var entry in BpEntries)
        {
            Console.WriteLine($"Trying entry: {entry.Title}");
            if (XmlEntries.Any(x => x.WeakMatch(entry)))
            {
                Console.WriteLine("Found match.");
                HandleMatch(entry);
            }
            else
            {
                Console.WriteLine("No match found, this is a new entry.");
                NewXmlEntriesToAdd.Add(entry);
            }
        }
    }

    private void HandleMatch(BPDataEntry entry)
    {
        Console.WriteLine($"Entry is: {entry.Name}");
        var matchingEntries = XmlEntries.Where(x => x.WeakMatch(entry));
        var xmlDataEntries = matchingEntries as XMLDataEntry[] ?? matchingEntries.ToArray();

        if (xmlDataEntries.Count() == 1 && xmlDataEntries.First().FullMatch(entry))
        {
            Console.WriteLine(
                $"Entry has exactly 1 entry in the XML, which is a total match. Therefore nothing will be done for entry: {entry.Title}");
            return;
        }

        if (xmlDataEntries.Count() > 1)
        {
            HandleMultipleMatches(entry, xmlDataEntries);
        }

        if (xmlDataEntries.Count() == 1 && !xmlDataEntries.First().FullMatch(entry))
        {
            Console.WriteLine("Found editable match");
            //TODO uncomment this
            //HandleNonMatchingEntries(entry, xmlDataEntries.First());
        }
    }

    private void HandleMultipleMatches(BPDataEntry entry, XMLDataEntry[] xmlDataEntries)
    {
        Console.WriteLine("Matched with multiple entries?");
        var shortList = GenerateShortList(entry, xmlDataEntries);

        Console.Write("Matches: ");
        foreach (var match in shortList)
        {
            Console.Write($"{match.Title}, ");
        }
        // TODO: Handle multiple strong matches, e.g., by prompting the user to select one
    }

    private List<XMLDataEntry> GenerateShortList(BPDataEntry entry, XMLDataEntry[] xmlDataEntries)
    {
        var shortList = new List<XMLDataEntry>();
        foreach (var match in xmlDataEntries)
        {
            if (match.StrongMatch(entry))
            {
                Console.WriteLine("strong match");
                shortList.Add(match);
            }
        }

        if (shortList.Count == 0)
        {
            foreach (var match in xmlDataEntries)
            {
                if (match.MediumMatch(entry))
                {
                    Console.WriteLine("medium match");
                    shortList.Add(match);
                }
            }
        }

        return shortList;
    }

    private void HandleNonMatchingEntries(BPDataEntry entry, XMLDataEntry matchingEntry)
    {
        var matcherUI = new DataMatcherConflictUI();
        matcherUI.HandleNonMatchingEntries(entry, matchingEntry);
        BpEntriesToUpdate.AddRange(matcherUI.BpEntriesToUpdate);
        PnEntriesToUpdate.AddRange(matcherUI.PnEntriesToUpdate);
    }
}