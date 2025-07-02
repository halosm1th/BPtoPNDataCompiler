using System.Text.RegularExpressions;

namespace BPtoPNDataCompiler;

public class CRReviewData
{
    public CRReviewData(string pageRange, string year, string cr, string idNumber, string startingPathToCsVv,
        string articleReviewing)
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

        AppearsInID = articleReviewing;
    }

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

    private string GetJournalID(string journal)
    {
        var file = File.ReadAllLines(startingPath + "/PN_Journal_IDs.csv");
        var listOFJournals = new Dictionary<string, string>();
        foreach (var line in file)
        {
            var text = line.Split(',');
            if (!listOFJournals.ContainsKey(text[0])) listOFJournals.Add(text[0], text[1]);
        }

        var id = listOFJournals[journal];

        return id;

        throw new NotImplementedException();
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
                         <ptr target="https://papyri.info/biblio/{JournalID.Replace(" ", "")}"/>
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