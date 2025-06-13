using DefaultNamespace;

namespace BPtoPNDataCompiler;

public class DataMatcherConflictUI
{
    // Dictionary to store edited choices for each row, mapping row number to choice (B/P/N/S)
    private Dictionary<int, string?> _editedChoices = new Dictionary<int, string?>();
    private bool ShouldCompareName = false;

    public DataMatcherConflictUI(Logger? logger, bool shouldCompareNames = false)
    {
        logger?.LogProcessingInfo("Creating DataMatcherConflictUI.");
        this.logger = logger;
        ShouldCompareName = shouldCompareNames;
    }

    private List<(List<BPDataEntry>, List<XMLDataEntry>)>? ProblemMultipleEntries { get; set; }

    // Updated lists to store detailed update information using the new UpdateDetail class
    public List<UpdateDetail<BPDataEntry>> BpEntriesToUpdate { get; } = new List<UpdateDetail<BPDataEntry>>();
    public List<UpdateDetail<XMLDataEntry>> PnEntriesToUpdate { get; } = new List<UpdateDetail<XMLDataEntry>>();
    public List<UpdateDetail<BPDataEntry>> SharedList { get; } = new List<UpdateDetail<BPDataEntry>>();

    private Logger? logger { get; }


    /// <summary>
    /// Handles the interactive UI for resolving conflicts between a BPDataEntry and an XMLDataEntry.
    /// </summary>
    /// <param name="entry">The BPDataEntry.</param>
    /// <param name="matchingEntry">The corresponding XMLDataEntry.</param>
    public void HandleNonMatchingEntries(BPDataEntry entry, XMLDataEntry matchingEntry)
    {
        logger?.Log(
            $"Generating the data-matcher UI for handling the conflict between BP {entry.BPNumber} & PN {matchingEntry.PNNumber}");
        Commands command = Commands.Help;
        int? selectedRow = null; // To keep track of the selected row for editing
        _editedChoices.Clear(); // Clear any previous edits for a new entry when starting a new conflict resolution

        while (command != Commands.Finished)
        {
            try
            {
                // Get comparison results for all fields
                var entries = GetComparisonsOfEntriesByLine(entry, matchingEntry);
                // Pass the _editedChoices dictionary to the menu printer to show current edits
                PrintNonMatchEntryMenu(entry, matchingEntry, entries, selectedRow, _editedChoices);
                PrintCommandMenu(command); // This will print the command prompt and help message

                Console.Write("Command: ");
                Console.Out.Flush(); // Ensure prompt is displayed before reading input
                string? input = Console.ReadLine()?.ToLower().Trim();

                logger?.LogProcessingInfo($"User entered {input}, parsing command");
                command = ParseCommand(input);

                switch (command)
                {
                    case Commands.Help:
                        // Help message is already printed by PrintCommandMenu if command was Help
                        // This case just ensures the loop continues
                        logger?.LogProcessingInfo("Help command entered.");
                        break;
                    case Commands.Edit:
                        logger?.LogProcessingInfo("Edit command entered. Starting edit menu.");
                        HandleEditCommand(entry, matchingEntry, entries, ref selectedRow);
                        break;
                    case Commands.Finished:
                        logger?.LogProcessingInfo("Finished command entered.");
                        logger?.LogProcessingInfo("Updated:");
                        logger?.LogProcessingInfo($"\t{entry}\n\t{matchingEntry}");
                        logger?.LogProcessingInfo("Into:");
                        if (BpEntriesToUpdate.Count > 0)
                        {
                            logger?.LogProcessingInfo(
                                $"\t{BpEntriesToUpdate.First().Entry}\n");
                        }

                        if (PnEntriesToUpdate.Count > 0)
                        {
                            logger?.LogProcessingInfo($"\t{PnEntriesToUpdate.First().Entry}");
                        }

                        // Exit loop
                        break;
                    case Commands.Invalid:
                    default:
                        logger?.LogProcessingInfo("Invalid command entered.");
                        Console.WriteLine("Invalid command. Type 'h' or 'help' for options.");
                        break;
                }
            }
            catch (Exception e)
            {
                logger?.LogError("Error in match handling menu, exiting.", e);
                Console.WriteLine($"An error occurred: {e.Message}");
                Console.WriteLine("Press ENTER to continue.");
                Console.ReadLine();
                command = Commands.Invalid; // Reset to invalid to show menu again
            }
        }
    }

    /// <summary>
    /// Handles the 'edit' command, prompting the user for a row number and choice.
    /// </summary>
    /// <param name="bpEntry">The BPDataEntry being edited.</param>
    /// <param name="xmlEntry">The XMLDataEntry being edited.</param>
    /// <param name="entriesMatches">Array indicating which fields match.</param>
    /// <param name="selectedRow">Reference to the currently selected row number.</param>
    private void HandleEditCommand(BPDataEntry bpEntry, XMLDataEntry xmlEntry, bool[] entriesMatches,
        ref int? selectedRow)
    {
        Console.Write("Enter the row number to edit: ");
        // There are 12 potential rows based on the Comparisons enum
        if (int.TryParse(Console.ReadLine(), out int rowNum) && rowNum >= 1 && rowNum <= 12)
        {
            selectedRow = rowNum;
            logger?.LogProcessingInfo($"Selected to edit row {rowNum} ({GetCategoryName(rowNum)}) on {bpEntry.Title}");
            // Print the name of the selected row
            Console.WriteLine(
                $"Selected row {rowNum} [{GetCategoryName(rowNum)}]. Enter 'B' for BP correct, 'P' for PN correct, 'N' for neither, or 'S' for Shared/Both.");
            Console.Write("Choice (B/P/N/S): ");
            string? choice = Console.ReadLine()?.ToUpper().Trim();
            logger?.LogProcessingInfo($"User selected {choice} as choice for which is correct.");

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
                    Console.WriteLine("Entry updated. Press ENTER to continue.");
                    Console.ReadLine(); // Wait for user to press Enter
                }
                else
                {
                    logger?.LogProcessingInfo("Edit cancelled.");
                    Console.WriteLine("Edit cancelled. Press ENTER to continue.");
                    Console.ReadLine();
                }
            }
            else
            {
                logger?.LogProcessingInfo("Invalid choice. Please enter 'B', 'P', 'N', or 'S'.");
                Console.WriteLine("Invalid choice. Please enter 'B', 'P', 'N', or 'S'. Press ENTER to continue.");
                Console.ReadLine();
            }
        }
        else
        {
            logger?.LogProcessingInfo("Invalid row number.");
            Console.WriteLine("Invalid row number. Press ENTER to continue.");
            Console.ReadLine();
        }
    }

    /// <summary>
    /// Gets the category name (field name) based on the row number.
    /// </summary>
    /// <param name="rowNum">The 1-based row number.</param>
    /// <returns>The string representation of the category name.</returns>
    private string GetCategoryName(int rowNum)
    {
        // rowNum directly corresponds to (Comparisons enum value + 1)
        if (rowNum >= 1 && rowNum <= 12)
        {
            return ((Comparisons) (rowNum - 1)).ToString().Replace("Match", "");
        }

        return "Unknown";
    }

    /// <summary>
    /// Updates the internal lists (BpEntriesToUpdate, PnEntriesToUpdate) with detailed change information
    /// based on the user's choice for a specific field.
    /// </summary>
    /// <param name="bpEntry">The BPDataEntry being considered.</param>
    /// <param name="xmlEntry">The XMLDataEntry being considered.</param>
    /// <param name="rowNum">The 1-based row number corresponding to the field being updated.</param>
    /// <param name="choice">The user's choice ('B', 'P', 'N', 'S').</param>
    private void UpdateEntryBasedOnChoice(BPDataEntry bpEntry, XMLDataEntry xmlEntry, int rowNum, string? choice)
    {
        logger?.LogProcessingInfo($"Updating entry based on choice for row {rowNum}...");
        Console.WriteLine($"Updating based on choice for row {rowNum}...");

        // Determine the field name based on rowNum
        string fieldName = GetCategoryName(rowNum);

        string? bpValue = null;
        string? pnValue = null;

        // Use a switch statement to get the current values of the field from both entries
        // This maps the rowNum (derived from Comparisons enum) to the actual property values.
        switch ((Comparisons) (rowNum - 1)) // Convert 1-based rowNum back to 0-based enum value
        {
            case Comparisons.BpNumMatch:
                bpValue = bpEntry.BPNumber;
                pnValue = xmlEntry.BPNumber;
                break;
            case Comparisons.CrMatch:
                bpValue = bpEntry.CR;
                pnValue = xmlEntry.CR;
                break;
            case Comparisons.IndexMatch:
                bpValue = bpEntry.Index;
                pnValue = xmlEntry.Index;
                break;
            case Comparisons.IndexBisMatch:
                bpValue = bpEntry.IndexBis;
                pnValue = xmlEntry.IndexBis;
                break;
            case Comparisons.InternetMatch:
                bpValue = bpEntry.Internet;
                pnValue = xmlEntry.Internet;
                break;
            case Comparisons.NameMatch:
                bpValue = bpEntry.Name;
                pnValue = xmlEntry.Name;
                break;
            case Comparisons.NoMatch:
                bpValue = bpEntry.No;
                pnValue = xmlEntry.No;
                break;
            case Comparisons.PublicationMatch:
                bpValue = bpEntry.Publication;
                pnValue = xmlEntry.Publication;
                break;
            case Comparisons.ResumeMatch:
                bpValue = bpEntry.Resume;
                pnValue = xmlEntry.Resume;
                break;
            case Comparisons.SbandsegMatch:
                bpValue = bpEntry.SBandSEG;
                pnValue = xmlEntry.SBandSEG;
                break;
            case Comparisons.TitleMatch:
                bpValue = bpEntry.Title;
                pnValue = xmlEntry.Title;
                break;
            case Comparisons.AnneeMatch:
                bpValue = bpEntry.Annee;
                pnValue = xmlEntry.Annee;
                break;
            default:
                logger?.LogProcessingInfo($"Warning: Unknown row number {rowNum} for field value extraction.");
                Console.WriteLine($"Warning: Unknown row number {rowNum} for field value extraction.");
                return; // Exit if rowNum is invalid
        }

        // Remove any existing update details for the same entry and field to ensure only the latest choice is kept.
        BpEntriesToUpdate.RemoveAll(ud => ud.Entry == bpEntry && ud.FieldName == fieldName);
        PnEntriesToUpdate.RemoveAll(ud => ud.Entry == xmlEntry && ud.FieldName == fieldName);

        // Add new UpdateDetail objects based on the user's choice
        if (choice == "B") // BP is correct, so PN entry should be updated to match BP's value
        {
            // Only add if there's an actual change needed
            if (pnValue != bpValue)
            {
                PnEntriesToUpdate.Add(new UpdateDetail<XMLDataEntry>(xmlEntry, fieldName, pnValue, bpValue));
                logger?.LogProcessingInfo(
                    $"Added PN update for entry (Title: {xmlEntry.Title ?? "N/A"}) for field '{fieldName}'." +
                    $" Old: '{pnValue ?? "NULL"}', New: '{bpValue ?? "NULL"}'.");
                Console.WriteLine(
                    $"Added PN update for entry (Title: {xmlEntry.Title ?? "N/A"}) for field '{fieldName}'." +
                    $" Old: '{pnValue ?? "NULL"}', New: '{bpValue ?? "NULL"}'.");
            }
            else
            {
                logger?.LogProcessingInfo($"PN field '{fieldName}' is already identical to BP. No update recorded.");
                Console.WriteLine($"PN field '{fieldName}' is already identical to BP. No update recorded.");
            }
        }
        else if (choice == "P") // PN is correct, so BP entry should be updated to match PN's value
        {
            // Only add if there's an actual change needed
            if (bpValue != pnValue)
            {
                BpEntriesToUpdate.Add(new UpdateDetail<BPDataEntry>(bpEntry, fieldName, bpValue, pnValue));
                Console.WriteLine(
                    $"Added BP update for entry (Title: {bpEntry.Title ?? "N/A"}) for field '{fieldName}'. Old: '{bpValue ?? "NULL"}', New: '{pnValue ?? "NULL"}'.");
                logger?.LogProcessingInfo(
                    $"Added BP update for entry (Title: {bpEntry.Title ?? "N/A"}) for field '{fieldName}'. Old: '{bpValue ?? "NULL"}', New: '{pnValue ?? "NULL"}'.");
            }
            else
            {
                logger?.LogProcessingInfo($"BP field '{fieldName}' is already identical to PN. No update recorded.");
                Console.WriteLine($"BP field '{fieldName}' is already identical to PN. No update recorded.");
            }
        }
        else if (choice == "S") // Both are correct/shared. If values differ, record updates to make them consistent.
        {
            if (bpValue != pnValue)
            {
                // Decide which value to standardize on, e.g., BP's value for both
                // For demonstration, let's say we standardize to BP's value for both if they differ.
                // You might need more complex logic here based on your "Shared" definition.
                SharedList.Add(new UpdateDetail<BPDataEntry>(xmlEntry, fieldName, bpValue, pnValue));

                logger?.LogProcessingInfo(
                    $"Added Shared update for field '{fieldName}'. Standardized to BP's value. PN Old: '{pnValue ?? "NULL"}', PN New: '{bpValue ?? "NULL"}'.");
                Console.WriteLine(
                    $"Added Shared update for field '{fieldName}'. Standardized to BP's value. PN Old: '{pnValue ?? "NULL"}', PN New: '{bpValue ?? "NULL"}'.");
            }
            else
            {
                logger?.LogProcessingInfo($"Field '{fieldName}' is already shared and identical. No update needed.");
                Console.WriteLine($"Field '{fieldName}' is already shared and identical. No update needed.");
            }
        }
        else if (choice == "N") // Neither is correct. No update recorded, but ensures previous updates are removed.
        {
            logger?.LogProcessingInfo(
                $"Neither BP nor PN was chosen as correct for field '{fieldName}'. Any previous updates for this field have been removed.");
            Console.WriteLine(
                $"Neither BP nor PN was chosen as correct for field '{fieldName}'. Any previous updates for this field have been removed.");
        }
    }

    /// <summary>
    /// Parses the user's command input.
    /// </summary>
    /// <param name="input">The raw string input from the console.</param>
    /// <returns>The corresponding Commands enum value.</returns>
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

    /// <summary>
    /// Prints the command menu to the console.
    /// </summary>
    /// <param name="command">The current command, used to conditionally print help.</param>
    private void PrintCommandMenu(Commands command)
    {
        if (command == Commands.Help)
        {
            Console.WriteLine("\n--- Commands ---");
            Console.WriteLine("h / help   : Show this help menu");
            Console.WriteLine("e / edit   : Edit a specific row's value");
            Console.WriteLine("f / finished : Finish editing this entry and proceed");
            Console.WriteLine("----------------");
        }
    }

    /// <summary>
    /// Sets the console foreground color based on whether an entry matches.
    /// </summary>
    /// <param name="entryMatches">True if the entries match, false otherwise.</param>
    private void SetConsoleColour(bool entryMatches)
    {
        if (entryMatches) Console.ForegroundColor = ConsoleColor.Green;
        else Console.ForegroundColor = ConsoleColor.Red;
    }

    /// <summary>
    /// Prints the menu displaying non-matching entries and their comparison status.
    /// </summary>
    /// <param name="entry">The BPDataEntry.</param>
    /// <param name="matchingEntry">The XMLDataEntry.</param>
    /// <param name="entriesMatches">Array indicating which fields match.</param>
    /// <param name="selectedRow">The currently selected row for editing (if any).</param>
    /// <param name="editedChoices">Dictionary of user's edited choices for rows.</param>
    private void PrintNonMatchEntryMenu(BPDataEntry entry, XMLDataEntry matchingEntry, bool[] entriesMatches,
        int? selectedRow, Dictionary<int, string?> editedChoices)
    {
        Console.Clear(); // Clear console for cleaner display
        Console.Out.Flush(); // Ensure buffer is truly empty and ready for new output

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

        // Helper function to wrap text for display
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
                if (lastSpace > pos &&
                    lastSpace > pos) // Ensure lastSpace is within the current segment and not at the beginning
                {
                    length = lastSpace - pos + 1;
                }

                string line = text.Substring(pos, length).TrimEnd();
                lines.Add(line);
                pos += length;
            }

            return lines;
        }

        // Helper function to print a wrapped row
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

        // Print rows conditionally based on data presence, using renamed functions
        if (entry.HasBPNum || matchingEntry.HasBPNum)
            PrintWrappedRow(1, "BPNumber", entry.BPNumber, matchingEntry.BPNumber,
                entriesMatches[(int) Comparisons.BpNumMatch], selectedRow == 1,
                _editedChoices.GetValueOrDefault(1) ?? string.Empty);

        if (entry.HasCR || matchingEntry.HasCR)
            PrintWrappedRow(2, "CR", entry.CR, matchingEntry.CR,
                entriesMatches[(int) Comparisons.CrMatch], selectedRow == 2,
                _editedChoices.GetValueOrDefault(2) ?? string.Empty);

        if (entry.HasIndex || matchingEntry.HasIndex)
            PrintWrappedRow(3, "Index", entry.Index, matchingEntry.Index,
                entriesMatches[(int) Comparisons.IndexMatch], selectedRow == 3,
                _editedChoices.GetValueOrDefault(3) ?? string.Empty);

        if (entry.HasIndexBis || matchingEntry.HasIndexBis)
            PrintWrappedRow(4, "IndexBis", entry.IndexBis, matchingEntry.IndexBis,
                entriesMatches[(int) Comparisons.IndexBisMatch], selectedRow == 4,
                _editedChoices.GetValueOrDefault(4) ?? string.Empty);

        if (entry.HasInternet || matchingEntry.HasInternet)
            PrintWrappedRow(5, "Internet", entry.Internet, matchingEntry.Internet,
                entriesMatches[(int) Comparisons.InternetMatch], selectedRow == 5,
                _editedChoices.GetValueOrDefault(5) ?? string.Empty);

        if (entry.HasName || matchingEntry.HasName && ShouldCompareName)
            PrintWrappedRow(6, "Name", entry.Name, matchingEntry.Name, entriesMatches[(int) Comparisons.NameMatch],
                selectedRow == 6, _editedChoices.GetValueOrDefault(6) ?? string.Empty);

        if (entry.HasNo || matchingEntry.HasNo)
            PrintWrappedRow(7, "No", entry.No, matchingEntry.No, entriesMatches[(int) Comparisons.NoMatch],
                selectedRow == 7, _editedChoices.GetValueOrDefault(7) ?? string.Empty);

        if (entry.HasPublication || matchingEntry.HasPublication)
            PrintWrappedRow(8, "Publication", entry.Publication, matchingEntry.Publication,
                entriesMatches[(int) Comparisons.PublicationMatch], selectedRow == 8,
                _editedChoices.GetValueOrDefault(8) ?? string.Empty);

        if (entry.HasResume || matchingEntry.HasResume)
            PrintWrappedRow(9, "Resume", entry.Resume, matchingEntry.Resume,
                entriesMatches[(int) Comparisons.ResumeMatch], selectedRow == 9,
                _editedChoices.GetValueOrDefault(9) ?? string.Empty);

        if (entry.HasSBandSEG || matchingEntry.HasSBandSEG)
            PrintWrappedRow(10, "Segs", entry.SBandSEG, matchingEntry.SBandSEG,
                entriesMatches[(int) Comparisons.SbandsegMatch], selectedRow == 10,
                _editedChoices.GetValueOrDefault(10) ?? string.Empty);

        if (entry.HasTitle || matchingEntry.HasTitle)
            PrintWrappedRow(11, "Title", entry.Title, matchingEntry.Title,
                entriesMatches[(int) Comparisons.TitleMatch], selectedRow == 11,
                _editedChoices.GetValueOrDefault(11) ?? string.Empty);

        // Print the PN file name below the table
        Console.WriteLine($"\nPN File: {matchingEntry.PNFileName}");
    }

    /// <summary>
    /// Compares relevant fields between a BPDataEntry and an XMLDataEntry and returns
    /// an array indicating which fields match.
    /// </summary>
    /// <param name="entry">The BPDataEntry.</param>
    /// <param name="matchingEntry">The XMLDataEntry to compare against.</param>
    /// <returns>A boolean array where true indicates a match for the corresponding field.</returns>
    private bool[] GetComparisonsOfEntriesByLine(BPDataEntry entry, XMLDataEntry matchingEntry)
    {
        var matches = new bool[12]; // Array size matches the number of Comparisons enum members

        // Compare each field, considering nulls and using CheckEquals for strings
        matches[((int) Comparisons.BpNumMatch)] =
            (entry.HasBPNum && matchingEntry.HasBPNum) && (CheckEquals(entry.BPNumber, matchingEntry.BPNumber));
        matches[((int) Comparisons.CrMatch)] =
            (entry.HasCR && matchingEntry.HasCR) && (CheckEquals(entry.CR, matchingEntry.CR));
        matches[((int) Comparisons.IndexMatch)] =
            (entry.HasIndex && matchingEntry.HasIndex) && (CheckEquals(entry.Index, matchingEntry.Index));
        matches[((int) Comparisons.IndexBisMatch)] =
            (entry.HasIndexBis && matchingEntry.HasIndexBis) && (CheckEquals(entry.IndexBis, matchingEntry.IndexBis));
        matches[((int) Comparisons.InternetMatch)] =
            (entry.HasInternet && matchingEntry.HasInternet) && (CheckEquals(entry.Internet, matchingEntry.Internet));
        matches[((int) Comparisons.NameMatch)] =
            (entry.HasName && matchingEntry.HasName) && (CheckEquals(entry.Name, matchingEntry.Name));
        matches[((int) Comparisons.NoMatch)] =
            (entry.HasNo && matchingEntry.HasNo) && (CheckEquals(entry.No, matchingEntry.No));
        matches[((int) Comparisons.PublicationMatch)] =
            (entry.HasPublication && matchingEntry.HasPublication) &&
            (CheckEquals(entry.Publication, matchingEntry.Publication));
        matches[((int) Comparisons.ResumeMatch)] =
            (entry.HasResume && matchingEntry.HasResume) && (CheckEquals(entry.Resume, matchingEntry.Resume));
        matches[((int) Comparisons.SbandsegMatch)] =
            (entry.HasSBandSEG && matchingEntry.HasSBandSEG) && (CheckEquals(entry.SBandSEG, matchingEntry.SBandSEG));
        matches[((int) Comparisons.TitleMatch)] =
            (entry.HasTitle && matchingEntry.HasTitle) && (CheckEquals(entry.Title, matchingEntry.Title));
        matches[((int) Comparisons.AnneeMatch)] =
            (entry.HasAnnee && matchingEntry.HasAnnee) && (CheckEquals(entry.Annee, matchingEntry.Annee));

        return matches;
    }

    /// <summary>
    /// Compares two nullable strings for equality, handling nulls gracefully.
    /// </summary>
    /// <param name="a">The first string.</param>
    /// <param name="b">The second string.</param>
    /// <returns>True if the strings are equal or both null, false otherwise.</returns>
    private bool CheckEquals(string? a, string? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        return a.Equals(b, StringComparison.Ordinal); // Use Ordinal for case-sensitive, culture-insensitive comparison
    }

    /// <summary>
    /// Enum representing the different comparison categories (fields).
    /// </summary>
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

    /// <summary>
    /// Enum representing the available commands in the UI.
    /// </summary>
    enum Commands
    {
        Finished,
        Edit,
        Help,
        Invalid
    }
}