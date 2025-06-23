using DefaultNamespace;

namespace BPtoPNDataCompiler;

// ReSharper disable once InconsistentNaming
public class XMLDataEntry : BPDataEntry
{
    public XMLDataEntry(string fileName, Logger? logger) : base(null, logger)
    {
        PNFileName = fileName;
    }

    public new bool HasNo => HasBPNum;
    public new string No => BPNumber ?? "0000-0000";


    public string Note { get; set; }

    public string PNFileName { get; set; }
    public string? PNNumber { get; set; }

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


    public override string ToString()
    {
        return $"{Name ?? ""} {Internet ?? ""} {Publication ?? ""} " +
               $"{Resume ?? ""} {Title ?? ""} {Index ?? ""} {IndexBis ?? ""} " +
               $"{No} {CR ?? ""} {BPNumber ?? ""} {SBandSEG ?? ""}";
    }

    public bool FullMatch(BPDataEntry entry, bool shouldCompareNames)
    {
        Logger?.LogProcessingInfo($"Checking if {entry.Title} is a full match for {this.Title}");
        //If they both have share name, then shareName is equal if the names are equal.
        //If they both don't have share name, then they share in not having a name

        var shareName = HasName switch
        {
            true when entry.HasName => Name == entry.Name,
            false when !entry.HasName => true,
            _ => false
        };

        if (!shouldCompareNames)
        {
            shareName = true;
        }

        //Same for internet
        var shareNet = HasInternet switch
        {
            true when entry.HasInternet => Internet == entry.Internet,
            false when !entry.HasInternet => true,
            _ => false
        };

        //Same for internet
        var sharePub = HasPublication switch
        {
            true when entry.HasPublication => CheckEquals(Publication ?? "", entry.Publication ?? ""),
            false when !entry.HasPublication => true,
            _ => false
        };

        //Same for internet
        var shareRes = HasResume switch
        {
            true when entry.HasResume => Resume == entry.Resume,
            false when !entry.HasResume => true,
            _ => false
        };

        //Same for internet
        var shareTitle = HasTitle switch
        {
            true when entry.HasTitle => Title == entry.Title,
            false when !entry.HasTitle => true,
            _ => false
        };

        //Same for internet
        var shareIndex = HasIndex switch
        {
            true when entry.HasIndex => Index == entry.Index,
            false when !entry.HasIndex => true,
            _ => false
        };

        //Same for internet
        var shareIndexBis = HasIndexBis switch
        {
            true when entry.HasIndexBis => IndexBis == entry.IndexBis,
            false when !entry.HasIndexBis => true,
            _ => false
        };

        //Same for internet
        var shareNo = HasNo switch
        {
            true when entry.HasNo => No == entry.No,
            false when !entry.HasNo => true,
            _ => false
        };

        //Same for internet
        // ReSharper disable once InconsistentNaming
        var shareCR = HasCR switch
        {
            true when entry.HasCR => CR == entry.CR,
            false when !entry.HasCR => true,
            _ => false
        };

        //Same for internet
        // ReSharper disable once InconsistentNaming
        var shareBP = HasBPNum switch
        {
            true when entry.HasBPNum => BPNumber == entry.BPNumber,
            false when !entry.HasBPNum => true,
            _ => false
        };

        //Same for internet
        // ReSharper disable once InconsistentNaming
        var shareSBSEg = HasSBandSEG switch
        {
            true when entry.HasSBandSEG => SBandSEG == entry.SBandSEG,
            false when !entry.HasSBandSEG => true,
            _ => false
        };

        var result = shareName && shareNet && sharePub && shareRes && shareTitle
                     && shareIndex && shareIndexBis && shareNo && shareCR && shareBP && shareSBSEg;
        Logger?.LogProcessingInfo($"Comparison done, result is: {result}");
        return result;
    }

    private bool CheckEquals(string a, string b)
    {
        int index = 0;
        if (a.Length != b.Length) return false;

        for (index = 0; index < a.Length; index++)
        {
            if (a[index] != b[index])
                return false;
        }

        return true;
    }

    private bool[] GetComparisonsOfEntriesByLine(BPDataEntry entry, bool shouldCompareNames)
    {
        var matches = new bool[12];
        matches[((int) Comparisons.bpNumMatch)] =
            (entry.HasBPNum && HasBPNum) && (entry.BPNumber == BPNumber);
        matches[((int) Comparisons.crMatch)] = (entry.HasCR && HasCR) && (entry.CR == CR);
        matches[((int) Comparisons.indexMatch)] = (entry.HasBPNum && HasIndex) && (entry.Index == Index);
        matches[((int) Comparisons.indexBisMatch)] = (entry.HasBPNum && HasIndexBis) && (entry.IndexBis == IndexBis);
        matches[((int) Comparisons.internetMatch)] = (entry.HasBPNum && HasInternet) && (entry.Internet == Internet);

        if (shouldCompareNames)
            matches[((int) Comparisons.nameMatch)] = (entry.HasBPNum && HasName) && (entry.Name == Name);
        else matches[((int) Comparisons.nameMatch)] = true;

        matches[((int) Comparisons.publicationMatch)] =
            (entry.HasPublication && HasPublication) && (entry.Publication == Publication);
        matches[((int) Comparisons.resumeMatch)] = (entry.HasResume && HasResume) && (entry.Resume == Resume);
        matches[((int) Comparisons.sbandsegMatch)] = (entry.HasSBandSEG && HasSBandSEG) && (entry.SBandSEG == SBandSEG);
        matches[((int) Comparisons.titleMatch)] = (entry.HasTitle && HasTitle) && (entry.Title == Title);
        matches[((int) Comparisons.anneeMatch)] = (entry.HasAnnee && HasAnnee) && (entry.Annee == Annee);
        matches[((int) Comparisons.noMatch)] = (entry.HasNo && HasNo) && (entry.No == No);

        return matches;
    }

    public int GetMatchStrength(BPDataEntry entry, bool shouldCompareNames = false)
    {
        var match = GetComparisonsOfEntriesByLine(entry, shouldCompareNames);
        var str = match.Aggregate(0, (h, t) => t ? h + 1 : h);
        return str;
    }

    public bool StrongMatch(BPDataEntry entry, bool shouldCompareNames = false)
    {
        var matchStrength = GetComparisonsOfEntriesByLine(entry, shouldCompareNames);
        var truthCount = matchStrength.Aggregate(0, (total, x) => x ? total = total + 1 : total);
        var strong = truthCount >= 9;
        return strong;
    }

    public bool MediumMatch(BPDataEntry entry, bool shouldCompareNames = false)
    {
        var matchStrength = GetComparisonsOfEntriesByLine(entry, shouldCompareNames);
        var truthCount = matchStrength.Aggregate(0, (total, x) => x ? total = total + 1 : total);
        var medium = truthCount >= 6;
        return medium;
    }

    public bool WeakMatch(BPDataEntry entry, bool shouldCompareNames = false)
    {
        var matchStrength = GetComparisonsOfEntriesByLine(entry, shouldCompareNames);
        var truthCount = matchStrength.Aggregate(0, (total, x) => x ? total = total + 1 : total);
        //If they match on more than one thing, find it and mention it.
        var weak = truthCount > 3;
        return weak;
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

        // ReSharper disable once IdentifierTypo
        sbandsegMatch = 9,
        titleMatch = 10,

        // ReSharper disable once IdentifierTypo
        anneeMatch = 11
    }
}