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
        bool anyMatch = (entry.Name == Name) || (entry.Internet == Internet) || (entry.Publication == Publication)
                        || (entry.Resume == Resume) || (entry.Title == Title) || (entry.Index == Index) ||
                        (entry.IndexBis == IndexBis)
                        || (entry.No == No) || (entry.CR == CR) || (entry.BPNumber == BPNumber) ||
                        (entry.SBandSEG == SBandSEG);

        return anyMatch;
    }

    public bool FullMatch(BPDataEntry entry)
    {
        bool fullMatch = (entry.Name == Name) && (entry.Internet == Internet)
                                              && (entry.Publication == Publication)
                                              && (entry.Resume == Resume) && (entry.Title == Title)
                                              && (entry.Index == Index) && (entry.IndexBis == IndexBis)
                                              && (entry.No == No) && (entry.CR == CR) && (entry.BPNumber == BPNumber)
                                              && (entry.SBandSEG == SBandSEG);

        return fullMatch;
    }
}