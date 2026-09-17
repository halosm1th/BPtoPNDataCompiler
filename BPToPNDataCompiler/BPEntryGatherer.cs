using System.Text.RegularExpressions;
using BPtoPNDataCompiler;
using HtmlAgilityPack;

namespace DefaultNamespace;

public class BPEntryGatherer
{
    // How many times to retry a single BP page request after a network/TLS-level failure
    // (e.g. transient "SSL connection could not be established" errors) before giving up on it.
    private const int MaxNetworkRetries = 3;
    private int EndYear;

    private int ENTRY_END = 9999;
    private int ENTRY_START = 1;
    private int StartYear;

    public BPEntryGatherer(int startYear, int endYear, Logger? logger, int entryStart, int entryEnd)
    {
        logger?.LogProcessingInfo($"Created BPEntryGatherer with start {startYear} and end {endYear}.");
        StartYear = startYear;
        EndYear = endYear;
        this.logger = logger;
        ENTRY_START = entryStart;
        ENTRY_END = entryEnd;
    }

    Logger? logger { get; }

    /// <summary>
    /// Loads a page, retrying on transient network/TLS failures. If every attempt fails, the last
    /// exception is allowed to propagate - callers use that to tell "we couldn't reach the server"
    /// apart from "the server answered and there's no entry here", since those mean very different
    /// things for deciding whether we've reached the end of a year's entries.
    /// </summary>
    private HtmlDocument LoadWithRetry(string url)
    {
        for (var attempt = 1; attempt <= MaxNetworkRetries; attempt++)
        {
            try
            {
                return new HtmlWeb().Load(url);
            }
            catch (Exception e) when (attempt < MaxNetworkRetries)
            {
                logger?.LogProcessingInfo(
                    $"Attempt {attempt}/{MaxNetworkRetries} to load {url} failed ({e.GetType().Name}: {e.Message}); retrying...");
                Thread.Sleep(750 * attempt);
            }
        }

        // Unreachable: the final attempt above either returns or its exception (uncaught, since the
        // `when` guard is false on the last iteration) propagates out of the method.
        throw new InvalidOperationException("Unreachable");
    }

    /// <summary>
    /// Fetches and parses a single BP entry. Throws if the page could not be retrieved at all after
    /// retries (a network/TLS problem - the entry's existence is unknown); returns null only when the
    /// page loaded successfully but has no "scheda" table (a confirmed "this entry doesn't exist").
    /// </summary>
    private BPDataEntry? GetEntry(int currentYear, int currentIndex)
    {
        logger?.LogProcessingInfo("Getting BP entry from BP website.");
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

        // HTTPS to bibpap.be currently fails the TLS handshake outright (confirmed independent of this
        // app - reproduces with plain PowerShell requests and in a real browser, "SSL received a record
        // that exceeded the maximum permissible length"), while the same page loads fine over plain
        // HTTP. Using HTTP here until bibpap.be's TLS setup is fixed on their end.
        var URL = $"http://bibpap.be/BP_enl/?fs=2&n={yearText}-{indexText}";
        var htmlDoc = LoadWithRetry(URL);

        try
        {
            var table = htmlDoc.DocumentNode.SelectSingleNode("//table[@class='scheda']");

            if (table != null)
            {
                //logger.LogProcessingInfo("Found table, creating BPDataEntry.");
                entry = new BPDataEntry($"{yearText}-{indexText}", logger);

                var rowNodes = table.SelectNodes(".//tr");
                rowNodes?.RemoveAt(0); //remove the first node, which is the Imprimer cette fiche
                if (rowNodes != null)
                    foreach (var node in rowNodes)
                    {
                        //TODO 1932-0019 cehck why index bis is hitting for index
                        //TODO check this  over again and again
                        if (node.ChildNodes.First(x => x.Name == "td").InnerText.Contains("Index bis"))
                        {
                            var textNode = node.SelectNodes(".//span")?[0];
                            var text = textNode?.InnerText.Trim();
                            if (text != null) entry.IndexBis = Regex.Replace(text, @"\s{2,}", " ");
                        }
                        else if (node.ChildNodes.First(x => x.Name == "td").InnerText.Contains("Index"))
                        {
                            var textNode = node.SelectNodes(".//span")?[0];
                            if (textNode != null)
                            {
                                var text = textNode.InnerText.Trim();
                                entry.Index = Regex.Replace(text, @"\s{2,}", " ");
                            }
                        }
                        else if (node.ChildNodes.First(x => x.Name == "td").InnerText.Contains("Titre"))
                        {
                            var textNode = node.SelectNodes(".//font")?[0];
                            if (textNode != null)
                            {
                                var text = textNode.InnerText.Trim();
                                entry.Title = Regex.Replace(text, @"\s{2,}", " ");
                            }
                        }
                        else if (node.ChildNodes.First(x => x.Name == "td").InnerText.Contains("Publication"))
                        {
                            var textNode = node.SelectNodes(".//font")?[0];
                            if (textNode != null)
                                entry.Publication = Regex.Replace(textNode.InnerText.Trim(), @"\s{2,}", " ");
                        }
                        else if (node.ChildNodes.First(x => x.Name == "td").InnerText.Contains("Résumé"))
                        {
                            var textNode = node.SelectNodes(".//font")?[0];
                            if (textNode != null)
                                entry.Resume = Regex.Replace(textNode.InnerText.Trim(), @"\s{2,}", " ");
                        }
                        else if (node.ChildNodes.First(x => x.Name == "td").InnerText.Contains("N°"))
                        {
                            var textNode = node.SelectNodes(".//span")?[0];
                            if (textNode != null) entry.No = Regex.Replace(textNode.InnerText.Trim(), @"\s{2,}", " ");
                        }
                        else if (node.ChildNodes.First(x => x.Name == "td").InnerText.Contains("Internet") ||
                                 node.InnerText.Contains("internet"))
                        {
                            var textNode = node.SelectNodes(".//a")?[0];
                            if (textNode != null)
                                entry.Internet = Regex.Replace(textNode.InnerText.Trim(), @"\s{2,}", " ");
                        }
                        else if (node.ChildNodes.First(x => x.Name == "td").InnerText.Contains("S.B. &amp; S.E.G."))
                        {
                            var textNode = node.SelectNodes(".//font")?[0];
                            if (textNode != null)
                                entry.sbSeg = Regex.Replace(textNode.InnerText.Trim(), @"\s{2,}", " ");
                        }
                        else if (node.ChildNodes.First(x => x.Name == "td").InnerText.Contains("C.R."))
                        {
                            var textNode = node.SelectNodes(".//font")?[0];
                            if (textNode != null) entry.CR = Regex.Replace(textNode.InnerText.Trim(), @"\s{2,}", " ");
                        }
                    }
            }
            else
            {
                logger?.LogProcessingInfo("Could not find table");
                entry = null;
            }

            if (entry != null)
            {
                logger?.LogProcessingInfo($"Found entry: {entry.Title} {entry.Publication}");
            }
            else
            {
                logger?.LogProcessingInfo("No entry found");
            }

            return entry;
        }
        catch (Exception e)
        {
            logger?.LogError($"Error in gathering BP entry {currentYear}-{currentIndex}", e);
            Console.WriteLine(e);
        }

        return null;
    }

    private List<BPDataEntry> GetEntriesForYear(int currentYear, string saveLocation)
    {
        // NOTE: this was briefly parallelized (a few concurrent requests per batch) to speed up
        // scraping, but the exact same TLS failure (see LoadWithRetry) reproduced with plain
        // sequential requests too, so concurrency wasn't the cause - reverted anyway since a batch
        // failing together could still have caused a spurious "end of year" stop.
        logger?.LogProcessingInfo(
            $"Getting entries for the year {currentYear}, starting at {ENTRY_START} to {ENTRY_END}");
        bool hasFailed = false;
        var Entries = new List<BPDataEntry>();

        for (int entryIndex = ENTRY_START; entryIndex <= ENTRY_END; entryIndex++)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Gathering entry: {currentYear}-{entryIndex}");
            //logger.LogProcessingInfo($"Gathering Entry: {currentYear}-{entryIndex}");
            BPDataEntry? entry = null;
            var isNetworkFailure = false;
            try
            {
                entry = GetEntry(currentYear, entryIndex);
            }
            catch (Exception e)
            {
                // GetEntry only throws when the page couldn't be retrieved at all after retries -
                // that tells us nothing about whether this entry exists, so it must NOT be treated
                // the same as a confirmed "no entry here" for the end-of-year detection below.
                isNetworkFailure = true;
                logger?.LogError(
                    $"Could not reach the server for entry {currentYear}-{entryIndex} after retries; skipping it without counting it toward end-of-year detection.",
                    e);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(
                    $"Entry {currentYear}-{entryIndex} could not be verified (network/SSL error, see log) - not treated as proof the year is over. Consider re-checking this number.");
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            if (isNetworkFailure)
            {
                continue;
            }

            if (entry == null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(
                    $"Entry {currentYear}-{entryIndex} could not be found. Will try next entry: {!hasFailed}");
                Console.ForegroundColor = ConsoleColor.Gray;
                //logger.LogProcessingInfo(
                //    $"Could not find entry {currentYear}-{entryIndex}, will try next entry? {!hasFailed}");
                if (hasFailed)
                {
                    entryIndex = int.MaxValue - 1;
                }
                else
                {
                    hasFailed = true;
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write($"Entry {currentYear}-{entryIndex} was found. ");
                Console.ForegroundColor = ConsoleColor.Gray;
                // logger.LogProcessingInfo(
                //     $"Found entry {currentYear}-{entryIndex}. Writing entry to disk and adding to entry list.");
                WriteEntry(entry, saveLocation);
                Entries.Add(entry);
            }
        }

        logger?.LogProcessingInfo($"Found a total of {Entries.Count} BP entries for year {currentYear}.");
        return Entries;
    }

    private void WriteEntry(BPDataEntry entry, string path)
    {
        var fileName = path + $"/{entry.BPNumber}.xml";
        //logger.LogProcessingInfo($"Writing {entry.Title} to {fileName}");

        var xml = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                  //Change to idno tupe
                  $"<bibl xmlns=\"http://www.tei-c.org/ns/1.0\" xml:id=\"b{fileName}\" type=\"book\">\n" +
                  $"{entry.ToXML()}" +
                  $"\n</bibl>";

        File.WriteAllText(fileName, xml);
    }

    private string SetDirectory(string depthLevel)
    {
        logger?.LogProcessingInfo("Setting directory for saving Bp Entries");

        var currentDir = Directory.GetCurrentDirectory() + depthLevel + "/";
        if (currentDir.ToLower().Contains("biblio"))
        {
            logger?.LogProcessingInfo("Was in biblio directory, moving to parent directory");
            Directory.SetCurrentDirectory("..");
        }

        if (currentDir.ToLower().Contains("BPXMLFiles"))
        {
            logger?.LogProcessingInfo("Was in BPXMLFiles directory, moving to parent directory");
            Directory.SetCurrentDirectory("..");
        }

        if (Directory.Exists(currentDir + "/BPXMLFiles"))
        {
            logger?.LogProcessingInfo("Found BPXMLFiles, changing to that directory");
        }
        else
        {
            logger?.LogProcessingInfo("Could not find BPXMLFiles, creating and changing to that directory");
            Directory.CreateDirectory(currentDir + "BPXMLFiles");
        }

        Directory.SetCurrentDirectory(currentDir + "/BPXMLFiles");

        return currentDir;
    }

    public List<BPDataEntry> GatherEntries(string depthLevel)
    {
        logger?.Log("Beginning to gather BP Entries.");
        logger?.LogProcessingInfo("Beginning to gather BP Entries.");

        var entries = new List<BPDataEntry>();
        try
        {
            var currentYear = StartYear;

            logger?.LogProcessingInfo("Setting directory for saving BP Entries.");
            var oldDir = SetDirectory(depthLevel);


            do
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"Gathering BP Entries for year: {currentYear}.");
                logger?.LogProcessingInfo($"Beginning to gather BP Entries for year: {currentYear}.");

                //TODO fix the directory here to be one sanitized path
                foreach (var entry in GetEntriesForYear(currentYear, Directory.GetCurrentDirectory()))
                {
                    logger?.LogProcessingInfo($"Adding {entry.Title} to BPentry list");
                    entries.Add(entry);
                }

                currentYear++;
            } while (currentYear <= EndYear);

            Directory.SetCurrentDirectory(oldDir);
        }
        catch (Exception e)
        {
            logger?.LogError("There was an error collecting BP entries", e);
            Console.WriteLine(e);
        }

        logger?.LogProcessingInfo($"Gathered {entries.Count} BP entries for processing.");
        logger?.Log($"Gathered {entries.Count} BP entries.");
        return entries;
    }
}