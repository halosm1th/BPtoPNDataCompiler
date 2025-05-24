namespace DefaultNamespace;

public class XMLDataEntry : BPDataEntry
{
    public XMLDataEntry(string fileName) : base(null)
    {
        PNFileName = fileName;
    }

    public bool HasNo => HasBPNum;
    public string No => BPNumber ?? "0000-0000";


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
        //If htey both have share name, then shareName is equal if the names are equal.
        //if they both don't have share name, then they share in not having a name
        var shareName = false;
        if (HasName && entry.HasName) shareName = Name == entry.Name;
        else if (!HasName && !entry.HasName) shareName = true;
        else shareName = false;

        //Same for internet
        var shareNet = false;
        if (HasInternet && entry.HasInternet) shareNet = Internet == entry.Internet;
        else if (!HasInternet && !entry.HasInternet) shareNet = true;
        else shareNet = false;

        //Same for internet
        var sharePub = false;
        if (HasPublication && entry.HasPublication) sharePub = Publication == entry.Publication;
        else if (!HasPublication && !entry.HasPublication) sharePub = true;
        else sharePub = false;

        //Same for internet
        var shareRes = false;
        if (HasResume && entry.HasResume) shareRes = Resume == entry.Resume;
        else if (!HasResume && !entry.HasResume) shareRes = true;
        else shareRes = false;

        //Same for internet
        var shareTitle = false;
        if (HasTitle && entry.HasTitle) shareTitle = Title == entry.Title;
        else if (!HasTitle && !entry.HasTitle) shareTitle = true;
        else shareTitle = false;

        //Same for internet
        var shareIndex = false;
        if (HasIndex && entry.HasIndex) shareIndex = Index == entry.Index;
        else if (!HasIndex && !entry.HasIndex) shareIndex = true;
        else shareIndex = false;

        //Same for internet
        var shareIndexBis = false;
        if (HasIndexBis && entry.HasIndexBis) shareIndexBis = IndexBis == entry.IndexBis;
        else if (!HasIndexBis && !entry.HasIndexBis) shareIndexBis = true;
        else shareIndexBis = false;

        //Same for internet
        var shareNo = false;
        if (HasNo && entry.HasNo) shareNo = No == entry.No;
        else if (!HasNo && !entry.HasNo) shareNo = true;
        else shareNo = false;

        //Same for internet
        var shareCR = false;
        if (HasCR && entry.HasCR) shareCR = CR == entry.CR;
        else if (!HasCR && !entry.HasCR) shareCR = true;
        else shareCR = false;

        //Same for internet
        var shareBP = false;
        if (HasBPNum && entry.HasBPNum) shareBP = BPNumber == entry.BPNumber;
        else if (!HasBPNum && !entry.HasBPNum) shareBP = true;
        else shareBP = false;

        //Same for internet
        var shareSBSEg = false;
        if (HasSBandSEG && entry.HasSBandSEG) shareSBSEg = SBandSEG == entry.SBandSEG;
        else if (!HasSBandSEG && !entry.HasSBandSEG) shareSBSEg = true;
        else shareSBSEg = false;

        /*
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
                         && ((entry.HasSBandSEG && HasSBandSEG) && (entry.SBandSEG == SBandSEG));*/

        return shareName && shareNet && sharePub && shareRes && shareTitle
               && shareIndex && shareIndexBis && shareNo && shareCR && shareBP && shareSBSEg;
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

    private bool[] GetComparisonsOfEntriesByLine(BPDataEntry entry)
    {
        var matches = new bool[12];
        matches[((int) Comparisons.bpNumMatch)] =
            (entry.HasBPNum && HasBPNum) && (entry.BPNumber == BPNumber);
        matches[((int) Comparisons.crMatch)] = (entry.HasCR && HasCR) && (entry.CR == CR);
        matches[((int) Comparisons.indexMatch)] = (entry.HasBPNum && HasIndex) && (entry.Index == Index);
        matches[((int) Comparisons.indexBisMatch)] = (entry.HasBPNum && HasIndexBis) && (entry.IndexBis == IndexBis);
        matches[((int) Comparisons.internetMatch)] = (entry.HasBPNum && HasInternet) && (entry.Internet == Internet);
        matches[((int) Comparisons.nameMatch)] = (entry.HasBPNum && HasName) && (entry.Name == Name);
        matches[((int) Comparisons.publicationMatch)] =
            (entry.HasPublication && HasPublication) && (entry.Publication == Publication);
        matches[((int) Comparisons.resumeMatch)] = (entry.HasResume && HasResume) && (entry.Resume == Resume);
        matches[((int) Comparisons.sbandsegMatch)] = (entry.HasSBandSEG && HasSBandSEG) && (entry.SBandSEG == SBandSEG);
        matches[((int) Comparisons.titleMatch)] = (entry.HasTitle && HasTitle) && (entry.Title == Title);
        matches[((int) Comparisons.anneeMatch)] = (entry.HasAnnee && HasAnnee) && (entry.Annee == Annee);
        matches[((int) Comparisons.noMatch)] = (entry.HasNo && HasNo) && (entry.No == No);

        return matches;
    }

    public bool StrongMatch(BPDataEntry entry)
    {
        var matchStrength = GetComparisonsOfEntriesByLine(entry);
        var truthCount = matchStrength.Aggregate(0, (total, x) => x ? total = total + 1 : total);
        return truthCount > 6;
    }

    public bool WeakMatch(BPDataEntry entry)
    {
        var matchStrength = GetComparisonsOfEntriesByLine(entry);
        var truthCount = matchStrength.Aggregate(0, (total, x) => x ? total = total + 1 : total);
        //If they match on more than one thing, find it and mention it.
        return truthCount > 1;
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