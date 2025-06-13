using System.Text.RegularExpressions;
using BPtoPNDataCompiler;
using HtmlAgilityPack;

namespace DefaultNamespace;

public class BPEntryGatherer
{
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

    private BPDataEntry? GetEntry(int currentYear, int currentIndex)
    {
        try
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

            var URL = $"https://bibpap.be/BP_enl/?fs=2&n={yearText}-{indexText}";
            //logger.LogProcessingInfo($"URl for BP Request: {URL}");
            var web = new HtmlWeb();
            var htmlDoc = web.Load(URL);
            //logger.LogProcessingInfo("Loaded URL from web, parsing HTML.");

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
                                entry.SBandSEG = Regex.Replace(textNode.InnerText.Trim(), @"\s{2,}", " ");
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
            try
            {
                entry = GetEntry(currentYear, entryIndex);
            }
            catch (Exception e)
            {
                logger?.LogError($"Could not gather entry {currentYear}-{entryIndex}", e);
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