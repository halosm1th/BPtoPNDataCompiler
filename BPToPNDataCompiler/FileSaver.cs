using System.Text;
using System.Xml;
using DefaultNamespace;

namespace BPtoPNDataCompiler;

public class FileSaver
{
    public FileSaver(string basePath, Logger? _logger)
    {
        logger = _logger;

        logger?.LogProcessingInfo("Creating paths for saving lists.");
        EndDataFolder = Path.Combine(basePath, "BpToPnOutput");
        BpEntryPath = Path.Combine(EndDataFolder, "BPEntriesToUpdate");
        NewXmlEntryPath = Path.Combine(EndDataFolder, "NewXmlEntries");
        SharedEntriesPath = Path.Combine(EndDataFolder, "MinorDeviations");
        CREntriesPath = Path.Combine(EndDataFolder, "CREntries");

        logger?.Log("Setting up directories for saving.");
        SetupDirectoriesForSaving(EndDataFolder, BpEntryPath, NewXmlEntryPath,
            CREntriesPath, SharedEntriesPath);
    }

    private Logger logger { get; set; }
    public string EndDataFolder { get; set; }
    private string BpEntryPath { get; set; }
    private string NewXmlEntryPath { get; set; }
    private string SharedEntriesPath { get; set; }
    private string CREntriesPath { get; set; }

    public void SaveLists(List<UpdateDetail<BPDataEntry>> BpEntriesToUpdate,
        Dictionary<string, XmlDocument> updatedPNEntries, List<BPDataEntry> NewXmlEntriesToAdd,
        List<UpdateDetail<BPDataEntry>> SharedEntriesToLog, List<CRReviewData> CREntries, CRReviewParser parser)
    {
        logger.LogProcessingInfo("Saving Lists");
        // Sanitize and build end folder path using Path.Combine

        logger?.LogProcessingInfo("Saving Bp Entries.");
        SaveBPEntries(BpEntriesToUpdate, BpEntryPath);

        logger?.LogProcessingInfo("Saving Pn Entries.");
        SavePNEntries(updatedPNEntries);

        logger?.LogProcessingInfo("Saving XML of new entries.");
        SaveNewXMlFromBPEntries(NewXmlEntriesToAdd, NewXmlEntryPath);

        logger?.LogProcessingInfo("Saving Minor Deviations");
        SaveMinorDeviations(SharedEntriesToLog, SharedEntriesPath);

        logger?.LogProcessingInfo("Saving CR Entries");
        SaveCREntries(CREntries, parser, CREntriesPath);

        logger?.LogProcessingInfo("Finished saving lists. ");
    }

    public List<CRReviewData> ParseCRReviews(CRReviewParser parser, int lastPN,
        List<(BPDataEntry, XMLDataEntry)> dmCrEntriesToUpdate)
    {
        logger?.Log("Starting CR Review parser");
        return parser.ParseReviews(dmCrEntriesToUpdate, lastPN);
    }

    private void SaveCREntries(List<CRReviewData> crEntries, CRReviewParser parser, string crEntriesPath)
    {
        parser.SaveCRReviews(crEntries, crEntriesPath);
    }

    private void SaveMinorDeviations(List<UpdateDetail<BPDataEntry>> sharedEntriesToLog,
        string sharedEntriesPath)
    {
        var sb = new StringBuilder();
        foreach (var entry in sharedEntriesToLog)
        {
            sb.Append(
                $"BP# {entry.Entry.BPNumber}. For [{entry.FieldName}] _BP has_ [{entry.OldValue}] :: _PN has_ [{entry.NewValue}].\n");
        }

        var minorDeviationPath = Path.Combine(sharedEntriesPath, "MinorDeviations.txt");
        Console.WriteLine($"Saving minorDeviations to {minorDeviationPath}");
        logger?.LogProcessingInfo($"Saving minorDeviations to {minorDeviationPath}");

        if (File.Exists(minorDeviationPath)) minorDeviationPath = minorDeviationPath.Replace(".xml", " (2).xml");
        File.WriteAllText(minorDeviationPath, sb.ToString());
    }


    /// <summary>
    /// Sets up the necessary directories for saving the processed data.
    /// </summary>
    /// <param name="EndDataFolder">The root folder for all saved data.</param>
    /// <param name="BPEntryPath">Path for BP entries to update.</param>
    /// <param name="PnEntryPath">Path for PN entries to update.</param>
    /// <param name="NewXmlEntryPath">Path for new XML entries.</param>
    private void SetupDirectoriesForSaving(string EndDataFolder, string BPEntryPath,
        string NewXmlEntryPath, string CRPath, string SharedListPath)
    {
        logger?.LogProcessingInfo(
            $"Setting up directories for saving. Paths are: {EndDataFolder} [{BPEntryPath}, {NewXmlEntryPath}]");
        Console.WriteLine($"Setting up saving directories [{BPEntryPath},  {NewXmlEntryPath}]");

        logger?.LogProcessingInfo("Creating directory for saving.");
        Directory.CreateDirectory(EndDataFolder);
        logger?.LogProcessingInfo("Creating BPEntry To Update Folder.");
        Directory.CreateDirectory(BPEntryPath);
        logger?.LogProcessingInfo("Creating New Xml Entries for PN Folder.");
        Directory.CreateDirectory(NewXmlEntryPath);
        logger?.LogProcessingInfo("Creating New Shared Entries Folder.");
        Directory.CreateDirectory(SharedListPath);
        logger?.LogProcessingInfo("Creating New CR Entries Folder.");
        Directory.CreateDirectory(CRPath);
    }

    /// <summary>
    /// Saves new XML entries to the specified path.
    /// </summary>
    /// <param name="NewXmlEntriesToAdd">List of new BPDataEntry objects to be saved as XML.</param>
    /// <param name="path">The directory path where the XML files will be saved.</param>
    private void SaveNewXMlFromBPEntries(List<BPDataEntry> NewXmlEntriesToAdd, string path)
    {
        logger?.LogProcessingInfo("Saving XMl Entries..");
        Console.WriteLine("Saving Xml Entries");
        foreach (var newXml in NewXmlEntriesToAdd)
        {
            // Sanitize file path by replacing invalid characters
            if (newXml.Title != null)
            {
                var title = newXml.Title.Replace("\"", "")
                    .Replace(":", "_")
                    .Replace("\\", "")
                    .Replace("/", "")
                    .Replace(".", "_")
                    .Replace(" ", "_")
                    .Replace("&", "")
                    .Replace(";", "")
                    .Replace("?", "")
                    .Replace(",", "")
                    .Replace("(", "")
                    .Replace(")", "")
                    .Replace("=", "")
                    .Replace("'", "");

                title = title.Substring(0, Math.Min(title.Length, 80));

                var filePath = Path.Combine(path, title);
                filePath = filePath + ".xml";
                Console.WriteLine($"Saving {newXml.Title} to {filePath}");
                logger?.LogProcessingInfo($"Saving  {newXml.Title} to {filePath}");
                WriteBPXmlEntry(newXml, filePath);
            }
        }
    }

    private void WriteBPXmlEntry(BPDataEntry entry, string path)
    {
        var xml = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                  $"<bibl xmlns=\"http://www.tei-c.org/ns/1.0\" xml:id=\"b{entry.BPNumber}\" type=\"\">\n" +
                  $"{entry.ToXML()}" +
                  $"\n</bibl>";

        try
        {
            if (File.Exists(path)) path = path.Replace(".xml", " (2).xml");
            File.WriteAllText(path, xml);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    /// <summary>
    /// Saves updated PN entries to the specified path.
    /// </summary>
    /// <param name="xmlDocuments">List of XMLDataEntry objects to be saved as XML.</param>
    /// <param name="path">The directory path where the XML files will be saved.</param>
    private void SavePNEntries(Dictionary<string, XmlDocument> xmlDocuments)
    {
        logger?.LogProcessingInfo("Saving PN Entries");
        Console.WriteLine("Saving Pn Entries");
        foreach (var xmlDocument in xmlDocuments)
        {
            WritePNEntry(xmlDocument.Value, xmlDocument.Key);
        }
    }

    /// <summary>
    /// Saves updated BP entries to the specified path.
    /// </summary>
    /// <param name="bpEntriesToUpdate">List of BPDataEntry objects to be saved as XML.</param>
    /// <param name="path">The directory path where the XML files will be saved.</param>
    private void SaveBPEntries(List<UpdateDetail<BPDataEntry>> bpEntriesToUpdate, string path)
    {
        //Whedn saving save as: BP NUmber + field name + what the change will be

        logger?.Log("Saving BP Entries");
        Console.WriteLine("Saving Bp Entries");
        var filePath = Path.Combine(path, "BPEntriesToUpdate.txt");
        var sb = new StringBuilder();

        if (File.Exists(filePath))
        {
            var text = File.ReadAllLines(filePath);
            foreach (var line in text)
            {
                sb.Append(line);
                sb.Append("\n");
            }
        }

        foreach (var bpEntries in bpEntriesToUpdate)
        {
            var bpText =
                $"BP #: {bpEntries.Entry.BPNumber}. Changed {bpEntries.FieldName} _from_ [{bpEntries.OldValue ?? "[BLANK]"}] _to_ [{bpEntries.NewValue}]. ";
            sb.Append(bpText + "\n");

            logger?.LogProcessingInfo($"Updated {bpText}");

            // Sanitize file path by replacing invalid characters
        }

        WriteBPEntry(sb, filePath);
    }

    private void WriteBPEntry(StringBuilder finalTexts, string path)
    {
        try
        {
            /**** if (File.Exists(path))
             {
                 var pth = path.Replace("BPEntriesToUpdate.txt", "");
                 int count = Directory.GetFiles(pth).Count(x => x.Contains(path));
                 count++;
                 path = path.Replace(".txt", $" ({count}).txt");
             }*/
            File.WriteAllText(path, finalTexts.ToString());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private void WritePNEntry(XmlDocument xmlDocument, string path)
    {
        try
        {
            // Ensure parent directory exists before writing
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory) && directory != null)
            {
                Directory.CreateDirectory(directory);
            }


            using var writer = new XmlTextWriter(path, Encoding.UTF8)
            {
                Formatting = Formatting.Indented,
                Indentation = 4
            };

            var numb = Path.GetFileNameWithoutExtension(path);
            Console.WriteLine($"Wrote {numb} to {path}");
            logger?.LogProcessingInfo($"Wrote {numb} to {path}");
            xmlDocument.WriteTo(writer);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error writing PN entry: {e.Message}\n{e.StackTrace}");
        }
    }

    public Dictionary<string, XmlDocument> UpdatePnEntries(
        List<UpdateDetail<XMLDataEntry>> pnEntriesNeedingUpdates)
    {
        logger?.Log($"Fixing {pnEntriesNeedingUpdates.Count} PN Entries.");
        var fixedEntries = new List<XMLDataEntry>();
        foreach (var entry in pnEntriesNeedingUpdates)
        {
            logger?.LogProcessingInfo(
                $"Fixed {entry.FieldName} on {entry.Entry.Title} from {entry.OldValue} to {entry.NewValue}");

            var fixedEntry = entry.Entry;
            // Using a switch statement for better readability and maintainability
            switch (entry.FieldName.ToLower())
            {
                case "bpnumber":
                    fixedEntry.BPNumber = entry.NewValue;
                    break;
                case "cr":
                    fixedEntry.CR = entry.NewValue;
                    break;
                case "index":
                    fixedEntry.Index = entry.NewValue;
                    break;
                case "indexbis":
                    fixedEntry.IndexBis = entry.NewValue;
                    break;
                case "internet":
                    fixedEntry.Internet = entry.NewValue;
                    break;
                case "name":
                    fixedEntry.Name = entry.NewValue;
                    break;
                case "publication":
                    fixedEntry.Publication = entry.NewValue;
                    break;
                case "resume":
                    fixedEntry.Resume = entry.NewValue;
                    fixedEntry.Note = entry.NewValue;
                    break;
                case "sbandseg":
                    fixedEntry.SBandSEG = entry.NewValue;
                    break;
                case "title":
                    fixedEntry.Title = entry.NewValue;
                    break;
            }

            fixedEntries.Add(fixedEntry);
        }

        var xmlDocments = new Dictionary<string, XmlDocument>();

        foreach (var entry in fixedEntries)
        {
            if (xmlDocments.Any(document =>
                {
                    // Create namespace manager
                    var nsManager = new XmlNamespaceManager(document.Value.NameTable);
                    nsManager.AddNamespace("tei", "http://www.tei-c.org/ns/1.0");

                    var bpElement = document.Value.SelectSingleNode("//tei:idno[@type='pi']", nsManager);
                    if (bpElement != null && bpElement.InnerText == entry.PNNumber)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }))
            {
                var bpElement = xmlDocments.First(document =>
                {
                    // Create namespace manager
                    var nsManager = new XmlNamespaceManager(document.Value.NameTable);
                    nsManager.AddNamespace("tei", "http://www.tei-c.org/ns/1.0");

                    var bpElement = document.Value.SelectSingleNode("//tei:idno[@type='bp']", nsManager);
                    if (bpElement != null && bpElement.InnerText == entry.BPNumber)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                });

                ChangeXmlDocument(entry, bpElement.Value);
            }
            else
            {
                var xmlDocument = LoadAndChangeXmlDocument(entry);
                xmlDocments.Add(entry.PNFileName, xmlDocument);
            }
        }

        return xmlDocments;
    }


    /// <summary>
    /// Saves the compiled lists of BP, PN, and new XML entries to disk.
    /// </summary>
    /// <param name="BpEntriesToUpdate">List of BP entries to update.</param>
    /// <param name="updatedPNEntries">List of PN entries to update.</param>
    /// <param name="NewXmlEntriesToAdd">List of new XML entries to add.</param>
    /// <returns>The name of the folder where the data was saved.</returns>
    private XmlDocument LoadAndChangeXmlDocument(XMLDataEntry entry)
    {
        var xmlDocument = new XmlDocument();
        xmlDocument.Load(entry.PNFileName);
        return ChangeXmlDocument(entry, xmlDocument);
    }

    private XmlDocument ChangeXmlDocument(XMLDataEntry entry, XmlDocument xmlDocument)
    {
        var root = xmlDocument.DocumentElement;

        // Create namespace manager
        var nsManager = new XmlNamespaceManager(xmlDocument.NameTable);
        nsManager.AddNamespace("tei", "http://www.tei-c.org/ns/1.0");

        // Get BP number using XPath
        var bpElement = root?.SelectSingleNode("//tei:idno[@type='bp']", nsManager);
        var name = root?.SelectSingleNode("//tei:seg[@subtype='nom']", nsManager);
        var index = root?.SelectSingleNode("//tei:seg[@subtype='index']", nsManager);
        var indexBis = root?.SelectSingleNode("//tei:seg[@subtype='indexBis']", nsManager);
        var title = root?.SelectSingleNode("//tei:seg[@subtype='titre']", nsManager);
        var publisher = root?.SelectSingleNode("//tei:seg[@subtype='publication']", nsManager);
        var resume = root?.SelectSingleNode("//tei:seg[@subtype='resume']", nsManager);
        var sbandSeg = root?.SelectSingleNode("//tei:seg[@subtype='sbSeg']", nsManager);
        var cr = root?.SelectSingleNode("//tei:seg[@subtype='cr']", nsManager);
        var internet = root?.SelectSingleNode("//tei:seg[@subtype='internet']", nsManager);
        var note = root?.SelectSingleNode("//tei:note[@resp='#BP']", nsManager);

        if (bpElement != null && entry.HasBPNum)
        {
            bpElement.InnerText = entry.BPNumber ?? "[none]";
        }
        else if (bpElement == null && entry.HasBPNum)
        {
            // Create new idno element with TEI namespace
            var newBpElement = xmlDocument.CreateElement("idno", "http://www.tei-c.org/ns/1.0");

            // Set type attribute
            var typeAttr = xmlDocument.CreateAttribute("type");
            typeAttr.Value = "bp";
            newBpElement.Attributes.Append(typeAttr);

            // Set BP number text
            newBpElement.InnerText = entry.BPNumber ?? "[none]";

            // Insert as first child of root element
            root?.AppendChild(newBpElement);
        }

        logger?.LogProcessingInfo("Checking if note exists");
        if (note != null && entry.HasResume)
        {
            logger?.LogProcessingInfo($"There is a note, updating it to contain: {entry.Resume}");
            note.InnerText = entry.Resume ?? "[none]";
        }
        else if (note == null & entry.HasResume)
        {
            logger.LogProcessingInfo($"There was no note. Creating a new note and inserting {entry.Resume}");
            // Create new seg element with TEI namespace 
            var newNameElement = xmlDocument.CreateElement("note", "http://www.tei-c.org/ns/1.0");

            // Set resp attribute
            var respAttr = xmlDocument.CreateAttribute("resp");
            respAttr.Value = "#BP";
            newNameElement.Attributes.Append(respAttr);

            // Set name text
            newNameElement.InnerText = entry.Resume ?? "[none]";

            // Insert as first child of root element
            root?.AppendChild(newNameElement);
        }


        if (name != null && entry.HasName)
        {
            name.InnerText = entry.Name ?? "[none]";
        }
        else if (name != null && !entry.HasName)
        {
            name.InnerText = entry.Name ?? "[none]";
        }
        else if (name == null && entry.HasName)
        {
            // Create new seg element with TEI namespace 
            var newNameElement = xmlDocument.CreateElement("seg", "http://www.tei-c.org/ns/1.0");

            // Set subtype attribute
            var subtypeAttr = xmlDocument.CreateAttribute("subtype");
            subtypeAttr.Value = "nom";
            newNameElement.Attributes.Append(subtypeAttr);

            // Set resp attribute
            var respAttr = xmlDocument.CreateAttribute("resp");
            respAttr.Value = "#BP";
            newNameElement.Attributes.Append(respAttr);

            // Set type attribute
            var type = xmlDocument.CreateAttribute("type");
            type.Value = "original";
            newNameElement.Attributes.Append(type);

            // Set name text
            newNameElement.InnerText = entry.Name ?? "[none]";

            // Insert as first child of root element
            root?.AppendChild(newNameElement);
        }

        if (index != null && entry.HasIndex)
        {
            index.InnerText = entry.Index ?? "[none]";
        }
        else if (index != null && !entry.HasIndex)
        {
            index.InnerText = entry.Index ?? "[none]";
        }
        else if (index == null && entry.HasIndex)
        {
            // Create new seg element with TEI namespace 
            var newNameElement = xmlDocument.CreateElement("seg", "http://www.tei-c.org/ns/1.0");

            // Set subtype attribute
            var subtypeAttr = xmlDocument.CreateAttribute("subtype");
            subtypeAttr.Value = "index";
            newNameElement.Attributes.Append(subtypeAttr);

            // Set resp attribute
            var respAttr = xmlDocument.CreateAttribute("resp");
            respAttr.Value = "#BP";
            newNameElement.Attributes.Append(respAttr);

            // Set type attribute
            var type = xmlDocument.CreateAttribute("type");
            type.Value = "original";
            newNameElement.Attributes.Append(type);

            // Set name text
            newNameElement.InnerText = entry.Index ?? "[none]";

            // Insert as first child of root element
            root?.AppendChild(newNameElement);
        }

        if (indexBis != null && entry.HasIndexBis)
        {
            indexBis.InnerText = entry.IndexBis ?? "[none]";
        }
        else if (indexBis != null && !entry.HasIndexBis)
        {
            indexBis.InnerText = entry.IndexBis ?? "[none]";
        }
        else if (indexBis == null && entry.HasIndexBis)
        {
            // Create new seg element with TEI namespace 
            var newNameElement = xmlDocument.CreateElement("seg", "http://www.tei-c.org/ns/1.0");

            // Set subtype attribute
            var subtypeAttr = xmlDocument.CreateAttribute("subtype");
            subtypeAttr.Value = "indexBis";
            newNameElement.Attributes.Append(subtypeAttr);

            // Set resp attribute
            var respAttr = xmlDocument.CreateAttribute("resp");
            respAttr.Value = "#BP";
            newNameElement.Attributes.Append(respAttr);

            // Set type attribute
            var type = xmlDocument.CreateAttribute("type");
            type.Value = "original";
            newNameElement.Attributes.Append(type);

            // Set name text
            newNameElement.InnerText = entry.IndexBis ?? "[none]";

            // Insert as first child of root element
            root?.AppendChild(newNameElement);
        }

        if (title != null && entry.HasTitle)
        {
            title.InnerText = entry.Title ?? "[none]";
        }
        else if (title != null && !entry.HasTitle)
        {
            title.InnerText = entry.Title ?? "[none]";
        }
        else if (title == null && entry.HasTitle)
        {
            // Create new seg element with TEI namespace 
            var newNameElement = xmlDocument.CreateElement("seg", "http://www.tei-c.org/ns/1.0");

            // Set subtype attribute
            var subtypeAttr = xmlDocument.CreateAttribute("subtype");
            subtypeAttr.Value = "titre";
            newNameElement.Attributes.Append(subtypeAttr);

            // Set resp attribute
            var respAttr = xmlDocument.CreateAttribute("resp");
            respAttr.Value = "#BP";
            newNameElement.Attributes.Append(respAttr);

            // Set type attribute
            var type = xmlDocument.CreateAttribute("type");
            type.Value = "original";
            newNameElement.Attributes.Append(type);

            // Set name text
            newNameElement.InnerText = entry.Title ?? "[none]";

            // Insert as first child of root element
            root?.AppendChild(newNameElement);
        }

        if (publisher != null && entry.HasPublication)
        {
            publisher.InnerText = entry.Publication ?? "[none]";
        }
        else if (publisher != null && !entry.HasPublication)
        {
            publisher.InnerText = entry.Publication ?? "[none]";
        }
        else if (publisher == null && entry.HasPublication)
        {
            // Create new seg element with TEI namespace 
            var newNameElement = xmlDocument.CreateElement("seg", "http://www.tei-c.org/ns/1.0");

            // Set subtype attribute
            var subtypeAttr = xmlDocument.CreateAttribute("subtype");
            subtypeAttr.Value = "publication";
            newNameElement.Attributes.Append(subtypeAttr);

            // Set subtype attribute
            var type = xmlDocument.CreateAttribute("type");
            type.Value = "original";
            newNameElement.Attributes.Append(type);

            // Set resp attribute
            var respAttr = xmlDocument.CreateAttribute("resp");
            respAttr.Value = "#BP";
            newNameElement.Attributes.Append(respAttr);

            // Set name text
            newNameElement.InnerText = entry.Publication ?? "[none]";

            // Insert as first child of root element
            root?.AppendChild(newNameElement);

            logger?.Log($"adding note entry to xml for {entry.BPNumber}");
            var noteNode = root?.SelectSingleNode("//note[@resp=\"#bp\"]");
            if (noteNode != null)
            {
                noteNode.InnerText = entry.Publication ?? "[none]";
            }
            else
            {
                var newNoteElement = xmlDocument.CreateElement("note", "http://www.tei-c.org/ns/1.0");
                newNoteElement.Attributes.Append(respAttr);
                newNoteElement.InnerText = entry.Publication ?? "NULL ERROR ON PUBLICATION ENTRY";
                root?.AppendChild(newNoteElement);
            }
        }

        if (resume != null && entry.HasResume)
        {
            resume.InnerText = entry.Resume ?? "[none]";
        }
        else if (resume != null && !entry.HasResume)
        {
            resume.InnerText = entry.Resume ?? "[none]";
        }
        else if (resume == null && entry.HasResume)
        {
            // Create new seg element with TEI namespace 
            var newNameElement = xmlDocument.CreateElement("seg", "http://www.tei-c.org/ns/1.0");

            // Set subtype attribute
            var subtypeAttr = xmlDocument.CreateAttribute("subtype");
            subtypeAttr.Value = "resume";
            newNameElement.Attributes.Append(subtypeAttr);

            // Set type attribute
            var type = xmlDocument.CreateAttribute("type");
            type.Value = "original";
            newNameElement.Attributes.Append(type);

            // Set resp attribute
            var respAttr = xmlDocument.CreateAttribute("resp");
            respAttr.Value = "#BP";
            newNameElement.Attributes.Append(respAttr);

            // Set name text
            newNameElement.InnerText = entry.Resume ?? "[none]";

            // Insert as first child of root element
            root?.AppendChild(newNameElement);
        }

        if (sbandSeg != null && entry.HasSBandSEG)
        {
            sbandSeg.InnerText = entry.SBandSEG ?? "[none]";
        }
        else if (sbandSeg != null && !entry.HasSBandSEG)
        {
            sbandSeg.InnerText = entry.SBandSEG ?? "[none]";
        }
        else if (sbandSeg == null && entry.HasSBandSEG)
        {
            // Create new seg element with TEI namespace 
            var newNameElement = xmlDocument.CreateElement("seg", "http://www.tei-c.org/ns/1.0");

            // Set subtype attribute
            var subtypeAttr = xmlDocument.CreateAttribute("subtype");
            subtypeAttr.Value = "resume";
            newNameElement.Attributes.Append(subtypeAttr);

            // Set resp attribute
            var respAttr = xmlDocument.CreateAttribute("resp");
            respAttr.Value = "#BP";
            newNameElement.Attributes.Append(respAttr);

            // Set type attribute
            var type = xmlDocument.CreateAttribute("type");
            type.Value = "original";
            newNameElement.Attributes.Append(type);

            // Set name text
            newNameElement.InnerText = entry.SBandSEG ?? "[none]";

            // Insert as first child of root element
            root?.AppendChild(newNameElement);
        }

        if (cr != null && entry.HasCR)
        {
            cr.InnerText = entry.CR ?? "[none]";
        }
        else if (cr != null && !entry.HasCR)
        {
            cr.InnerText = entry.CR ?? "[none]";
        }
        else if (cr == null && entry.HasCR)
        {
            // Create new seg element with TEI namespace 
            var newNameElement = xmlDocument.CreateElement("seg", "http://www.tei-c.org/ns/1.0");

            // Set subtype attribute
            var subtypeAttr = xmlDocument.CreateAttribute("subtype");
            subtypeAttr.Value = "cr";
            newNameElement.Attributes.Append(subtypeAttr);

            // Set resp attribute
            var respAttr = xmlDocument.CreateAttribute("resp");
            respAttr.Value = "#BP";
            newNameElement.Attributes.Append(respAttr);

            // Set type attribute
            var type = xmlDocument.CreateAttribute("type");
            type.Value = "original";
            newNameElement.Attributes.Append(type);

            // Set name text
            newNameElement.InnerText = entry.CR ?? "[none]";

            // Insert as first child of root element
            root?.AppendChild(newNameElement);
        }

        if (internet != null && entry.HasInternet)
        {
            internet.InnerText = entry.Internet ?? "[none]";
        }
        else if (internet != null && !entry.HasInternet)
        {
            internet.InnerText = entry.Internet ?? "[none]";
        }
        else if (internet == null && entry.HasInternet)
        {
            // Create new seg element with TEI namespace 
            var newNameElement = xmlDocument.CreateElement("seg", "http://www.tei-c.org/ns/1.0");

            // Set subtype attribute
            var subtypeAttr = xmlDocument.CreateAttribute("subtype");
            subtypeAttr.Value = "internet";
            newNameElement.Attributes.Append(subtypeAttr);

            // Set resp attribute
            var respAttr = xmlDocument.CreateAttribute("resp");
            respAttr.Value = "#BP";
            newNameElement.Attributes.Append(respAttr);

            // Set type attribute
            var type = xmlDocument.CreateAttribute("type");
            type.Value = "original";
            newNameElement.Attributes.Append(type);

            // Set name text
            newNameElement.InnerText = entry.Internet ?? "Error with setting internet value?";

            // Insert as first child of root element
            root?.AppendChild(newNameElement);
        }

        return xmlDocument;
    }
}