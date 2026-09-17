using System.Text;
using System.Text.RegularExpressions;
using DefaultNamespace;

namespace BPtoPNDataCompiler;

public class CRReviewData
{
    public CRReviewData(string pageRange, string year, string cr, string idNumber, string startingPathToCsVv,
        string articleReviewing, Logger _logger)
    {
        //Thomas Schmidt, MusHelv 68 (2011) pp. 232-233.
        //Lajos Berkes, Gnomon 85 (2013) pp. 464-466.
        IDNumber = idNumber;
        startingPath = startingPathToCsVv;
        CRData = cr;

        var name = cr.Split(",")[0];
        var nameParts = name.Split(" ");
        Forename = nameParts[0];
        var lastName = "";
        for (int i = 1; i < nameParts.Length; i++)
        {
            lastName += nameParts[i] + " ";
        }

        Lastname = lastName.Remove(lastName.Length - 1, 1);
        Date = year;
        var pages = pageRange.Split("-");
        PageStart = pages[0];
        PageEnd = pages[1];

        var issueRegex = new Regex(@" \d+ ");
        var issueMatch = issueRegex.Match(cr);
        Issue = issueMatch.Value;

        var journal = cr.Split(",")[^1];
        journal = journal.Split(year)[0].Replace("(", "");
        journal = journal.Replace(Issue, "").Trim();

        JournalID = GetJournalID(journal);

        if (journal == "-1")
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"The reviews for {articleReviewing} may need to be created manually");
            Console.ResetColor();
            logger.Log($"The reviews for {articleReviewing} may need to be created manually");
        }

        AppearsInID = articleReviewing;
        logger = _logger;
    }

    private Logger logger { get; }

    private string startingPath { get; set; }


    public string IDNumber { get; } = "[NONE]";
    public string Forename { get; } = "[NONE]";
    public string Lastname { get; } = "[NONE]";
    public string Issue { get; } = "[NONE]";
    public string JournalID { get; } = "[NONE]";
    public string AppearsInID { get; } = "[NONE]";
    public string CRData { get; } = "[NONE]";
    public string Date { get; } = "[NONE]";
    public string PageStart { get; } = "[NONE]";
    public string PageEnd { get; } = "[NONE]";


    private static Dictionary<string, string> _LongJournalTitles { get; set; }
    private static Dictionary<string, string> _ShortJournalTitles { get; set; }

    private string GetJournalID(string journal)
    {
        logger.LogProcessingInfo($"Getting journal ID for: {journal} in {CRData}");
        try
        {
            if (journal.Contains("-") || journal.Contains("p.") || string.IsNullOrEmpty(journal))
            {
                Console.WriteLine($"There was an error parsing the journal for: {CRData}");
                logger?.LogProcessingInfo($"\tThere was an error parsing the journal for: {CRData}");
            }
            else
            {
                if (journal == "")
                {
                    Console.WriteLine("no journal given");
                    logger?.LogProcessingInfo("\tno journal given");
                }

                var listOFJournals = GetJounrals();

                try
                {
                    if (journal != " " && !string.IsNullOrEmpty(journal))
                    {
                        if (journal.Contains(","))
                        {
                            journal = journal.Replace(",", "");
                        }

                        var id = "";
                        var checkText = " 3e s.";


                        if (journal.Contains(" 3e s.")) id = GetJournalIDWithCheckText(" 3e s.", journal);
                        else if (journal.Contains(" 4e s.")) id = GetJournalIDWithCheckText(" 4e s.", journal);
                        else if (journal.Contains("N.S.")) id = GetJournalIDWithCheckText("N.S.", journal);
                        else
                        {
                            id = JournalInJournalList(journal);
                            if (string.IsNullOrEmpty(id))
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"ERROR Could not find journal: {journal}");
                                logger?.Log($"ERROR Could not find journal: {journal}");
                                Console.ResetColor();
                            }
                        }


                        logger.LogProcessingInfo($"\tFound ID {id}.");
                        return id;
                    }
                    else
                    {
                        Console.WriteLine("No journal name found?");
                        logger?.LogProcessingInfo("\tNo journal name found?");
                        return "-1";
                    }
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"had an error parsing journal: {journal}: {e}");
                    Console.ResetColor();
                    logger?.LogError($"had an error parsing journal: {journal}", e);
                    return "-1";
                }
            }

            return "-1";
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Could not find a journal to match {journal}. Threw error: {e}");
            logger?.LogProcessingInfo($"\tCould not find a journal to match {journal}. Threw error: {e}");
            Console.ResetColor();
            return "-1";
        }

        throw new NotImplementedException();
    }

    private string? JournalInJournalList(string journal)
    {
        if (_ShortJournalTitles.Any(x => x.Key == journal))
        {
            return _ShortJournalTitles[journal];
        }

        if (_LongJournalTitles.Any(x => x.Key == journal))
        {
            return _LongJournalTitles[journal];
        }

        return null;
    }

    private string? GetJournalIDWithCheckText(string checkText, string journal)
    {
        if (journal.Contains(checkText))
        {
            var jTitle = JournalInJournalList(journal);
            if (jTitle != null) return jTitle;


            var shortName = journal.Replace(checkText, "").Trim();
            return JournalInJournalList(shortName);
        }

        return null;
    }

    private (Dictionary<string, string>, Dictionary<string, string>) GetJounrals()
    {
        if (_ShortJournalTitles == null || _ShortJournalTitles.Count == 0)
        {
            var file = File.ReadAllLines(Directory.GetCurrentDirectory() + "/PN_Journal_IDs.csv");
            var listOFShortJournals = new Dictionary<string, string>();
            var listOFLongJournals = new Dictionary<string, string>();
            foreach (var line in file)
            {
                var text = SplitCsvLine(line);
                if (text.Count < 3) continue;

                // Use indexer assignment (not Add) since some short/long titles repeat across rows.
                listOFShortJournals[text[2]] = text[2];
                listOFLongJournals[text[1]] = text[1];
            }

            _ShortJournalTitles = listOFShortJournals;
            _LongJournalTitles = listOFLongJournals;
        }

        return (_ShortJournalTitles, _LongJournalTitles);
    }

    /// <summary>
    /// Splits a CSV line on commas, respecting double-quoted fields that may contain commas of their own
    /// (several long journal titles in PN_Journal_IDs.csv do, e.g. "Atti della Accademia ..., Lettere ed Arti di Napoli").
    /// </summary>
    private static List<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }


    public override string ToString()
    {
        return $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <bibl xmlns="http://www.tei-c.org/ns/1.0" xml:id="b{IDNumber}" type="review">
                  <author>
                      <forename>{Forename}</forename>
                      <surname>{Lastname}</surname>
                   </author>
                   <date>{Date}</date>
                  <biblScope type="pp" from="{PageStart}" to="{PageEnd}">{PageStart}-{PageEnd}</biblScope>
                  <relatedItem type="appearsIn">
                      <bibl>
                         <ptr target="https://papyri.info/biblio/{JournalID}"/>
                         <!--ignore - start, i.e. SoSOL users may not edit this-->
                         <!--ignore - stop-->
                      </bibl>
                  </relatedItem>
                  <biblScope type="issue">{Issue}</biblScope>
                  <relatedItem type="reviews" n="1">
                      <bibl>
                         <ptr target="https://papyri.info/biblio/{AppearsInID}"/>
                         <!--ignore - start, i.e. SoSOL users may not edit this-->
                         <!--ignore - stop-->
                      </bibl>
                  </relatedItem>
                  <idno type="pi">{IDNumber}</idno>
                  <seg type="original" subtype="cr" resp="#BP">{CRData}</seg>
                </bibl>
                """;
    }
}