namespace DefaultNamespace;

class DataMatcher
{
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
        sbandsegMatch = 9 ,
        titleMatch = 10,
        anneeMatch = 11
    }
    
    public DataMatcher(List<XMLDataEntry> xmlEntries, List<BPDataEntry> bpEntries)
    {
        XmlEntries = xmlEntries;
        BPEntries = bpEntries;
    }

    private List<XMLDataEntry> XmlEntries { get; set; }
    private List<(List<BPDataEntry>, List<XMLDataEntry>)> ProblemMultipleEntries { get; set; }
    private List<BPDataEntry> BPEntries { get; set; }
    private List<BPDataEntry> NewXMLEntriesToAdd { get; set; } = new List<BPDataEntry>();

    public void MatchEntries()
    {
        Console.WriteLine("Starting Entry Checker");
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
            Console.WriteLine($"Entry has exactly 1 entry in the XML, which is a total match. Therefore nothing will be done for entry: {entry}");
            return;
        }

        if (matchingEntries.Count() > 1)
        {
            
        }

        if (matchingEntries.Count() == 1 && !matchingEntries.First().FullMatch(entry))
        {
            HandleNonMatchingEntries(entry, matchingEntries.First());
        }
    }

    enum Commands
    {
        Finished,
        Edit,
        Help,
        Invalid
    }

    private void HandleNonMatchingEntries(BPDataEntry entry, XMLDataEntry matchingEntry)
    {
        var command = Commands.Help;
        var entries = GetComparisonsOfEntriesByLine(entry, matchingEntry);
        PrintNonMatchEntryMenu(entry,matchingEntry, entries);
        PrintCommandMenu(command);
        
    }

    private void PrintCommandMenu(Commands command)
    {
        if (command == Commands.Invalid)
        {
            
        }else if (command == Commands.Finished)
        {
            
        }else if (command == Commands.Edit)
        {
            
        }else if (command == Commands.Help){
            
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

        SetConsoleColour (entriesMatches[(int)Comparisons.bpNumMatch]);
        Console.WriteLine($"| 1    | BPNumber     |     {entry.BPNumber}     |     {matchingEntry.BPNumber}     |");
        
        SetConsoleColour (entriesMatches[(int)Comparisons.indexMatch]);
        Console.WriteLine($"| 2   | Index     |     {entry.Index}     |     {matchingEntry.Index}     |");
        
        SetConsoleColour (entriesMatches[(int)Comparisons.indexBisMatch]);
        Console.WriteLine($"|  3   |IndexBis     |     {entry.IndexBis}     |     {matchingEntry.IndexBis}     |");
        
        SetConsoleColour (entriesMatches[(int)Comparisons.internetMatch]);
        Console.WriteLine($"|  4  | Internet     |     {entry.Internet}     |     {matchingEntry.Internet}     |");
       
        SetConsoleColour (entriesMatches[(int)Comparisons.publicationMatch]); Console.WriteLine($"|     Name     |     {entry.Name}     |     {matchingEntry.Name}     |");
        Console.WriteLine($"|  5 |  Publication     |     {entry.Publication}     |     {matchingEntry.Publication}     |");
        
        SetConsoleColour (entriesMatches[(int)Comparisons.sbandsegMatch]);Console.WriteLine($"|     Resume     |     {entry.Resume}     |     {matchingEntry.Resume}     |");
        Console.WriteLine($"|  6 |  Segs     |     {entry.SBandSEG}     |     {matchingEntry.SBandSEG}     |");
        
        SetConsoleColour (entriesMatches[(int)Comparisons.noMatch]);Console.WriteLine($"|     Title     |     {entry.Title}     |     {matchingEntry.Title}     |");
        Console.WriteLine($"|  7 |  No     |     {entry.No}     |     {matchingEntry.No}     |");
        Console.WriteLine("|-----------------------------------------------------------------|");
    }

    private bool[] GetComparisonsOfEntriesByLine(BPDataEntry entry, XMLDataEntry matchingEntry)
    {
        var matches = new bool[11];
        matches[(int) Comparisons.bpNumMatch] = entry.BPNumber == matchingEntry.BPNumber;
        matches[(int) Comparisons.crMatch] = entry.CR == matchingEntry.CR;
        matches[(int) Comparisons.indexMatch] = entry.Name == matchingEntry.Index;
        matches[(int) Comparisons.indexBisMatch] = entry.Name == matchingEntry.IndexBis;
        matches[(int) Comparisons.internetMatch] = entry.Name == matchingEntry.Internet;
        matches[(int) Comparisons.nameMatch] = entry.Name == matchingEntry.Name;
        matches[(int) Comparisons.publicationMatch] = entry.Name == matchingEntry.Publication;
        matches[(int) Comparisons.resumeMatch] = entry.Name == matchingEntry.Resume;
        matches[(int) Comparisons.sbandsegMatch] = entry.Name == matchingEntry.SBandSEG;
        matches[(int) Comparisons.titleMatch] = entry.Name == matchingEntry.Title;
        matches[(int) Comparisons.anneeMatch] = entry.Name == matchingEntry.Annee;
        matches[(int) Comparisons.noMatch] = entry.No == matchingEntry.No;

        return matches;
    }
}