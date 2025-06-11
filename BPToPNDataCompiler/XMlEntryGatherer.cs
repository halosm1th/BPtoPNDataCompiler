using System.Text.RegularExpressions;
using System.Xml;
using BPtoPNDataCompiler;

namespace DefaultNamespace;

public class XMLEntryGatherer
{
    private static readonly Dictionary<string, Action<XmlElement, XMLDataEntry>> AttributeSetters = new()
    {
        {"idno:pi", (node, entry) => entry.PNNumber = Regex.Replace(node.InnerText.Trim(), " {2, }", " ")},
        {"idno:bp", (node, entry) => entry.BPNumber = Regex.Replace(node.InnerText.Trim(), " {2, }", " ")},
        {"seg:indexBis", (node, entry) => entry.IndexBis = Regex.Replace(node.InnerText.Trim(), " {2, }", " ")},
        {"seg:index", (node, entry) => entry.Index = Regex.Replace(node.InnerText.Trim(), " {2, }", " ")},
        {"seg:titre", (node, entry) => entry.Title = Regex.Replace(node.InnerText.Trim(), " {2, }", " ")},
        {"seg:publication", (node, entry) => entry.Publication = Regex.Replace(node.InnerText.Trim(), " {2, }", " ")},
        {"seg:cr", (node, entry) => entry.CR = Regex.Replace(node.InnerText.Trim(), " {2, }", " ")},
        {"seg:nom", (node, entry) => entry.Name = Regex.Replace(node.InnerText.Trim(), " {2, }", " ")},
        {"seg:resume", (node, entry) => entry.Resume = Regex.Replace(node.InnerText.Trim(), " {2, }", " ")},
        {"seg:internet", (node, entry) => entry.Internet = Regex.Replace(node.InnerText.Trim(), " {2, }", " ")},
        {"seg:sbSeg", (node, entry) => entry.SBandSEG = Regex.Replace(node.InnerText.Trim(), " {2, }", " ")}
    };

    public XMLEntryGatherer(string path, Logger? logger)
    {
        logger?.LogProcessingInfo($"Created new XMLEntryGatherer with path: {path}");
        BiblioPath = path;
        this.logger = logger;
    }

    public string BiblioPath { get; set; }
    private Logger? logger { get; }

    private XMLDataEntry GetEntry(string filePath)
    {
        //logger.LogProcessingInfo($"Getting entry at {filePath}");
        var entry = new XMLDataEntry(filePath, logger);
        var doc = new XmlDocument();
        doc.Load(filePath);
        //logger.LogProcessingInfo("Entry loaded.");

        //Console.WriteLine($"getting: {filePath}");

        if (doc?.DocumentElement?.ChildNodes != null)
            foreach (var rawNode in doc.DocumentElement.ChildNodes)
            {
                if (rawNode.GetType() == typeof(XmlElement))
                {
                    var node = ((XmlElement) rawNode);
                    SetEntryAttributes(node, entry);
                }
                else
                {
                    //logger.LogProcessingInfo($"Found a node that is not an element, moving onto {filePath}");
                    Console.WriteLine($"getting: {filePath}");
                }
            }

        //logger.LogProcessingInfo($"Finished processing entry {entry}");
        return entry;
    }

    private void SetEntryAttributes(XmlElement node, XMLDataEntry entry)
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
    }

    private List<XMLDataEntry> GetEntriesFromFolder(string folder)
    {
        logger?.Log($"Getting entries from folder {folder}");
        logger?.LogProcessingInfo($"Getting entries from folder {folder}");
        var dataEntries = new List<XMLDataEntry>();
        foreach (var file in Directory.GetFiles(folder))
        {
            var entry = GetEntry(file);
            //logger.LogProcessingInfo($"Gathered {entry.Title} from file {file}");
            if (entry != null)
                dataEntries.Add(entry);
        }

        return dataEntries;
    }

    public List<XMLDataEntry> GatherEntries()
    {
        //logger.LogProcessingInfo("Gathering XMl Entries");
        var entries = new List<XMLDataEntry>();
        try
        {
            foreach (var folder in Directory.GetDirectories(BiblioPath))
            {
                logger?.LogProcessingInfo($"Adding XML files in {folder}");
                logger?.Log("Adding XMl Files in {folder}");
                Console.WriteLine($"adding files in : {folder}");
                foreach (var entry in GetEntriesFromFolder(folder))
                {
                    //logger.Log($"Adding {entry.Title} from {folder} to entries");
                    //logger.LogProcessingInfo($"Adding {entry.Title} from {folder} to entries");
                    entries.Add(entry);
                }
            }
        }
        catch (Exception e)
        {
            logger?.LogError("Error in gather xml entries: ", e);
            Console.WriteLine(e);
        }

        logger?.LogProcessingInfo($"Gathered {entries.Count} XML entries for processing.");
        return entries;
    }
}