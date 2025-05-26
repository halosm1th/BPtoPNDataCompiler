using System.Xml;

namespace DefaultNamespace;

public class XMLEntryGatherer
{
    private static readonly Dictionary<string, Action<XmlElement, XMLDataEntry>> AttributeSetters = new()
    {
        {"idno:pi", (node, entry) => entry.PNNumber = node.InnerText},
        {"idno:bp", (node, entry) => entry.BPNumber = node.InnerText},
        {"seg:indexBis", (node, entry) => entry.IndexBis = node.InnerText},
        {"seg:index", (node, entry) => entry.Index = node.InnerText},
        {"seg:titre", (node, entry) => entry.Title = node.InnerText},
        {"seg:publication", (node, entry) => entry.Publication = node.InnerText},
        {"seg:cr", (node, entry) => entry.CR = node.InnerText},
        {"seg:nom", (node, entry) => entry.Name = node.InnerText},
        {"seg:resume", (node, entry) => entry.Resume = node.InnerText},
        {
            "seg:internet", (node, entry) =>
            {
                Console.WriteLine("Internet hit");
                entry.Internet = node.InnerText;
            }
        },
        {"seg:sbSeg", (node, entry) => entry.SBandSEG = node.InnerText}
    };

    public XMLEntryGatherer(string path, Logger logger)
    {
        logger.LogProcessingInfo($"Created new XMLEntryGatherer with path: {path}");
        BiblioPath = path;
        this.logger = logger;
    }

    public string BiblioPath { get; set; }
    private Logger logger { get; }

    private async Task<XMLDataEntry> GetEntry(string filePath)
    {
        logger.LogProcessingInfo($"Getting entry at {filePath}");
        var entry = new XMLDataEntry(filePath);
        var doc = new XmlDocument();
        doc.Load(filePath);
        logger.LogProcessingInfo("Entry loaded.");

        //Console.WriteLine($"getting: {filePath}");

        foreach (var rawNode in doc?.DocumentElement?.ChildNodes)
        {
            if (rawNode.GetType() == typeof(XmlElement))
            {
                logger.LogProcessingInfo("Found an element in the xmlfile, processing node.");
                var node = ((XmlElement) rawNode);
                await SetEntryAttributes(node, entry);
            }
            else
            {
                logger.LogProcessingInfo($"Found a node that is not an element, moving onto {filePath}");
                Console.WriteLine($"getting: {filePath}");
            }
        }

        logger.LogProcessingInfo($"Done processing, returning: {entry}");
        return entry;
    }

    private Task SetEntryAttributes(XmlElement node, XMLDataEntry entry)
    {
        foreach (var key in AttributeSetters.Keys)
        {
            var parts = key.Split(':');
            if (node.LocalName == parts[0] && node.OuterXml.Contains($"subtype=\"{parts[1]}\""))
            {
                AttributeSetters[key](node, entry);
                break;
            }
            else if (parts[0] == "idno" && node.LocalName == "idno" && node.OuterXml.Contains($"type=\"{parts[1]}\""))
            {
                AttributeSetters[key](node, entry);
                break;
            }
        }

        return Task.CompletedTask;
    }

    private async IAsyncEnumerable<XMLDataEntry> GetEntriesFromFolder(string folder)
    {
        foreach (var file in Directory.GetFiles(folder))
        {
            var entry = await GetEntry(file);
            if (entry != null)
                yield return entry;
        }
    }

    public async Task<List<XMLDataEntry>> GatherEntries()
    {
        var entries = new List<XMLDataEntry>();
        try
        {
            foreach (var folder in Directory.GetDirectories(BiblioPath))
            {
                Console.WriteLine($"adding files in : {folder}");
                await foreach (var entry in GetEntriesFromFolder(folder))
                {
                    entries.Add(entry);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return entries;
    }
}