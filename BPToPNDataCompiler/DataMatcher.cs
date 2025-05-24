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
            Console.WriteLine($"Trying entry: {entry.Title}");
            if (XmlEntries.Any(x => x.WeakMatch(entry)))
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
        Console.WriteLine($"Entry is: {entry.Name}");
        var matchingEntries = XmlEntries.Where(x => x.WeakMatch(entry));
        if (matchingEntries.Count() == 1 && matchingEntries.First().FullMatch(entry))
        {
            Console.WriteLine(
                $"Entry has exactly 1 entry in the XML, which is a total match. Therefore nothing will be done for entry: {entry.Title}");
            return;
        }
        else if (matchingEntries.Count() > 1)
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

            foreach (var match in shortList)
            {
                Console.WriteLine($"Match: {match.Title}");
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
        int numberWidth = 6;
        int categoryWidth = 12;
        int dataWidth = 40;
        int totalWidth = numberWidth + categoryWidth + dataWidth * 2 + 5;

        string horizontalLine = "+" + new string('-', numberWidth) + "+" + new string('-', categoryWidth) + "+" +
                                new string('-', dataWidth) + "+" + new string('-', dataWidth) + "+";

        string headerText = " COLLISION FOUND ";
        int paddingLeft = (totalWidth - headerText.Length) / 2;
        int paddingRight = totalWidth - headerText.Length - paddingLeft;

        // Always print headers first
        Console.WriteLine(horizontalLine);
        Console.WriteLine(new string(' ', paddingLeft) + headerText + new string(' ', paddingRight));
        Console.WriteLine(horizontalLine);

        Console.WriteLine(
            "|" + "Number".PadRight(numberWidth) +
            "|" + "Category".PadRight(categoryWidth) +
            "|" + "Data from BP".PadRight(dataWidth) +
            "|" + $"Data from PN ({matchingEntry.PNFileName})".PadRight(dataWidth) + "|"
        );
        Console.WriteLine(horizontalLine);

        // Wrapping and printing helpers unchanged
        static List<string> WrapText(string text, int maxWidth)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(text))
            {
                lines.Add("");
                return lines;
            }

            int pos = 0;
            while (pos < text.Length)
            {
                int length = Math.Min(maxWidth, text.Length - pos);
                int lastSpace = text.LastIndexOf(' ', pos + length - 1, length);
                if (lastSpace > pos)
                {
                    length = lastSpace - pos + 1;
                }

                string line = text.Substring(pos, length).TrimEnd();
                lines.Add(line);
                pos += length;
            }

            return lines;
        }

        void PrintWrappedRow(string number, string category, string bpData, string pnData, bool match)
        {
            var bpLines = WrapText(bpData ?? "", dataWidth);
            var pnLines = WrapText(pnData ?? "", dataWidth);
            int maxLines = Math.Max(bpLines.Count, pnLines.Count);

            SetConsoleColour(match);
            for (int i = 0; i < maxLines; i++)
            {
                string numPart = (i == 0) ? number.PadRight(numberWidth) : new string(' ', numberWidth);
                string catPart = (i == 0) ? category.PadRight(categoryWidth) : new string(' ', categoryWidth);
                string bpPart = i < bpLines.Count ? bpLines[i].PadRight(dataWidth) : new string(' ', dataWidth);
                string pnPart = i < pnLines.Count ? pnLines[i].PadRight(dataWidth) : new string(' ', dataWidth);

                Console.WriteLine($"|{numPart}|{catPart}|{bpPart}|{pnPart}|");
            }

            Console.ResetColor();

            Console.WriteLine(horizontalLine);
        }

        bool BothNullOrEmpty(string s1, string s2) => string.IsNullOrWhiteSpace(s1) && string.IsNullOrWhiteSpace(s2);

        // Print only non-empty rows
        if (!BothNullOrEmpty(entry.BPNumber, matchingEntry.BPNumber))
            PrintWrappedRow("01", "BPNumber", entry.BPNumber, matchingEntry.BPNumber,
                entriesMatches[(int) Comparisons.bpNumMatch]);

        if (!BothNullOrEmpty(entry.Index, matchingEntry.Index))
            PrintWrappedRow("02", "Index", entry.Index, matchingEntry.Index,
                entriesMatches[(int) Comparisons.indexMatch]);

        if (!BothNullOrEmpty(entry.IndexBis, matchingEntry.IndexBis))
            PrintWrappedRow("03", "IndexBis", entry.IndexBis, matchingEntry.IndexBis,
                entriesMatches[(int) Comparisons.indexBisMatch]);

        if (!BothNullOrEmpty(entry.Internet, matchingEntry.Internet))
            PrintWrappedRow("04", "Internet", entry.Internet, matchingEntry.Internet,
                entriesMatches[(int) Comparisons.internetMatch]);

        if (!BothNullOrEmpty(entry.Name, matchingEntry.Name))
            PrintWrappedRow("05", "Name", entry.Name, matchingEntry.Name, entriesMatches[(int) Comparisons.nameMatch]);

        if (!BothNullOrEmpty(entry.Publication, matchingEntry.Publication))
            PrintWrappedRow("06", "Publication", entry.Publication, matchingEntry.Publication,
                entriesMatches[(int) Comparisons.publicationMatch]);

        if (!BothNullOrEmpty(entry.Resume, matchingEntry.Resume))
            PrintWrappedRow("07", "Resume", entry.Resume, matchingEntry.Resume,
                entriesMatches[(int) Comparisons.resumeMatch]);

        if (!BothNullOrEmpty(entry.SBandSEG, matchingEntry.SBandSEG))
            PrintWrappedRow("08", "Segs", entry.SBandSEG, matchingEntry.SBandSEG,
                entriesMatches[(int) Comparisons.sbandsegMatch]);

        if (!BothNullOrEmpty(entry.Title, matchingEntry.Title))
            PrintWrappedRow("09", "Title", entry.Title, matchingEntry.Title,
                entriesMatches[(int) Comparisons.titleMatch]);

        if (!BothNullOrEmpty(entry.No, matchingEntry.No))
            PrintWrappedRow("10", "No", entry.No, matchingEntry.No, entriesMatches[(int) Comparisons.noMatch]);
    }


    private bool[] GetComparisonsOfEntriesByLine(BPDataEntry entry, XMLDataEntry matchingEntry)
    {
        var matches = new bool[12];
        matches[((int) Comparisons.bpNumMatch)] =
            (entry.HasBPNum && matchingEntry.HasBPNum) && (entry.BPNumber == matchingEntry.BPNumber);
        matches[((int) Comparisons.crMatch)] = (entry.HasCR && matchingEntry.HasCR) && (entry.CR == matchingEntry.CR);
        matches[((int) Comparisons.indexMatch)] =
            (entry.HasBPNum && matchingEntry.HasIndex) && (entry.Index == matchingEntry.Index);
        matches[((int) Comparisons.indexBisMatch)] = (entry.HasBPNum && matchingEntry.HasIndexBis) &&
                                                     (entry.IndexBis == matchingEntry.IndexBis);
        matches[((int) Comparisons.internetMatch)] = (entry.HasBPNum && matchingEntry.HasInternet) &&
                                                     (entry.Internet == matchingEntry.Internet);
        matches[((int) Comparisons.nameMatch)] =
            (entry.HasBPNum && matchingEntry.HasName) && (entry.Name == matchingEntry.Name);
        matches[((int) Comparisons.publicationMatch)] = (entry.HasPublication && matchingEntry.HasPublication) &&
                                                        (entry.Publication == matchingEntry.Publication);
        matches[((int) Comparisons.resumeMatch)] =
            (entry.HasResume && matchingEntry.HasResume) && (entry.Resume == matchingEntry.Resume);
        matches[((int) Comparisons.sbandsegMatch)] = (entry.HasSBandSEG && matchingEntry.HasSBandSEG) &&
                                                     (entry.SBandSEG == matchingEntry.SBandSEG);
        matches[((int) Comparisons.titleMatch)] = (entry.HasTitle && matchingEntry.HasTitle) &&
                                                  (CheckEquals(entry.Title, matchingEntry.Title));
        matches[((int) Comparisons.anneeMatch)] =
            (entry.HasAnnee && matchingEntry.HasAnnee) && (entry.Annee == matchingEntry.Annee);
        matches[((int) Comparisons.noMatch)] =
            (entry.HasNo && matchingEntry.HasNo) && (CheckEquals(entry.No, matchingEntry.No));

        return matches;
    }

    private bool CheckEquals(string a, string b)
    {
        int index = 0;
        if (a.Length != b.Length) return false;

        for (index = 0; index < a.Length; index++)
        {
            if (a[index] != b[index])
                return false;
        }

        return true;
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