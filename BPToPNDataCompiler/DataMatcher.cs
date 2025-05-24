using DefaultNamespace;

namespace BPtoPNDataCompiler;

class DataMatcher
{
    // New dictionary to store edited choices for each row
    private Dictionary<int, string?> _editedChoices = new Dictionary<int, string?>();

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
    private List<BPDataEntry> BpEntriesToUpdate { get; set; } = new List<BPDataEntry>(); // New list for BP updates
    private List<XMLDataEntry> PnEntriesToUpdate { get; set; } = new List<XMLDataEntry>(); // New list for PN updates


    public void MatchEntries()
    {
        Console.WriteLine("Starting Entry Checker");
        Console.WriteLine($"Parsing: {BpEntries.Count} entries");

        foreach (var entry in BpEntries)
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
        else if (xmlDataEntries.Count() > 1)
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

            foreach (var match in shortList)
            {
                Console.WriteLine($"Match: {match.Title}");
            }
            // TODO: Handle multiple strong matches, e.g., by prompting the user to select one
        }

        if (xmlDataEntries.Count() == 1 && !xmlDataEntries.First().FullMatch(entry))
        {
            Console.WriteLine("Found editable match");
            HandleNonMatchingEntries(entry, xmlDataEntries.First());
        }
    }

    private void HandleNonMatchingEntries(BPDataEntry entry, XMLDataEntry matchingEntry)
    {
        Commands command = Commands.Help;
        int? selectedRow = null; // To keep track of the selected row for editing
        _editedChoices.Clear(); // Clear any previous edits for a new entry

        while (command != Commands.Finished)
        {
            try
            {
                var entries = GetComparisonsOfEntriesByLine(entry, matchingEntry);
                // Pass the _editedChoices dictionary to the menu printer
                PrintNonMatchEntryMenu(entry, matchingEntry, entries, selectedRow, _editedChoices);
                PrintCommandMenu(command); // This will print the command prompt

                Console.Write("Command: ");
                Console.Out.Flush(); // Ensure prompt is displayed before reading input
                string? input = Console.ReadLine()?.ToLower().Trim();

                command = ParseCommand(input);

                switch (command)
                {
                    case Commands.Help:
                        // Help message is already printed by PrintCommandMenu (if command was Help)
                        // This case just ensures the loop continues
                        break;
                    case Commands.Edit:
                        HandleEditCommand(entry, matchingEntry, entries, ref selectedRow);
                        break;
                    case Commands.Finished:
                        // Exit loop
                        break;
                    case Commands.Invalid:
                        Console.WriteLine("Invalid command. Type 'h' or 'help' for options.");
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                command = Commands.Invalid; // Reset to invalid to show menu again
            }
        }
    }

    private void HandleEditCommand(BPDataEntry bpEntry, XMLDataEntry xmlEntry, bool[] entriesMatches,
        ref int? selectedRow)
    {
        Console.Write("Enter the row number to edit: ");
        // Now there are 12 potential rows based on the Comparisons enum
        if (int.TryParse(Console.ReadLine(), out int rowNum) && rowNum >= 1 && rowNum <= 12)
        {
            selectedRow = rowNum;
            // Print the name of the selected row
            Console.WriteLine(
                $"Selected row {rowNum} [{GetCategoryName(rowNum)}]. Enter 'B' for BP correct, 'P' for PN correct, 'N' for neither, or 'S' for Shared/Both.");
            Console.Write("Choice (B/P/N/S): ");
            string? choice = Console.ReadLine()?.ToUpper().Trim();

            if (choice == "B" || choice == "P" || choice == "N" || choice == "S")
            {
                Console.WriteLine(
                    $"Mark row {rowNum} [{GetCategoryName(rowNum)}] as {(choice == "B" ? "BP" : (choice == "P" ? "PN" : (choice == "S" ? "Shared/Both" : "Neither")))} being the correct choice: y/n?");
                Console.Write("Confirm (y/n): ");
                string? confirm = Console.ReadLine()?.ToLower().Trim();

                if (confirm == "y")
                {
                    UpdateEntryBasedOnChoice(bpEntry, xmlEntry, rowNum, choice);
                    _editedChoices[rowNum] = choice; // Store the edited choice
                    selectedRow = null; // Deselect row after update
                    Console.WriteLine("Entry updated. Press ENTER to continue."); // Changed prompt
                    Console.ReadLine(); // Just use ReadLine to wait for user to press Enter
                }
                else
                {
                    Console.WriteLine("Edit cancelled.");
                }
            }
            else
            {
                Console.WriteLine("Invalid choice. Please enter 'B', 'P', 'N', or 'S'.");
            }
        }
        else
        {
            Console.WriteLine("Invalid row number.");
        }
    }

    private string GetCategoryName(int rowNum)
    {
        // rowNum directly corresponds to (Comparisons enum value + 1)
        if (rowNum >= 1 && rowNum <= 12)
        {
            return ((Comparisons) (rowNum - 1)).ToString().Replace("Match", "");
        }

        return "Unknown";
    }

    private void UpdateEntryBasedOnChoice(BPDataEntry bpEntry, XMLDataEntry xmlEntry, int rowNum, string? choice)
    {
        Console.WriteLine($"Updating based on choice for row {rowNum}...");

        // Remove from both lists first to ensure clean state before re-adding
        if (BpEntriesToUpdate.Contains(bpEntry))
        {
            BpEntriesToUpdate.Remove(bpEntry);
            Console.WriteLine($"Removed BP entry (Title: {bpEntry.Title}) from BPEntriesToUpdate.");
        }

        if (PnEntriesToUpdate.Contains(xmlEntry))
        {
            PnEntriesToUpdate.Remove(xmlEntry);
            Console.WriteLine($"Removed PN entry (Title: {xmlEntry.Title}) from PNEntriesToUpdate.");
        }

        if (choice == "B") // BP is correct, so PN entry should be updated
        {
            if (!PnEntriesToUpdate.Contains(xmlEntry))
            {
                PnEntriesToUpdate.Add(xmlEntry);
                Console.WriteLine($"Added PN entry (Title: {xmlEntry.Title}) to PNEntriesToUpdate.");
            }
        }
        else if (choice == "P") // PN is correct, so BP entry should be updated
        {
            if (!BpEntriesToUpdate.Contains(bpEntry))
            {
                BpEntriesToUpdate.Add(bpEntry);
                Console.WriteLine($"Added BP entry (Title: {bpEntry.Title}) to BPEntriesToUpdate.");
            }
        }
        else if (choice == "S") // Both are correct/shared
        {
            if (!BpEntriesToUpdate.Contains(bpEntry))
            {
                BpEntriesToUpdate.Add(bpEntry);
                Console.WriteLine($"Added BP entry (Title: {bpEntry.Title}) to BPEntriesToUpdate (Shared).");
            }

            if (!PnEntriesToUpdate.Contains(xmlEntry))
            {
                PnEntriesToUpdate.Add(xmlEntry);
                Console.WriteLine($"Added PN entry (Title: {xmlEntry.Title}) to PNEntriesToUpdate (Shared).");
            }
        }
        else if (choice == "N") // Neither is correct
        {
            Console.WriteLine("Neither BP nor PN was chosen as correct for this field, removed from update lists.");
        }
    }


    private Commands ParseCommand(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return Commands.Invalid;

        switch (input)
        {
            case "h":
            case "help":
                return Commands.Help;
            case "e":
            case "edit":
                return Commands.Edit;
            case "f":
            case "finished":
                return Commands.Finished;
            default:
                return Commands.Invalid;
        }
    }


    private void PrintCommandMenu(Commands command)
    {
        // This method will now primarily print the prompt for the user
        // and a help message if the 'help' command was entered.

        if (command == Commands.Help)
        {
            Console.WriteLine("\n--- Commands ---");
            Console.WriteLine("h / help   : Show this help menu");
            Console.WriteLine("e / edit   : Edit a specific row's value");
            Console.WriteLine("f / finished : Finish editing this entry and proceed");
            Console.WriteLine("----------------");
        }
    }

    private void SetConsoleColour(bool entryMatches)
    {
        if (entryMatches) Console.ForegroundColor = ConsoleColor.Green;
        else Console.ForegroundColor = ConsoleColor.Red;
    }

    private void PrintNonMatchEntryMenu(BPDataEntry entry, XMLDataEntry matchingEntry, bool[] entriesMatches,
        int? selectedRow, Dictionary<int, string?> editedChoices) // Added editedChoices parameter
    {
        Console.Clear(); // Clear console for cleaner display
        Console.Out.Flush(); // Added flush after clear to ensure buffer is truly empty and ready for new output

        int numberWidth = 6;
        int categoryWidth = 12;
        int dataWidth = 40;
        int statusWidth = 3; // Width for the status column (checkmark, X, B/P/N, or arrow)
        int totalWidth = numberWidth + categoryWidth + dataWidth * 2 + 5 + statusWidth; // Adjusted total width

        string horizontalLine = "+" + new string('-', numberWidth) + "+" + new string('-', categoryWidth) + "+" +
                                new string('-', dataWidth) + "+" + new string('-', dataWidth) + "+" +
                                new string('-', statusWidth) + "+"; // Adjusted line

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
            "|" + $"Data from PN".PadRight(dataWidth) + // Removed filename from header to place below
            "|" + "Stat".PadRight(statusWidth) + "|" // New header for status
        );
        Console.WriteLine(horizontalLine);
        Console.Out.Flush(); // Explicitly flush the console output buffer

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

        void PrintWrappedRow(int rowNumber, string category, string? bpData, string? pnData, bool match,
            bool isSelected,
            string editedStatus)
        {
            var bpLines = WrapText(bpData ?? "", dataWidth);
            var pnLines = WrapText(pnData ?? "", dataWidth);
            int maxLines = Math.Max(bpLines.Count, pnLines.Count);

            SetConsoleColour(match);
            for (int i = 0; i < maxLines; i++)
            {
                string numPart =
                    (i == 0) ? rowNumber.ToString("D2").PadRight(numberWidth) : new string(' ', numberWidth);
                string catPart = (i == 0) ? category.PadRight(categoryWidth) : new string(' ', categoryWidth);
                string bpPart = i < bpLines.Count ? bpLines[i].PadRight(dataWidth) : new string(' ', dataWidth);
                string pnPart = i < pnLines.Count ? pnLines[i].PadRight(dataWidth) : new string(' ', dataWidth);

                string statusPart;
                if (i == 0) // Only display status on the first line of a wrapped row
                {
                    if (isSelected)
                    {
                        statusPart = " <--"; // Arrow for currently selected row
                    }
                    else if (!string.IsNullOrEmpty(editedStatus))
                    {
                        statusPart = $" {editedStatus} "; // Display edited choice (B/P/N/S)
                    }
                    else if (match)
                    {
                        statusPart = " ✓ "; // Checkmark for match
                    }
                    else
                    {
                        statusPart = " X "; // X for unedited difference
                    }
                }
                else
                {
                    statusPart = new string(' ', statusWidth); // Empty for subsequent wrapped lines
                }


                Console.WriteLine($"|{numPart}|{catPart}|{bpPart}|{pnPart}|{statusPart}|");
            }

            Console.ResetColor();

            Console.WriteLine(horizontalLine);
        }

        // Renamed local functions to avoid ambiguity
        bool HasStringData(string s1, string s2) => !string.IsNullOrWhiteSpace(s1) || !string.IsNullOrWhiteSpace(s2);
        bool HasBoolData(bool b1, bool b2) => b1 || b2; // For boolean Has properties
        bool HasNullableIntData(int? i1, int? i2) => i1.HasValue || i2.HasValue; // For nullable int


        // Print rows conditionally based on data presence, using renamed functions
        if (entry.BPNumber != null && matchingEntry.BPNumber != null &&
            HasStringData(entry.BPNumber, matchingEntry.BPNumber))
            PrintWrappedRow(1, "BPNumber", entry.BPNumber, matchingEntry.BPNumber,
                entriesMatches[(int) Comparisons.BpNumMatch], selectedRow == 1,
                _editedChoices.GetValueOrDefault(1) ?? string.Empty);

        if (matchingEntry.CR != null && entry.CR != null && HasStringData(entry.CR, matchingEntry.CR))
            PrintWrappedRow(2, "CR", entry.CR, matchingEntry.CR,
                entriesMatches[(int) Comparisons.CrMatch], selectedRow == 2,
                _editedChoices.GetValueOrDefault(2) ?? string.Empty);

        if (entry.Index != null && matchingEntry.Index != null && HasStringData(entry.Index, matchingEntry.Index))
            PrintWrappedRow(3, "Index", entry.Index, matchingEntry.Index,
                entriesMatches[(int) Comparisons.IndexMatch], selectedRow == 3,
                _editedChoices.GetValueOrDefault(3) ?? string.Empty);

        if (entry.IndexBis != null && matchingEntry.IndexBis != null &&
            HasStringData(entry.IndexBis, matchingEntry.IndexBis))
            PrintWrappedRow(4, "IndexBis", entry.IndexBis, matchingEntry.IndexBis,
                entriesMatches[(int) Comparisons.IndexBisMatch], selectedRow == 4,
                _editedChoices.GetValueOrDefault(4) ?? string.Empty);

        if (matchingEntry.Internet != null && entry.Internet != null &&
            HasStringData(entry.Internet, matchingEntry.Internet))
            PrintWrappedRow(5, "Internet", entry.Internet, matchingEntry.Internet,
                entriesMatches[(int) Comparisons.InternetMatch], selectedRow == 5,
                _editedChoices.GetValueOrDefault(5) ?? string.Empty);

        if (entry.Name != null && matchingEntry.Name != null && HasStringData(entry.Name, matchingEntry.Name))
            PrintWrappedRow(6, "Name", entry.Name, matchingEntry.Name, entriesMatches[(int) Comparisons.NameMatch],
                selectedRow == 6, _editedChoices.GetValueOrDefault(6) ?? string.Empty);

        if (entry.No != null && HasStringData(entry.No, matchingEntry.No)) // Moved 'No' to after 'Name'
            PrintWrappedRow(7, "No", entry.No, matchingEntry.No, entriesMatches[(int) Comparisons.NoMatch],
                selectedRow == 7, _editedChoices.GetValueOrDefault(7) ?? string.Empty);

        if (entry.Publication != null && matchingEntry.Publication != null &&
            HasStringData(entry.Publication, matchingEntry.Publication))
            PrintWrappedRow(8, "Publication", entry.Publication, matchingEntry.Publication,
                entriesMatches[(int) Comparisons.PublicationMatch], selectedRow == 8,
                _editedChoices.GetValueOrDefault(8) ?? string.Empty);

        if (entry.Resume != null && matchingEntry.Resume != null && HasStringData(entry.Resume, matchingEntry.Resume))
            PrintWrappedRow(9, "Resume", entry.Resume, matchingEntry.Resume,
                entriesMatches[(int) Comparisons.ResumeMatch], selectedRow == 9,
                _editedChoices.GetValueOrDefault(9) ?? string.Empty);

        if (matchingEntry.SBandSEG != null && entry.SBandSEG != null &&
            HasStringData(entry.SBandSEG, matchingEntry.SBandSEG))
            PrintWrappedRow(10, "Segs", entry.SBandSEG, matchingEntry.SBandSEG,
                entriesMatches[(int) Comparisons.SbandsegMatch], selectedRow == 10,
                _editedChoices.GetValueOrDefault(10) ?? string.Empty);

        if (matchingEntry.Title != null && entry.Title != null && HasStringData(entry.Title, matchingEntry.Title))
            PrintWrappedRow(11, "Title", entry.Title, matchingEntry.Title,
                entriesMatches[(int) Comparisons.TitleMatch], selectedRow == 11,
                _editedChoices.GetValueOrDefault(11) ?? string.Empty);

        // Print the PN file name below the table
        Console.WriteLine($"\nPN File: {matchingEntry.PNFileName}");
    }


    private bool[] GetComparisonsOfEntriesByLine(BPDataEntry entry, XMLDataEntry matchingEntry)
    {
        var matches = new bool[12];
        matches[((int) Comparisons.BpNumMatch)] =
            (entry.HasBPNum && matchingEntry.HasBPNum) && (entry.BPNumber == matchingEntry.BPNumber);
        matches[((int) Comparisons.CrMatch)] = (entry.HasCR && matchingEntry.HasCR) && (entry.CR == matchingEntry.CR);
        matches[((int) Comparisons.IndexMatch)] =
            (entry.HasBPNum && matchingEntry.HasIndex) && (entry.Index == matchingEntry.Index);
        matches[((int) Comparisons.IndexBisMatch)] = (entry.HasBPNum && matchingEntry.HasIndexBis) &&
                                                     (entry.IndexBis == matchingEntry.IndexBis);
        matches[((int) Comparisons.InternetMatch)] = (entry.HasBPNum && matchingEntry.HasInternet) &&
                                                     (entry.Internet == matchingEntry.Internet);
        matches[((int) Comparisons.NameMatch)] =
            (entry.HasBPNum && matchingEntry.HasName) && (entry.Name == matchingEntry.Name);
        matches[((int) Comparisons.PublicationMatch)] = (entry.HasPublication && matchingEntry.HasPublication) &&
                                                        (entry.Publication == matchingEntry.Publication);
        matches[((int) Comparisons.ResumeMatch)] =
            (entry.HasResume && matchingEntry.HasResume) && (entry.Resume == matchingEntry.Resume);
        matches[((int) Comparisons.SbandsegMatch)] = (entry.HasSBandSEG && matchingEntry.HasSBandSEG) &&
                                                     (entry.SBandSEG == matchingEntry.SBandSEG);
        if (entry.Title != null)
            if (matchingEntry.Title != null)
                matches[((int) Comparisons.TitleMatch)] = (entry.HasTitle && matchingEntry.HasTitle) &&
                                                          (CheckEquals(entry.Title, matchingEntry.Title));
        matches[((int) Comparisons.AnneeMatch)] =
            (entry.HasAnnee && matchingEntry.HasAnnee) && (entry.Annee == matchingEntry.Annee);
        matches[((int) Comparisons.NoMatch)] =
            (entry.HasNo && matchingEntry.HasNo) && (CheckEquals(entry.No, matchingEntry.No));

        return matches;
    }

    private bool CheckEquals(string? a, string? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (a.Length != b.Length) return false;

        for (int index = 0; index < a.Length; index++)
        {
            if (a[index] != b[index])
                return false;
        }

        return true;
    }


    enum Comparisons
    {
        BpNumMatch = 0,
        CrMatch = 1,
        IndexMatch = 2,
        IndexBisMatch = 3,
        InternetMatch = 4,
        NameMatch = 5,
        NoMatch = 6,
        PublicationMatch = 7,
        ResumeMatch = 8,
        SbandsegMatch = 9,
        TitleMatch = 10,
        AnneeMatch = 11
    }

    enum Commands
    {
        Finished,
        Edit,
        Help,
        Invalid
    }
}