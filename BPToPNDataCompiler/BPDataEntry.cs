using System.Text;

namespace DefaultNamespace;

public class BPDataEntry
{
    private string? index = null;

    //At a minimum all entries must have one number
    public BPDataEntry(string number)
    {
        BPNumber = number;
    }

    public string? Index
    {
        get { return index; }
        set { index = ReplaceInvalidText(value); }
    }

    public bool HasIndex => index != null;

    public string? indexBis { get; set; }
    public string? title { get; set; }
    public string? publication { get; set; }
    public string? internet { get; set; }
    public string? SBandSEG { get; set; }
    public string? No { get; set; }
    public string? annee { get; set; }
    public string? resume { get; set; }
    public string? CR { get; set; }
    public string? Name { get; set; }
    public string BPNumber { get; set; }

    private string ReplaceInvalidText(string value)
    {
        value = value.Replace("&", "&amp;");
        value = value.Replace("<", "&lt;");
        value = value.Replace(">", "&gt;");
        return value;
    }

    public string ToXML()
    {
        var sb = new StringBuilder();
        sb.Append($"<idno type=\"bp\">{BPNumber}</idno>");
        if (index != null) sb.Append($"<seg type=\"original\" subtype=\"index\" resp=\"#BP\">{index}</seg>");
        if (indexBis != null) sb.Append($"<seg type=\"original\" subtype=\"indexBis\" resp=\"#BP\">{indexBis}</seg>");
        if (title != null) sb.Append($"<seg type=\"original\" subtype=\"titre\" resp=\"#BP\">{title}</seg>");
        if (SBandSEG != null) sb.Append($"<seg type=\"original\" subtype=\"SBandSeg\" resp=\"#BP\">{SBandSEG}</seg>");
        if (No != null) sb.Append($"<seg type=\"original\" subtype=\"No\" resp=\"#BP\">{No}</seg>");
        if (annee != null) sb.Append($"<seg type=\"original\" subtype=\"annee\" resp=\"#BP\">{annee}</seg>");
        if (publication != null)
            sb.Append($"<seg type=\"original\" subtype=\"publication\" resp=\"#BP\">{publication}</seg>");
        if (resume != null) sb.Append($"<seg type=\"original\" subtype=\"resume\" resp=\"#BP\">{resume}</seg>");
        if (CR != null) sb.Append($"<seg type=\"original\" subtype=\"cr\" resp=\"#BP\">{CR}</seg>");
        if (Name != null) sb.Append($"<seg type=\"original\" subtype=\"nom\" resp=\"#BP\">{Name}</seg>");
        if (internet != null) sb.Append($"<ptr target=\"{internet}\"");

        return sb.ToString();
    }
}