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
}