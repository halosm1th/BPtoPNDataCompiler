using System.Text;
using DefaultNamespace;

// ReSharper disable InconsistentNaming

namespace BPtoPNDataCompiler;

public class BPDataEntry
{
    private string? _annee;
    private string? _bpNum;
    private string? _cr;
    private string? _index;
    private string? _indexBis;
    private string? _internet;
    private string? _name;
    private string? _no;
    private string? _publication;
    private string? _resume;
    private string? _sbandseg;
    private string? _title;

    //At a minimum all entries must have one number
    public BPDataEntry(string? number, Logger logger)
    {
        BPNumber = number;
        this.Logger = logger;
    }

    protected Logger Logger { get; }

    public string? Index
    {
        get => _index;
        set => _index = ReplaceInvalidText(value);
    }

    public bool HasIndex => _index != null;

    public string? IndexBis
    {
        get => _indexBis;
        set => _indexBis = ReplaceInvalidText(value);
    }

    public bool HasIndexBis => _indexBis != null;

    public string? Title
    {
        get => _title;
        set => _title = ReplaceInvalidText(value);
    }

    public bool HasTitle => _title != null;

    public string? Publication
    {
        get => _publication;
        set => _publication = ReplaceInvalidText(value);
    }

    public bool HasPublication => _publication != null;

    public string? Internet
    {
        get => _internet;
        set => _internet = ReplaceInvalidText(value);
    }

    public bool HasInternet => _internet != null;

    public string? SBandSEG
    {
        get => _sbandseg;
        set => _sbandseg = ReplaceInvalidText(value);
    }

    public bool HasSBandSEG => _sbandseg != null;

    public string? No
    {
        get => _no;
        set => _no = ReplaceInvalidText(value);
    }

    public bool HasNo => _no != null;

    public string? Annee
    {
        get => _annee;
        set => _annee = ReplaceInvalidText(value);
    }

    public bool HasAnnee => _annee != null;

    public string? Resume
    {
        get => _resume;
        set => _resume = ReplaceInvalidText(value);
    }

    public bool HasResume => _resume != null;

    public string? CR
    {
        get => _cr;
        set => _cr = ReplaceInvalidText(value);
    }

    public bool HasCR => _cr != null;

    public string? Name
    {
        get => _name;
        set => _name = ReplaceInvalidText(value);
    }

    public bool HasName => _name != null;

    public string? BPNumber
    {
        get => _bpNum;
        set => _bpNum = ReplaceInvalidText(value);
    }

    public bool HasBPNum => _bpNum != null;

    private string? ReplaceInvalidText(string? value)
    {
        if (Logger != null)
        {
            // logger.LogProcessingInfo($"Replacing invalid text in {value ?? ""}");
        }

        value = value?.Replace("&quot;", "\"");
        value = value?.Replace("&#34;", "\"");
        value = value?.Replace("&#039;", "'");
        value = value?.Replace("&", "&amp;");
        value = value?.Replace("<", "&lt;");
        value = value?.Replace(">", "&gt;");
        if (Logger != null)
        {
            //logger.LogProcessingInfo($"Replaced text resulted in: {value}");
        }

        return value;
    }

    public override string ToString()
    {
        return $"{Name ?? ""} {Internet ?? ""} {Publication ?? ""} " +
               $"{Resume ?? ""} {Title ?? ""} {Index ?? ""} {IndexBis ?? ""} " +
               $"{No ?? ""} {CR ?? ""} {BPNumber ?? ""} {SBandSEG ?? ""}";
    }

    public string ToXML()
    {
        var sb = new StringBuilder();
        sb.Append($"<idno type=\"bp\">{BPNumber}</idno>\n");
        if (_index != null) sb.Append($"<seg type=\"original\" subtype=\"index\" resp=\"#BP\">{_index}</seg>\n");
        if (IndexBis != null) sb.Append($"<seg type=\"original\" subtype=\"indexBis\" resp=\"#BP\">{IndexBis}</seg>\n");
        if (_title != null) sb.Append($"<seg type=\"original\" subtype=\"titre\" resp=\"#BP\">{_title}</seg>\n");
        if (SBandSEG != null) sb.Append($"<seg type=\"original\" subtype=\"SBandSeg\" resp=\"#BP\">{SBandSEG}</seg>\n");
        if (No != null) sb.Append($"<seg type=\"original\" subtype=\"No\" resp=\"#BP\">{No}</seg>\n");
        if (_annee != null) sb.Append($"<seg type=\"original\" subtype=\"annee\" resp=\"#BP\">{_annee}</seg>\n");
        if (Publication != null)
            sb.Append($"<seg type=\"original\" subtype=\"publication\" resp=\"#BP\">{Publication}</seg>\n");
        if (_resume != null) sb.Append($"<seg type=\"original\" subtype=\"resume\" resp=\"#BP\">{_resume}</seg>\n");
        if (CR != null) sb.Append($"<seg type=\"original\" subtype=\"cr\" resp=\"#BP\">{CR}</seg>\n");
        if (Name != null) sb.Append($"<seg type=\"original\" subtype=\"nom\" resp=\"#BP\">{Name}</seg>\n");
        if (_internet != null) sb.Append($"<ptr target=\"{_internet}\"\n");

        return sb.ToString();
    }
}