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
        private int bpEntriesUpdated = 0;
        private int matchMenu = 0;
        private int multipleMatches = 0;
        private int newXmlEntriesAdded = 0;

        public bool NoDataMatcher = false;
        private int noMatch = 0;
        private int perfectMatch = 0;
        private int pnEntriesUpdated = 0;
        private int SharedEntriesUpdated = 0;

        /// <summary>
        /// Initializes a new instance of the DataMatcher class.
        /// </summary>
        /// <param name="xmlEntries">A list of XMLDataEntry objects to match against.</param>
        /// <param name="bpEntries">A list of BPDataEntry objects to be matched.</param>
        public DataMatcher(List<XMLDataEntry> xmlEntries, List<BPDataEntry> bpEntries, Logger? logger,
            bool shouldCompareNames = false, bool noDataMatcher = false)
        {
            logger?.LogProcessingInfo(
                $"Created Data Matcher with {xmlEntries.Count} xml entries and {bpEntries.Count} bp entries.");
            Console.WriteLine("Creating Data matcher...");
            this.logger = logger;
            XmlEntries = xmlEntries;
            BpEntries = bpEntries;
            ShouldCompareNames = shouldCompareNames;
            NoDataMatcher = noDataMatcher;
            ;
        }

        private bool ShouldCompareNames { get; set; }

        private Logger? logger { get; }

        // Lists to store different categories of entries after matching process
        public List<BPDataEntry> NewXmlEntriesToAdd { get; } = new List<BPDataEntry>();

        // Updated lists to store detailed update information using the UpdateDetail class
        // This allows tracking which entry, which field, its old value, and its new value.
        public List<UpdateDetail<BPDataEntry>> BpEntriesToUpdate { get; } = new List<UpdateDetail<BPDataEntry>>();
        public List<UpdateDetail<XMLDataEntry>> PnEntriesToUpdate { get; } = new List<UpdateDetail<XMLDataEntry>>();
        public List<UpdateDetail<BPDataEntry>> SharedEntriesToLog { get; } = new List<UpdateDetail<BPDataEntry>>();

        public List<(BPDataEntry, XMLDataEntry)> CREntriesToUpdate { get; } = new List<(BPDataEntry, XMLDataEntry)>();

        private List<XMLDataEntry> XmlEntries { get; set; }
        private List<BPDataEntry> BpEntries { get; set; }

        /// <summary>
        /// Initiates the entry matching process.
        /// Iterates through BP entries and attempts to find corresponding XML entries.
        /// </summary>
        public void MatchEntries()
        {
            Console.WriteLine("Starting Entry Checker");
            Console.WriteLine($"Checking: {BpEntries.Count} entries");
            logger?.Log(
                $"Starting match checker to compare {BpEntries.Count} entries to {XmlEntries.Count} XML entries.");
            logger?.LogProcessingInfo(
                $"Starting match checker to compare {BpEntries.Count} entries to {XmlEntries.Count} XML entries.");

            foreach (var entry in BpEntries)
            {
                Console.Write($"Trying entry: {entry.Title}. ");
                logger?.LogProcessingInfo($"Trying entry: {entry.Title}.");
                // Check if any weak match or exact BPNumber match exists in XML entries
                if (XmlEntries.Any(x => x.WeakMatch(entry) || x.BPNumber == entry.BPNumber))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("Found potential match.\n");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    logger?.LogProcessingInfo($"Found potential match for {entry.Title}, testing it.");
                    HandleMatch(entry);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("No match found, this is a new entry.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    logger?.LogProcessingInfo($"no match found for {entry.Title}, creating new xml entry.");
                    NewXmlEntriesToAdd.Add(entry); // Add to list of new entries if no match
                    newXmlEntriesAdded++;
                    noMatch++;
                }
            }

            logger?.LogProcessingInfo(
                $"Processed {BpEntries.Count}, resultling in: {bpEntriesUpdated} updates to BP entries, {pnEntriesUpdated} updates to PN entries, {SharedEntriesUpdated} shared entries, and {newXmlEntriesAdded} new XML entries. (perfect match: {perfectMatch}, no match: {noMatch}, multiple match: {multipleMatches}, match menu: {matchMenu})");
            logger?.Log(
                $"Processed {BpEntries.Count}, resultling in: {bpEntriesUpdated} updates to BP entries, {pnEntriesUpdated} updates to PN entries, {SharedEntriesUpdated} shared entries,and {newXmlEntriesAdded} new XML entries. (perfect match: {perfectMatch}, no match: {noMatch}, multiple match: {multipleMatches}, match menu: {matchMenu})");
            Console.WriteLine(
                $"Processed {BpEntries.Count}, resultling in: {bpEntriesUpdated} updates to BP entries, {pnEntriesUpdated} updates to PN entries, {SharedEntriesUpdated} shared entries,and {newXmlEntriesAdded} new XML entries. (perfect match: {perfectMatch}, no match: {noMatch}, multiple match: {multipleMatches}, match menu: {matchMenu})");
        }


        /// <summary>
        /// Handles a BPDataEntry that has found at least one potential match in XMLDataEntries.
        /// Determines if it's a full match, a non-full match requiring user input, or multiple matches.
        /// </summary>
        /// <param name="entry">The BPDataEntry being processed.</param>
        private void HandleMatch(BPDataEntry entry)
        {
            logger?.Log($"Handling match with {entry.Title}");
            logger?.LogProcessingInfo($"Handling match with {entry.Title}");

            var matchingEntries = new List<XMLDataEntry>();

            logger?.LogProcessingInfo(
                $"Checking if any XML entries have a BPNumber which exactly matches this entries bp number ({entry.BPNumber}).");
            // Collect all XML entries that have the same BPNumber
            if (XmlEntries.Any(x => x.BPNumber == entry.BPNumber))
            {
                logger?.LogProcessingInfo("Gathering all xml entries with the same BPNumber as the current entry.");
                var entriesWithMatchingBPNum = XmlEntries.Where(x => x.BPNumber == entry.BPNumber);
                logger?.LogProcessingInfo(
                    $"Gathered {entriesWithMatchingBPNum.Count()} entries that share a BPNumber with {entry.BPNumber}.");
                matchingEntries.AddRange(entriesWithMatchingBPNum);
            }

            // Collect all XML entries that are weak matches and not already in matchingEntries
            var weakMatches = XmlEntries.Where(x => x.WeakMatch(entry, ShouldCompareNames));
            logger?.LogProcessingInfo(
                $"Collecting all XML entries that are weak matches that we had not already found, totalling {weakMatches.Count()}");
            foreach (var weakMatch in weakMatches)
            {
                logger?.LogProcessingInfo(
                    $"Checking if {weakMatch.Title} ({weakMatch.BPNumber} | {weakMatch.PNNumber}) is already in the list of matching entries.");
                if (!matchingEntries.Contains(weakMatch))
                {
                    logger?.LogProcessingInfo(
                        $"{weakMatch.Title} ({weakMatch.BPNumber} | {weakMatch.PNNumber}) is not in the list of matching entries, adding it.");
                    matchingEntries.Add(weakMatch);
                }
            }

            var xmlDataEntries = matchingEntries.ToArray();

            logger?.LogProcessingInfo($"Checking {xmlDataEntries.Length} matching entries against entry ");

            // Case 1: Exactly one match and it's a full match. No action needed.
            if (xmlDataEntries.Length == 1 && xmlDataEntries.First().FullMatch(entry, ShouldCompareNames))
            {
                perfectMatch++;

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(
                    $"{entry.Title} has exactly 1 entry in the XML, which is a total match. Therefore nothing will be done for this entry.");
                Console.ForegroundColor = ConsoleColor.Gray;
                logger?.Log(
                    $"{entry.Title} has exactly 1 entry in the XML, which is a total match. Therefore nothing will be done for this entry.");
                logger?.LogProcessingInfo(
                    $"{entry.Title} has exactly 1 entry in the XML, which is a total match. Therefore nothing will be done for this entry.");
                return;
            }

            // Case 2: Exactly one match, but it's not a full match. User interaction required.
            if (xmlDataEntries.Length == 1 && !xmlDataEntries.First().FullMatch(entry, ShouldCompareNames))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Found editable match. Launching conflict resolution UI.");
                Console.ForegroundColor = ConsoleColor.Gray;
                logger?.LogProcessingInfo("Found editable match. Launching conflict resolution UI.");
                logger?.Log("Found editable match. Launching conflict resolution UI.");
                matchMenu++;
                if (!NoDataMatcher) HandleNonMatchingEntries(entry, xmlDataEntries.First());
            }

            // Case 3: More than one match. This indicates a conflict that needs specific handling.
            if (xmlDataEntries.Length > 1)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Found multiple matches. Handling as a conflict.");
                Console.ForegroundColor = ConsoleColor.Gray;
                logger?.LogProcessingInfo($"Found multiple matches ({xmlDataEntries.Length}). Handling as a conflict.");
                logger?.Log($"Found multiple matches ({xmlDataEntries.Length}). Handling as a conflict.");
                HandleMultipleMatches(entry, xmlDataEntries);
                multipleMatches++;
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
            logger?.LogProcessingInfo(
                $"Processing multiple matches with entry {entry.Title}. Starting by generating the short list of matches");
            var shortList = GenerateShortList(entry, xmlDataEntries);

            if (shortList.Count > 1)
            {
                logger?.LogProcessingInfo(
                    $"Found more than one match by BP. A total of {shortList.Count} entries with matching BP #'s were found.");
                Console.WriteLine(
                    $"The match shortlist has {shortList.Count} conflicting elements with matching BPNumber.");

                // Log the conflicting XML entries for review.
                // For each conflicting XML entry, we add an UpdateDetail.
                // Here, we are just noting their involvement in the conflict without changing their values,
                // but you could modify their BPNumber or other fields if required.
                var sb = new StringBuilder();
                var sbStart = new StringBuilder();
                sbStart.Append($"Conflicting PN Matches (BPNumbers: {entry.BPNumber}");
                foreach (var match in shortList)
                {
                    sb.Append($"{match.Title ?? "NULL"} ");
                    sbStart.Append($" | {match.PNNumber}");
                    // Add an UpdateDetail for each conflicting XML entry.
                    // This creates a record that this PN entry was part of a multiple match conflict.
                    // The NewValue is the same as OldValue here, indicating no direct change to the PN entry itself
                    // in this specific conflict resolution step, but it's flagged for review.
                    var update = new UpdateDetail<XMLDataEntry>(match, "BPNumber", match.BPNumber,
                        (match.BPNumber));
                    PnEntriesToUpdate.RemoveAll(ud =>
                        ud.Entry.PNFileName == match.PNFileName && ud.FieldName == "BPNumber"); // Prevent duplicates
                    PnEntriesToUpdate.Add(update);
                    pnEntriesUpdated++;

                    logger?.Log(
                        $"Added {update.Entry.Title} to the PN entry update list, changing {update.FieldName} from {update.OldValue} to {update.NewValue}.");
                }

                sbStart.Append("): ");
                sb.Append(sb.ToString());

                Console.WriteLine(sbStart.ToString());
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
            logger?.LogProcessingInfo("Generating short list of possible multi-matches");
            var shortList = new List<XMLDataEntry>();

            // Add all XML entries that have the same BPNumber as the current BP entry
            if (xmlDataEntries.Any(x => x.BPNumber == entry.BPNumber))
            {
                var entriesWithMatchingBpNum = xmlDataEntries.Where(x => x.BPNumber == entry.BPNumber);
                Console.WriteLine($"Found {entriesWithMatchingBpNum.Count()} entries with matching BPNumber in XML.");
                logger?.LogProcessingInfo(
                    $"Found {entriesWithMatchingBpNum.Count()} entries with matching BPNumber in XML, adding them to be processed.");
                shortList.AddRange(entriesWithMatchingBpNum);
            }

            logger?.LogProcessingInfo($"Short list is finished, it is composed of: {shortList.Count} elements.");
            return shortList;
        }

        /// <summary>
        /// Delegates to the DataMatcherConflictUI to handle non-matching entries interactively.
        /// </summary>
        /// <param name="entry">The BPDataEntry to resolve.</param>
        /// <param name="matchingEntry">The XMLDataEntry that partially matches.</param>
        private void HandleNonMatchingEntries(BPDataEntry entry, XMLDataEntry matchingEntry)
        {
            logger?.Log(
                $"Creating Datamatcher conflict UI to deal with match between {entry.Title} and {matchingEntry.Title}");
            var matcherUI = new DataMatcherConflictUI(logger, ShouldCompareNames);
            matcherUI.HandleNonMatchingEntries(entry, matchingEntry);

            // Add the detailed update records from the UI to the DataMatcher's lists.
            logger?.Log("finished with Data matcher, adding its lists to the BP and PN entry lists.");
            var mUiBP = matcherUI.BpEntriesToUpdate;
            var mUiPN = matcherUI.PnEntriesToUpdate;
            var MUIShared = matcherUI.SharedList;
            bpEntriesUpdated += mUiBP.Count;
            pnEntriesUpdated += mUiPN.Count;
            SharedEntriesUpdated += MUIShared.Count;
            SharedEntriesToLog.AddRange(MUIShared);
            BpEntriesToUpdate.AddRange(mUiBP);
            PnEntriesToUpdate.AddRange(mUiPN);
            CREntriesToUpdate.AddRange(matcherUI.CREntriesToWorkWith);
        }
    }
}