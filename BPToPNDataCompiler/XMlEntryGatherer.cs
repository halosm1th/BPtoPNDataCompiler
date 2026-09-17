using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Xml;
using BPtoPNDataCompiler;

namespace DefaultNamespace;

public class XMLEntryGatherer
{
    private static readonly Dictionary<string, Action<XmlElement, XMLDataEntry>> AttributeSetters = new()
    {
        {"idno:pi", (node, entry) => entry.PNNumber = Regex.Replace(node.InnerText.Trim(), @"\s{2,}", " ")},
        {"idno:bp", (node, entry) => entry.BPNumber = Regex.Replace(node.InnerText.Trim(), @"\s{2,}", " ")},
        {"seg:indexBis", (node, entry) => entry.IndexBis = Regex.Replace(node.InnerText.Trim(), @"\s{2,}", " ")},
        {"seg:index", (node, entry) => entry.Index = Regex.Replace(node.InnerText.Trim(), @"\s{2,}", " ")},
        {"seg:titre", (node, entry) => entry.Title = Regex.Replace(node.InnerText.Trim(), @"\s{2,}", " ")},
        {"seg:publication", (node, entry) => entry.Publication = Regex.Replace(node.InnerText.Trim(), @"\s{2,}", " ")},
        {"seg:cr", (node, entry) => entry.CR = Regex.Replace(node.InnerText.Trim(), @"\s{2,}", " ")},
        {"seg:nom", (node, entry) => entry.Name = Regex.Replace(node.InnerText.Trim(), @"\s{2,}", " ")},
        {"seg:resume", (node, entry) => entry.Resume = Regex.Replace(node.InnerText.Trim(), @"\s{2,}", " ")},
        {"seg:internet", (node, entry) => entry.Internet = Regex.Replace(node.InnerText.Trim(), @"\s{2,}", " ")},
        {"seg:sbSeg", (node, entry) => entry.sbSeg = Regex.Replace(node.InnerText.Trim(), @"\s{2,}", " ")}
    };

    public XMLEntryGatherer(string path, Logger? logger)
    {
        logger?.LogProcessingInfo($"Created new XMLEntryGatherer with path: {path}");
        BiblioPath = path;
        this.logger = logger;
    }

    public string BiblioPath { get; set; }
    private Logger? logger { get; }

    public XMLDataEntry GetEntry(string filePath)
    {
        try
        {
            //logger.LogProcessingInfo($"Getting entry at {filePath}");
            var entry = new XMLDataEntry(filePath, logger);
            var doc = new XmlDocument();

            //Console.WriteLine($"getting: {filePath}");
            doc.Load(filePath);
            //logger.LogProcessingInfo("Entry loaded.");


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
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Ran inot an error processing file: {filePath}: {e}");
            Console.ResetColor();
            logger?.LogError($"Ran inot an error processing file: {filePath}.", e);

            return null;
        }
    }

    private void SetEntryAttributes(XmlElement node, XMLDataEntry entry)
    {
        // idno elements key on "type", seg elements key on "subtype"; reading the attribute directly
        // and doing a single dictionary lookup avoids re-serializing the whole node (OuterXml) and
        // scanning all 11 setters for every single element, which dominated load time on large Biblio sets.
        var attributeValue = node.GetAttribute(node.LocalName == "idno" ? "type" : "subtype");
        if (string.IsNullOrEmpty(attributeValue)) return;

        if (AttributeSetters.TryGetValue($"{node.LocalName}:{attributeValue}", out var setter))
        {
            setter(node, entry);
        }
    }

    public List<XMLDataEntry> GatherEntries()
    {
        var entries = new ConcurrentBag<XMLDataEntry>();
        try
        {
            var allFiles = Directory.GetDirectories(BiblioPath)
                .SelectMany(folder => Directory.GetFiles(folder).Where(x => Path.GetExtension(x) == ".xml"))
                .ToList();

            logger?.LogProcessingInfo($"Found {allFiles.Count} XML files to load, reading them in parallel.");
            Console.WriteLine($"Found {allFiles.Count} XML files, loading in parallel...");

            // Reading and parsing each PN biblio file is independent of every other one, so loading them
            // in parallel lets this scale with available CPU cores/disk throughput instead of one-at-a-time.
            Parallel.ForEach(allFiles, file =>
            {
                var entry = GetEntry(file);
                if (entry != null) entries.Add(entry);
            });
        }
        catch (Exception e)
        {
            logger?.LogError("Error in gather xml entries: ", e);
            Console.WriteLine(e);
        }

        logger?.LogProcessingInfo($"Gathered {entries.Count} XML entries for processing.");
        return entries.ToList();
    }
}