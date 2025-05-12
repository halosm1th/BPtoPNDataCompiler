using System.Xml;

namespace DefaultNamespace;

public class XMLEntryGatherer
{
    public XMLEntryGatherer(string path)
    {
        BiblioPath = path;
    }

    public string BiblioPath { get; set; }

    private async Task<XMLDataEntry> GetEntry(string filePath)
    {
        var entry = new XMLDataEntry(filePath);

        Console.WriteLine(filePath);
        var doc = new XmlDocument();
        doc.Load(filePath);

        var name = doc.Name;

        foreach (XmlElement node in doc?.DocumentElement?.ChildNodes)
        {
            //var name = node.ChildNodes;
            if (node.LocalName == "idno" && node.OuterXml.Contains("type=\"pi\""))
            {
                entry.PNNumber = node.InnerText;
            }
            else if (node.LocalName == "idno" && node.OuterXml.Contains("type=\"bp\""))
            {
                entry.BPNumber = node.InnerText;
            }
            else if (node.LocalName == "seg" && node.OuterXml.Contains("subtype=\"indexBis\""))
            {
                entry.IndexBis = node.InnerText;
            }
            else if (node.LocalName == "seg" && node.OuterXml.Contains("subtype=\"index\""))
            {
                entry.Index = node.InnerText;
            }
            else if (node.LocalName == "seg" && node.OuterXml.Contains("subtype=\"titre\""))
            {
                entry.Title = node.InnerText;
            }
            else if (node.LocalName == "seg" && node.OuterXml.Contains("subtype=\"publication\""))
            {
                entry.Publication = node.InnerText;
            }
            else if (node.LocalName == "seg" && node.OuterXml.Contains("subtype=\"cr\""))
            {
                entry.CR = node.InnerText;
            }
            else if (node.LocalName == "seg" && node.OuterXml.Contains("subtype=\"nom\""))
            {
                entry.Name = node.InnerText;
            }
            else if (node.LocalName == "seg" && node.OuterXml.Contains("subtype=\"resume\""))
            {
                entry.Resume = node.InnerText;
            }
            else if (node.LocalName == "seg" && node.OuterXml.Contains("subtype=\"internet\""))
            {
                Console.WriteLine("Internet hit");
                entry.Internet = node.InnerText;
            }
            else if (node.LocalName == "seg" && node.OuterXml.Contains("subtype=\"SBandSEG\""))
            {
                Console.WriteLine("SBandSEG hit");
                entry.SBandSEG = node.InnerText;
            }
            else if (node.LocalName == "seg" && node.OuterXml.Contains("subtype=\"no\""))
            {
                Console.WriteLine("No. hit");
                entry.No = node.InnerText;
            }
            else if (node.LocalName == "seg" && node.OuterXml.Contains("subtype=\"annee\""))
            {
                Console.WriteLine("Annee hit");
                entry.Annee = node.InnerText;
            }
        }

        return entry;
    }

    private async Task<List<XMLDataEntry>> GetEntriesFromFolder(string folder)
    {
        var entries = new List<XMLDataEntry>();

        var files = Directory.GetFiles(folder);
        var entryTasks = new List<Task<XMLDataEntry>>();

        foreach (var file in files)
        {
            var entryTask = GetEntry(file);
            entryTasks.Add(entryTask);
        }

        foreach (var t in entryTasks)
        {
            entries.Add(await t);
        }


        return entries;
    }

    public async Task<List<XMLDataEntry>> GatherEntries()
    {
        var entries = new List<XMLDataEntry>();

        var folders = Directory.GetDirectories(BiblioPath);
        var ranges = new List<Task<List<XMLDataEntry>>>();

        foreach (var folder in folders)
        {
            var getEntries = GetEntriesFromFolder(folder);
            ranges.Add(getEntries);
        }

        foreach (var range in ranges)
        {
            entries.AddRange(await range);
        }


        return entries;
    }
}