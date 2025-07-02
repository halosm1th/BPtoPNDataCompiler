using System.Text.RegularExpressions;
using System.Xml;
using DefaultNamespace;

namespace BPtoPNDataCompiler;

public class CRReviewParser
{
    public CRReviewParser(Logger _logger, List<XMLDataEntry> _entries, string StartingPath)
    {
        logger = _logger;
        PNEntries = _entries;
        PathToJournalCSV = StartingPath;
    }

    private string PathToJournalCSV { get; set; }

    private Logger logger { get; set; }
    private List<XMLDataEntry> PNEntries { get; set; }

    public List<CRReviewData> ParseReviews(List<(BPDataEntry, XMLDataEntry)> CREntries, int lastPN)
    {
        logger.LogProcessingInfo($"Starting to parse {CREntries.Count} reviews.");
        var returnList = new List<CRReviewData>();

        var currentPN = lastPN + 1;
        foreach (var entry in CREntries)
        {
            var results = ParseEntry(entry, currentPN);
            logger.LogProcessingInfo($"finished processing entry {entry.Item1.CR}, returned: {results.Count} reviews");

            Console.WriteLine($"finished processing entry {entry.Item1.CR}, returned: {results.Count} reviews");
            returnList.AddRange(results);
        }


        logger.LogProcessingInfo($"Finished processing reviews, there are {returnList.Count} new entries.");
        return returnList;
    }

    private List<CRReviewData> ParseEntry((BPDataEntry, XMLDataEntry) entryPath, int currentPN)
    {
        var CR = entryPath.Item1.CR;
        var pageRanges = GetPagesRangesfromCR(CR, entryPath.Item2.PNNumber);

        var URlsForReviwsOfXml = GetReviews(entryPath.Item2);
        var reviewEntries = GetEntriesFromURLs(URlsForReviwsOfXml);

        var entriesWithoutReviews = GetNewEntries(pageRanges, reviewEntries);
        var entriesNotInXML = GetNewEntries(entriesWithoutReviews, PNEntries);
        var formattedNewEntries = CreateEntriesFromCR(entriesNotInXML, currentPN);

        return formattedNewEntries;
        throw new NotImplementedException();
    }

    private List<CRReviewData> CreateEntriesFromCR(
        List<(string year, string pageRange, string cr, string articleReviewed)> entriesNotInXml,
        int nextNumb)
    {
        var results = new List<CRReviewData>();
        foreach (var entry in entriesNotInXml)
        {
            results.Add(new CRReviewData(entry.pageRange, entry.year, entry.cr, nextNumb.ToString(), PathToJournalCSV,
                entry.articleReviewed));
            nextNumb++;
        }

        return results;
    }

    private List<(string year, string pageRange, string CR, string articleReviewed)> GetNewEntries(
        List<(string year, string pageRange, string CR, string articleReviewed)> pageRanges,
        List<XMLDataEntry> reviewEntries)
    {
        var entryMatches = new List<(string cr, XMLDataEntry match)>();
        var noMatch = new List<(string year, string pageRange, string cr, string articleReviewed)>();

        foreach (var pageRange in pageRanges)
        {
            if (reviewEntries.Any(x => x.ToString().Contains(pageRange.year)))
            {
                var yearMatches = reviewEntries.Where(x => x.ToString().Contains(pageRange.year));

                if (yearMatches.Any(x => x.ToString().Contains(pageRange.pageRange)))
                {
                    var values = reviewEntries.Where(x => x.ToString().Contains(pageRange.year))
                        .Where(x => x.ToString().Contains(pageRange.pageRange));

                    foreach (var value in values)
                    {
                        entryMatches.Add(new(pageRange.CR ?? "NO CR", value));
                    }
                }
                else
                {
                    noMatch.Add(pageRange);
                }
            }
            else
            {
                noMatch.Add(pageRange);
            }
        }

        return noMatch;
    }

    private List<(string year, string pageRange, string cr, string articleReviewed)> GetPagesRangesfromCR(string? cr,
        string src)
    {
        var pageRanges = new List<(string year, string range, string cr, string articleReviewed)>();
        cr = cr.Replace("C.R. par ", "");
        cr = cr.Replace("C.R. ", "");

        var parts = cr.Split(" - ");

        foreach (var part in parts)
        {
            var yearRegex = new Regex(@"\b(19|20)\d{2}\b");
            var pageWithPPRegex = new Regex(@"((pp.|p. ) \d+-\d+)");
            var pageRegex = new Regex(@"\d+-\d+");
            var year = yearRegex.Match(part);
            var pagesWithPP = pageWithPPRegex.Match(part);
            var pages = pageRegex.Match(pagesWithPP.Value);
            pageRanges.Add((year.Value, pages.Value, part, src));
        }

        return pageRanges;
    }

    private List<XMLDataEntry> GetEntriesFromURLs(List<string> uRlsForReviwsOfXml)
    {
        var entries = new List<XMLDataEntry>();
        foreach (var url in uRlsForReviwsOfXml)
        {
            var text = url.Replace("https://papyri.info/biblio/", "");
            var entry = PNEntries.First(x => x.PNNumber == text);
            entries.Add(entry);
        }

        return entries;
    }

    private List<string> GetReviews(XMLDataEntry entryPathItem2)
    {
        // Initialize a new XmlDocument object.
        var xmlDoc = new XmlDocument();

        // Load the XML file from the path specified in entryPathItem2.PNFileName.
        // This will parse the XML content into a DOM (Document Object Model) tree.
        xmlDoc.Load(entryPathItem2.PNFileName);

        // Initialize a list to store the extracted 'target' attribute values.
        List<string> ptrTargets = new List<string>();

        // Create an XmlNamespaceManager to handle XML namespaces.
        // The XML document uses a default namespace (xmlns="http://www.tei-c.org/ns/1.0").
        // To query elements within this namespace using XPath, we need to associate a prefix
        // with the namespace URI.
        XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);

        // Add the TEI namespace with a chosen prefix (e.g., "tei").
        // This prefix will be used in the XPath expression to correctly identify elements
        // that belong to this namespace.
        nsmgr.AddNamespace("tei", "http://www.tei-c.org/ns/1.0");

        // Select all 'ptr' nodes that are direct children of 'bibl' nodes,
        // which are themselves direct children of 'relatedItem' nodes,
        // anywhere in the document (indicated by '//').
        // The XPath expression now uses the 'tei' prefix for all elements
        // to correctly match elements within the TEI namespace.
        XmlNodeList ptrNodes = xmlDoc.SelectNodes("//tei:relatedItem[@type='reviews']/tei:bibl/tei:ptr", nsmgr);

        if (ptrNodes.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Notice! Document {entryPathItem2.PNFileName} did not have any relatedItem nodes.");
            Console.ResetColor();
        }

        // Iterate through each XmlNode found by the SelectNodes method.
        foreach (XmlNode node in ptrNodes)
        {
            // Ensure the current node is indeed an element and its name is "ptr".
            // This check adds robustness, though SelectNodes should return only elements matching the XPath.
            if (node.NodeType == XmlNodeType.Element && node.Name == "ptr")
            {
                // Check if the "target" attribute exists for the current 'ptr' node.
                if (node.Attributes["target"] != null)
                {
                    // If the "target" attribute exists, retrieve its value
                    // and add it to our list of ptrTargets.
                    ptrTargets.Add(node.Attributes["target"].Value);
                }
            }
        }

        // Return the list containing all collected 'target' attribute values.
        return ptrTargets;
    }

    public void SaveCRReviews(List<CRReviewData> baseText, string savePath)
    {
        //TODO fix this so that it generates XML entries for these things
        //This will require doing a few complex things before running. 
        foreach (var entry in baseText)
        {
            var crEntry = Path.Combine(savePath, $"{entry.IDNumber}.xml");
            Console.WriteLine($"Saving {entry.IDNumber} to {crEntry}");
            logger?.LogProcessingInfo($"Saving {entry.IDNumber} to {crEntry}");

            File.WriteAllText(crEntry, entry.ToString());
        }
    }
}