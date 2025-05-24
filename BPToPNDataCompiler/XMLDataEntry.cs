namespace DefaultNamespace;

public class XMLDataEntry : BPDataEntry
{
    public XMLDataEntry(string fileName) : base(null)
    {
        PNFileName = fileName;
    }


    public string PNFileName { get; set; }
    public string PNNumber { get; set; }

    public bool AnyMatch(BPDataEntry entry)
    {
        bool anyMatch = ((entry.HasName && HasName) && (entry.Name == Name))
                        || ((entry.HasInternet && HasInternet) && (entry.Internet == Internet))
                        || ((entry.HasPublication && HasPublication) && (entry.Publication == Publication))
                        || ((entry.HasResume && HasResume) && (entry.Resume == Resume))
                        || ((entry.HasTitle && HasTitle) && (entry.Title == Title))
                        || ((entry.HasIndex && HasIndex) && (entry.Index == Index))
                        || ((entry.HasIndexBis && HasIndexBis) && (entry.IndexBis == IndexBis))
                        || ((entry.HasNo && HasNo) && (entry.No == No))
                        || ((entry.HasCR && HasCR) && (entry.CR == CR))
                        || ((entry.HasBPNum && HasBPNum) && (entry.BPNumber == BPNumber))
                        || ((entry.HasSBandSEG && HasSBandSEG) && (entry.SBandSEG == SBandSEG));

        return anyMatch;
    }

    public bool FullMatch(BPDataEntry entry)
    {
        bool fullMatch = ((entry.HasName && HasName) && (entry.Name == Name))
                         && ((entry.HasInternet && HasInternet) && (entry.Internet == Internet))
                         && ((entry.HasPublication && HasPublication) && (entry.Publication == Publication))
                         && ((entry.HasResume && HasResume) && (entry.Resume == Resume))
                         && ((entry.HasTitle && HasTitle) && (entry.Title == Title))
                         && ((entry.HasIndex && HasIndex) && (entry.Index == Index))
                         && ((entry.HasIndexBis && HasIndexBis) && (entry.IndexBis == IndexBis))
                         && ((entry.HasNo && HasNo) && (entry.No == No))
                         && ((entry.HasCR && HasCR) && (entry.CR == CR))
                         && ((entry.HasBPNum && HasBPNum) && (entry.BPNumber == BPNumber))
                         && ((entry.HasSBandSEG && HasSBandSEG) && (entry.SBandSEG == SBandSEG));

        return fullMatch;
    }

    private bool[] GetComparisonsOfEntriesByLine(BPDataEntry entry)
    {
        var matches = new bool[12];
        matches[((int) Comparisons.bpNumMatch)] = (entry.HasBPNum && HasBPNum) && (entry.BPNumber == BPNumber);
        matches[((int) Comparisons.crMatch)] = (entry.HasCR && HasCR) && (entry.CR == CR);
        matches[((int) Comparisons.indexMatch)] = (entry.HasBPNum && HasIndex) &&  (entry.Index == Index);
        matches[((int) Comparisons.indexBisMatch)] = (entry.HasBPNum && HasIndexBis) &&  (entry.IndexBis == IndexBis);
        matches[((int) Comparisons.internetMatch)] = (entry.HasBPNum && HasInternet) &&  (entry.Internet == Internet);
        matches[((int) Comparisons.nameMatch)] = (entry.HasBPNum && HasName) &&  (entry.Name == Name);
        matches[((int) Comparisons.noMatch)] = (entry.HasNo && HasNo) &&  (entry.No == No);
        matches[((int) Comparisons.publicationMatch)] = (entry.HasPublication && HasPublication) &&  (entry.Publication == Publication);
        matches[((int) Comparisons.resumeMatch)] = (entry.HasResume && HasResume) &&  (entry.Resume == Resume);
        matches[((int) Comparisons.sbandsegMatch)] = (entry.HasSBandSEG && HasSBandSEG) &&  (entry.SBandSEG == SBandSEG);
        matches[((int) Comparisons.titleMatch)] = (entry.HasTitle && HasTitle) &&  (entry.Title == Title);
        matches[((int) Comparisons.anneeMatch)] = (entry.HasAnnee && HasAnnee) &&  (entry.Annee == Annee);

        return matches;
    }

    public bool StrongMatch(BPDataEntry entry)
    {
        try
        {
            var matchStrength = GetComparisonsOfEntriesByLine(entry);
            var truthCount = matchStrength.Aggregate(0, (total, x) => x ? total = total + 1 : total);
            return truthCount >= 3;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return false;
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
}