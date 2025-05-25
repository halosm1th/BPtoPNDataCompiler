using System.Text;
using DefaultNamespace;

// Assuming DefaultNamespace contains BPDataEntry and XMLDataEntry

namespace BPtoPNDataCompiler
{
    /// <summary>
    /// The DataMatcher class is responsible for comparing BPDataEntry objects with XMLDataEntry objects
    /// and identifying matches, new entries, and conflicting entries. It also orchestrates the UI
    /// for resolving non-matching entries.
    /// </summary>
    class DataMatcher
    {
        /// <summary>
        /// Initializes a new instance of the DataMatcher class.
        /// </summary>
        /// <param name="xmlEntries">A list of XMLDataEntry objects to match against.</param>
        /// <param name="bpEntries">A list of BPDataEntry objects to be matched.</param>
        public DataMatcher(List<XMLDataEntry> xmlEntries, List<BPDataEntry> bpEntries)
        {
            Console.WriteLine("Creating Data matcher...");
            XmlEntries = xmlEntries;
            BpEntries = bpEntries;
            Console.WriteLine("Saved the entry lists.");
        }

        // Lists to store different categories of entries after matching process
        public List<BPDataEntry> NewXmlEntriesToAdd { get; } = new List<BPDataEntry>();

        // Updated lists to store detailed update information using the UpdateDetail class
        // This allows tracking which entry, which field, its old value, and its new value.
        public List<UpdateDetail<BPDataEntry>> BpEntriesToUpdate { get; } = new List<UpdateDetail<BPDataEntry>>();
        public List<UpdateDetail<XMLDataEntry>> PnEntriesToUpdate { get; } = new List<UpdateDetail<XMLDataEntry>>();

        private List<XMLDataEntry> XmlEntries { get; set; }
        private List<(List<BPDataEntry>, List<XMLDataEntry>)>? ProblemMultipleEntries { get; set; }
        private List<BPDataEntry> BpEntries { get; set; }

        /// <summary>
        /// Initiates the entry matching process.
        /// Iterates through BP entries and attempts to find corresponding XML entries.
        /// </summary>
        public void MatchEntries()
        {
            Console.WriteLine("Starting Entry Checker");
            Console.WriteLine($"Checking: {BpEntries.Count} entries");

            foreach (var entry in BpEntries)
            {
                Console.Write($"Trying entry: {entry.Title}. ");
                // Check if any weak match or exact BPNumber match exists in XML entries
                if (XmlEntries.Any(x => x.WeakMatch(entry) || x.BPNumber == entry.BPNumber))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("Found potential match.\n");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    HandleMatch(entry);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("No match found, this is a new entry.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    NewXmlEntriesToAdd.Add(entry); // Add to list of new entries if no match
                }
            }
        }

        /// <summary>
        /// Handles a BPDataEntry that has found at least one potential match in XMLDataEntries.
        /// Determines if it's a full match, a non-full match requiring user input, or multiple matches.
        /// </summary>
        /// <param name="entry">The BPDataEntry being processed.</param>
        private void HandleMatch(BPDataEntry entry)
        {
            var matchingEntries = new List<XMLDataEntry>();

            // Collect all XML entries that have the same BPNumber
            if (XmlEntries.Any(x => x.BPNumber == entry.BPNumber))
            {
                matchingEntries.AddRange(XmlEntries.Where(x => x.BPNumber == entry.BPNumber));
            }

            // Collect all XML entries that are weak matches and not already in matchingEntries
            var weakMatches = XmlEntries.Where(x => x.WeakMatch(entry));
            foreach (var weakMatch in weakMatches)
            {
                if (!matchingEntries.Contains(weakMatch))
                {
                    matchingEntries.Add(weakMatch);
                }
            }

            var xmlDataEntries = matchingEntries.ToArray();

            // Case 1: Exactly one match and it's a full match. No action needed.
            if (xmlDataEntries.Length == 1 && xmlDataEntries.First().FullMatch(entry))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(
                    $"{entry.Title} has exactly 1 entry in the XML, which is a total match. Therefore nothing will be done for this entry.");
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }

            // Case 2: Exactly one match, but it's not a full match. User interaction required.
            if (xmlDataEntries.Length == 1 && !xmlDataEntries.First().FullMatch(entry))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Found editable match. Launching conflict resolution UI.");
                Console.ForegroundColor = ConsoleColor.Gray;
                HandleNonMatchingEntries(entry, xmlDataEntries.First());
            }

            // Case 3: More than one match. This indicates a conflict that needs specific handling.
            if (xmlDataEntries.Length > 1)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Found multiple matches. Handling as a conflict.");
                Console.ForegroundColor = ConsoleColor.Gray;
                HandleMultipleMatches(entry, xmlDataEntries);
            }
        }

        /// <summary>
        /// Handles multiple matches where the entries might share the same BPNumber or other conflicting data.
        /// In this scenario, the BPNumber of the original BPDataEntry will be marked with "HAS ERROR".
        /// </summary>
        /// <param name="entry">The original BPDataEntry that has multiple matches.</param>
        /// <param name="xmlDataEntries">An array of XMLDataEntry objects that are conflicting matches.</param>
        private void HandleMultipleMatches(BPDataEntry entry, XMLDataEntry[] xmlDataEntries)
        {
            var shortList = GenerateShortList(entry, xmlDataEntries);

            if (shortList.Count > 1)
            {
                Console.WriteLine(
                    $"The match shortlist has {shortList.Count} conflicting elements with matching BPNumber.");

                // Log the conflicting XML entries for review.
                // For each conflicting XML entry, we add an UpdateDetail.
                // Here, we are just noting their involvement in the conflict without changing their values,
                // but you could modify their BPNumber or other fields if required.
                var sb = new StringBuilder();
                sb.Append("Conflicting PN Matches (BPNumbers): ");
                foreach (var match in shortList)
                {
                    sb.Append($"{match.Title ?? "NULL"} ");
                    // Add an UpdateDetail for each conflicting XML entry.
                    // This creates a record that this PN entry was part of a multiple match conflict.
                    // The NewValue is the same as OldValue here, indicating no direct change to the PN entry itself
                    // in this specific conflict resolution step, but it's flagged for review.
                    PnEntriesToUpdate.RemoveAll(ud =>
                        ud.Entry == match && ud.FieldName == "BPNumber"); // Prevent duplicates
                    PnEntriesToUpdate.Add(new UpdateDetail<XMLDataEntry>(match, "BPNumber", match.BPNumber,
                        match.BPNumber));
                }

                Console.WriteLine(sb.ToString());
            }
        }

        /// <summary>
        /// Generates a shortlist of XMLDataEntry objects that match the BPDataEntry,
        /// primarily focusing on entries with matching BPNumbers.
        /// </summary>
        /// <param name="entry">The BPDataEntry to match against.</param>
        /// <param name="xmlDataEntries">The array of potential XMLDataEntry matches.</param>
        /// <returns>A list of XMLDataEntry objects that are considered strong matches or share BPNumber.</returns>
        private List<XMLDataEntry> GenerateShortList(BPDataEntry entry, XMLDataEntry[] xmlDataEntries)
        {
            var shortList = new List<XMLDataEntry>();

            // Add all XML entries that have the same BPNumber as the current BP entry
            if (xmlDataEntries.Any(x => x.BPNumber == entry.BPNumber))
            {
                var entriesWithMatchingBPNum = xmlDataEntries.Where(x => x.BPNumber == entry.BPNumber);
                Console.WriteLine($"Found {entriesWithMatchingBPNum.Count()} entries with matching BPNumber in XML.");
                shortList.AddRange(entriesWithMatchingBPNum);
            }

            /* The commented-out code below was for strong, medium, and weak matches.
             * It is left commented as per the original file, but if you wish to
             * re-enable broader matching criteria, uncomment and adjust as needed.
            foreach (var match in xmlDataEntries)
            {
                if (match.StrongMatch(entry) && !shortList.Contains(match))
                {
                    Console.WriteLine("strong match");
                    shortList.Add(match);
                }
            }

            if (shortList.Count == 0)
            {
                Console.WriteLine("No strong matches, no direct matches, trying medium matches?");
                foreach (var match in xmlDataEntries)
                {
                    if (match.MediumMatch(entry))
                    {
                        Console.WriteLine("medium match");
                        shortList.Add(match);
                    }
                }
            }

            if (shortList.Count == 0)
            {
                Console.WriteLine("No medium matches, no direct matches, trying weak matches?");
                foreach (var match in xmlDataEntries)
                {
                    if (match.WeakMatch(entry))
                    {
                        Console.WriteLine("weak match");
                        shortList.Add(match);
                    }
                }
            }*/

            return shortList;
        }

        /// <summary>
        /// Delegates to the DataMatcherConflictUI to handle non-matching entries interactively.
        /// </summary>
        /// <param name="entry">The BPDataEntry to resolve.</param>
        /// <param name="matchingEntry">The XMLDataEntry that partially matches.</param>
        private void HandleNonMatchingEntries(BPDataEntry entry, XMLDataEntry matchingEntry)
        {
            var matcherUI = new DataMatcherConflictUI();
            matcherUI.HandleNonMatchingEntries(entry, matchingEntry);

            // Add the detailed update records from the UI to the DataMatcher's lists.
            BpEntriesToUpdate.AddRange(matcherUI.BpEntriesToUpdate);
            PnEntriesToUpdate.AddRange(matcherUI.PnEntriesToUpdate);
        }
    }
}