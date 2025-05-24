namespace DefaultNamespace;

class DataMatcher
{
    public DataMatcher(List<XMLDataEntry> xmlEntries, List<BPDataEntry> bpEntries)
    {
        Console.WriteLine("Creating Data matcher?");
        XmlEntries = xmlEntries;
        BPEntries = bpEntries;
        Console.WriteLine("Saved the entry lists?");
    }

    private List<XMLDataEntry> XmlEntries { get; set; }
    private List<(List<BPDataEntry>, List<XMLDataEntry>)> ProblemMultipleEntries { get; set; }
    private List<BPDataEntry> BPEntries { get; set; }
    private List<BPDataEntry> NewXMLEntriesToAdd { get; set; } = new List<BPDataEntry>();

    public void MatchEntries()
    {
        Console.WriteLine("Starting Entry Checker");
        Console.WriteLine($"Parsing: {BPEntries.Count} entries");

        foreach (var entry in BPEntries)
        {
            Console.WriteLine($"Trying entry: {entry.Name}");
            if (XmlEntries.Any(x => x.AnyMatch(entry)))
            {
                Console.WriteLine("Found match. Rating Quality");
                HandleMatch(entry);
            }
            else
            {
                Console.WriteLine("No match found, this is a new entry.");
                NewXMLEntriesToAdd.Add(entry);
            }
        }
    }

    private void HandleMatch(BPDataEntry entry)
    {
        var matchingEntries = XmlEntries.Where(x => x.AnyMatch(entry));
        if (matchingEntries.Count() == 1 && matchingEntries.First().FullMatch(entry))
        {
            Console.WriteLine(
                $"Entry has exactly 1 entry in the XML, which is a total match. Therefore nothing will be done for entry: {entry}");
            return;
        } else if (matchingEntries.Count() > 1)
        {
            var shortList = new List<XMLDataEntry>();

            foreach (var match in matchingEntries)
            {
                if (match.StrongMatch(entry))
                {
                    Console.WriteLine("strong match");
                    shortList.Add(match);
                }
            }

            Console.WriteLine("Found more than one match");
            foreach (var match in shortList)
            {
                Console.WriteLine($"Match: {match.Name}");
            }
        }

        if (matchingEntries.Count() == 1 && !matchingEntries.First().FullMatch(entry))
        {
            Console.WriteLine("Found editable match");
            HandleNonMatchingEntries(entry, matchingEntries.First());
        }
    }

    private void HandleNonMatchingEntries(BPDataEntry entry, XMLDataEntry matchingEntry)
    {
        try
        {
            var command = Commands.Help;
            var entries = GetComparisonsOfEntriesByLine(entry, matchingEntry);
            PrintNonMatchEntryMenu(entry, matchingEntry, entries);
            PrintCommandMenu(command);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private void PrintCommandMenu(Commands command)
    {
        if (command == Commands.Invalid)
        {
        }
        else if (command == Commands.Finished)
        {
        }
        else if (command == Commands.Edit)
        {
        }
        else if (command == Commands.Help)
        {
        }
    }

    private void SetConsoleColour(bool entryMatches)
    {
        if (entryMatches) Console.ForegroundColor = ConsoleColor.Green;
        else Console.ForegroundColor = ConsoleColor.Red;
    }

    private void PrintNonMatchEntryMenu(BPDataEntry entry, XMLDataEntry matchingEntry, bool[] entriesMatches)
    {
        Console.WriteLine($"|                COLLISION FOUND                                 |");
        Console.WriteLine($"|----------------------------------------------------------------|");
        Console.WriteLine($"| Number |   Category     |     Data from BP     |     Data from PN     |");

        SetConsoleColour(entriesMatches[(int) Comparisons.bpNumMatch]);
        Console.WriteLine($"| 1    | BPNumber     |     {entry.BPNumber}     |     {matchingEntry.BPNumber}     |");

        SetConsoleColour(entriesMatches[(int) Comparisons.indexMatch]);
        Console.WriteLine($"| 2   | Index     |     {entry.Index}     |     {matchingEntry.Index}     |");

        SetConsoleColour(entriesMatches[(int) Comparisons.indexBisMatch]);
        Console.WriteLine($"|  3   |IndexBis     |     {entry.IndexBis}     |     {matchingEntry.IndexBis}     |");

        SetConsoleColour(entriesMatches[(int) Comparisons.internetMatch]);
        Console.WriteLine($"|  4  | Internet     |     {entry.Internet}     |     {matchingEntry.Internet}     |");

        SetConsoleColour(entriesMatches[(int) Comparisons.publicationMatch]);
        Console.WriteLine($"|     Name     |     {entry.Name}     |     {matchingEntry.Name}     |");
        Console.WriteLine(
            $"|  5 |  Publication     |     {entry.Publication}     |     {matchingEntry.Publication}     |");

        SetConsoleColour(entriesMatches[(int) Comparisons.sbandsegMatch]);
        Console.WriteLine($"|     Resume     |     {entry.Resume}     |     {matchingEntry.Resume}     |");
        Console.WriteLine($"|  6 |  Segs     |     {entry.SBandSEG}     |     {matchingEntry.SBandSEG}     |");

        SetConsoleColour(entriesMatches[(int) Comparisons.noMatch]);
        Console.WriteLine($"|     Title     |     {entry.Title}     |     {matchingEntry.Title}     |");
        Console.WriteLine($"|  7 |  No     |     {entry.No}     |     {matchingEntry.No}     |");
        Console.WriteLine("|-----------------------------------------------------------------|");
    }

    private bool[] GetComparisonsOfEntriesByLine(BPDataEntry entry, XMLDataEntry matchingEntry)
    {
        var matches = new bool[12];
        matches[((int) Comparisons.bpNumMatch)] = (entry.HasBPNum && matchingEntry.HasBPNum) && (entry.BPNumber == matchingEntry.BPNumber);
        matches[((int) Comparisons.crMatch)] = (entry.HasCR && matchingEntry.HasCR) && (entry.CR == matchingEntry.CR);
        matches[((int) Comparisons.indexMatch)] = (entry.HasBPNum && matchingEntry.HasIndex) &&  (entry.Index == matchingEntry.Index);
        matches[((int) Comparisons.indexBisMatch)] = (entry.HasBPNum && matchingEntry.HasIndexBis) &&  (entry.IndexBis == matchingEntry.IndexBis);
        matches[((int) Comparisons.internetMatch)] = (entry.HasBPNum && matchingEntry.HasInternet) &&  (entry.Internet == matchingEntry.Internet);
        matches[((int) Comparisons.nameMatch)] = (entry.HasBPNum && matchingEntry.HasName) &&  (entry.Name == matchingEntry.Name);
        matches[((int) Comparisons.publicationMatch)] = (entry.HasPublication && matchingEntry.HasPublication) &&  (entry.Publication == matchingEntry.Publication);
        matches[((int) Comparisons.resumeMatch)] = (entry.HasResume && matchingEntry.HasResume) &&  (entry.Resume == matchingEntry.Resume);
        matches[((int) Comparisons.sbandsegMatch)] = (entry.HasSBandSEG && matchingEntry.HasSBandSEG) &&  (entry.SBandSEG == matchingEntry.SBandSEG);
        matches[((int) Comparisons.titleMatch)] = (entry.HasTitle && matchingEntry.HasTitle) &&  (entry.Title == matchingEntry.Title);
        matches[((int) Comparisons.anneeMatch)] = (entry.HasAnnee && matchingEntry.HasAnnee) &&  (entry.Annee == matchingEntry.Annee);
        matches[((int) Comparisons.noMatch)] = (entry.HasNo && matchingEntry.HasNo) &&  (entry.No == matchingEntry.No);

        return matches;
    }


    enum Comparisons
    {
        bpNumMatch = 0,
        crMatch = 1,
        indexMatch = 2,
        indexBisMatch = 3,
        internetMatch = 4,
        nameMatch = 5,
        noMatch = 6,
        publicationMatch = 7,
        resumeMatch = 8,
        sbandsegMatch = 9,
        titleMatch = 10,
        anneeMatch = 11
    }

    enum Commands
    {
        Finished,
        Edit,
        Help,
        Invalid
    }
}