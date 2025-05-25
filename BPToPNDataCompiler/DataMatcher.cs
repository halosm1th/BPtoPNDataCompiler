using System.Text;
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
        Console.WriteLine($"Checking: {BpEntries.Count} entries");

        foreach (var entry in BpEntries)
        {
            Console.Write($"Trying entry: {entry.Title}. ");
            if (XmlEntries.Any(x => x.WeakMatch(entry)))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Found weak match.\n");
                Console.ForegroundColor = ConsoleColor.Gray;
                HandleMatch(entry);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No match found, this is a new entry.");
                Console.ForegroundColor = ConsoleColor.Gray;
                NewXmlEntriesToAdd.Add(entry);
            }
        }
    }

    private void HandleMatch(BPDataEntry entry)
    {
        var matchingEntries = new List<XMLDataEntry>();
        if (XmlEntries.Any(x => x.BPNumber == entry.BPNumber))
        {
            matchingEntries.AddRange(XmlEntries.Where(x => x.BPNumber == entry.BPNumber));
        }

        var weakMatches = XmlEntries.Where(x => x.WeakMatch(entry));
        foreach (var weakMatch in weakMatches)
        {
            if (!matchingEntries.Contains(weakMatch))
            {
                matchingEntries.Add(weakMatch);
            }
        }

        var xmlDataEntries = matchingEntries.ToArray();

        if (xmlDataEntries.Count() == 1 && xmlDataEntries.First().FullMatch(entry))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(
                $"{entry.Title} has exactly 1 entry in the XML, which is a total match. Therefore nothing will be done for this entry.");
            Console.ForegroundColor = ConsoleColor.Gray;
            return;
        }

        if (xmlDataEntries.Count() > 1)
        {
            HandleMultipleMatches(entry, xmlDataEntries);
        }

        if (xmlDataEntries.Count() == 1 && !xmlDataEntries.First().FullMatch(entry))
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Found editable match");
            Console.ForegroundColor = ConsoleColor.Gray;
            //TODO uncomment this
            //HandleNonMatchingEntries(entry, xmlDataEntries.First());
        }
    }

    private void HandleMultipleMatches(BPDataEntry entry, XMLDataEntry[] xmlDataEntries)
    {
        var shortList = GenerateShortList(entry, xmlDataEntries);
        Console.WriteLine($"The match shortlist has {shortList.Count} elements");


        var sb = new StringBuilder();
        sb.Append("Matches: ");
        foreach (var match in shortList)
        {
            sb.Append($"{match.Title}, ");
        }

        Console.WriteLine(sb.ToString());

        Console.WriteLine("Best Match:");

        (XMLDataEntry match, int strength) strongestMatch = (null, -1);

        List<(XMLDataEntry match, int strength)> StrongMatchList = new List<(XMLDataEntry match, int strength)>();

        foreach (var match in shortList)
        {
            var strength = match.GetMatchStrength(entry);

            StrongMatchList.Add((match, strength));
            if (strength > strongestMatch.strength)
            {
                strongestMatch.match = match;
                strongestMatch.strength = strength;
            }
        }

        var index = 0;
        Console.WriteLine($"Comparing matches against: {entry.Title}");
        foreach (var match in StrongMatchList.OrderBy(x => x.strength))
        {
            Console.WriteLine($"{index}) {match.match.Title} (str {match.strength})");

            index++;
        }

        Console.WriteLine("Enter number for strongest match: ");
        var numb = Console.ReadLine();
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