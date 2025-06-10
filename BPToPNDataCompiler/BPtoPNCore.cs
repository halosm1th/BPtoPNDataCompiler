using System.CommandLine;
using System.CommandLine.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using DefaultNamespace;

// ReSharper disable InconsistentNaming

namespace BPtoPNDataCompiler;

public class BPtoPNCore
{
    // Static fields to hold the parsed argument values
    private static int startYear = 1932;
#if DEBUG
    private static int endYear = DateTime.Now.Year - 1; //1932;
    private static int bpStartNumber = 1; // New: Default beginning number for BP data
    private static int bpEndNumber = 9999; // New: Default finishing number for BP data

#else
    private static int endYear = DateTime.Now.Year - 1;
    private static int bpStartNumber = 1; // New: Default beginning number for BP data
    private static int bpEndNumber = 9999; // New: Default finishing number for BP data
#endif

    #region main and arg parsing

    public static Logger logger { get; private set; }
    private static bool ShouldCompareName { get; set; } = false;
    public static string DepthLevel = "/..";
    private static bool NoDelete = false;

    public static async Task Main(string[] args)
    {
        logger = new Logger(DepthLevel);
        try
        {
            // Define command-line options for the application
            var startYearOption = new Option<int>(
                name: "--start-year",
                getDefaultValue: () => startYear,
                description:
                "Sets the start year for data compilation. Use -s or --start-year. Default is 1932. Cannot be less than 1932."
            );
            startYearOption.AddAlias("-s"); // Add alias using AddAlias method

            var shouldCompareAuthorNames = new Option<bool>(
                name: "--compare-author-names",
                getDefaultValue: () => ShouldCompareName,
                description:
                "Sets if the author names should be a field to be compared. Defaults to false."
            );
            startYearOption.AddAlias("-c"); // Add alias using AddAlias method

            var endYearOption = new Option<int>(
                name: "--end-year",
                getDefaultValue: () => endYear,
                description:
                $"Sets the end year for data compilation. Use -e or --end-year. Default is the current system year -1 (Currently: {DateTime.Now.Year - 1}). Cannot be lower than the start year."
            );
            endYearOption.AddAlias("-e"); // Add alias using AddAlias method


            var noDelete = new Option<bool>(
                name: "--no-delete",
                getDefaultValue: () => NoDelete,
                description:
                $"If used, will **not** delete the resulting folder that is zipped. By default this folder is zipped and deleted after the program is run"
            );
            endYearOption.AddAlias("-d"); // Add alias using AddAlias method

            var bpStartNumberOption = new Option<int>(
                name: "--bp-start-number",
                getDefaultValue: () => bpStartNumber,
                description:
                "Sets the beginning number for BP data processing. Use -bps or --bp-start-number. Default is 0. Cannot be negative."
            );
            bpStartNumberOption.AddAlias("-bps"); // Add alias using AddAlias method
            bpStartNumberOption.AddAlias("-b"); // Add alias using AddAlias method

            var bpEndNumberOption = new Option<int>(
                name: "--bp-end-number",
                getDefaultValue: () => bpEndNumber,
                description:
                "Sets the finishing number for BP data processing. Use -bpe or --bp-end-number. Default is maximum integer value. Cannot be less than the BP start number."
            );
            bpEndNumberOption.AddAlias("-bpe"); // Add alias using AddAlias method
            bpEndNumberOption.AddAlias("-f"); // Add alias using AddAlias method


            var helpOption = new Option<bool>(
                name: "--menu",
                description: "Show help menu.",
                getDefaultValue: () => false
            );
            helpOption.AddAlias("-m");

            // Create the root command for the application
            var rootCommand =
                new RootCommand(
                    "BP to PN Data Compiler: Compiles and updates bibliographic data from BP and PN sources.")
                {
                    startYearOption,
                    endYearOption,
                    bpStartNumberOption,
                    bpEndNumberOption,
                    shouldCompareAuthorNames,
                    noDelete,
                    helpOption,
                };


            // Set the handler for the root command. This action will be executed when the command is invoked.
            rootCommand.SetHandler(async (context) =>
            {
                var showHelp = context.ParseResult.GetValueForOption(helpOption);
                if (showHelp)
                {
                    context.Console.Out.WriteLine(rootCommand.Description);
                    context.ExitCode = 0;
                    rootCommand.Invoke("-h"); // force internal help logic
                    Environment.Exit(context.ExitCode);
                }

                // Retrieve the parsed values for each option
                startYear = context.ParseResult.GetValueForOption(startYearOption);
                endYear = context.ParseResult.GetValueForOption(endYearOption);
                bpStartNumber = context.ParseResult.GetValueForOption(bpStartNumberOption);
                bpEndNumber = context.ParseResult.GetValueForOption(bpEndNumberOption);
                ShouldCompareName = context.ParseResult.GetValueForOption(shouldCompareAuthorNames);

                // Perform custom validation after parsing
                ValidateYears();
                ValidateBpNumbers();

                logger.Log("Parsing args completed.");
                Console.WriteLine($"Args parsed. Start Year: {startYear}, End Year: {endYear}.");
                Console.WriteLine($"BP Start Number: {bpStartNumber}, BP End Number: {bpEndNumber}.");
                logger.Log($"Start Year: {startYear}, End Year: {endYear}");
                logger.Log($"BP Start Number: {bpStartNumber}, BP End Number: {bpEndNumber}");

                // If all validations pass, proceed with the core application logic
                await Core();
            });


            // Invoke the command line parser with the provided arguments
            // System.CommandLine will automatically handle help (-h or --help) and validation errors.
            await rootCommand.InvokeAsync(args);
        }
        catch (ArgumentException e)
        {
            // Catch specific argument validation errors
            ExceptionInfo(e);
        }
        catch (DirectoryNotFoundException e)
        {
            // Catch directory related errors
            ExceptionInfo(e);
        }
        catch (Exception e)
        {
            // Catch any other unexpected exceptions
            ExceptionInfo(e);
        }
    }

    /// <summary>
    /// Validates the start and end years based on application rules.
    /// Throws ArgumentException if validation fails.
    /// </summary>
    private static void ValidateYears()
    {
        if (startYear > endYear)
        {
            logger.LogError("Error: End year cannot be greater than start year.", new ArgumentException());
            throw new ArgumentException("Error: End year cannot be greater than start year.");
        }

        if (startYear < 1932)
        {
            logger.LogError("Error: Start year cannot be less than 1932.", new ArgumentException());
            throw new ArgumentException("Error: Start year cannot be less than 1932.");
        }

        if (endYear > DateTime.Now.Year - 1)
        {
            logger.LogError(
                $"Error: The end year cannot be greater than the current system year -1 (Currently: {DateTime.Now.Year - 1})",
                new ArgumentException());
            throw new ArgumentException(
                $"Error: The end year cannot be greater than the current system year -1 (Currently: {DateTime.Now.Year - 1})");
        }
    }

    /// <summary>
    /// Validates the BP start and end numbers.
    /// Throws ArgumentException if validation fails.
    /// </summary>
    private static void ValidateBpNumbers()
    {
        if (bpStartNumber < 0) // Assuming BP numbers are non-negative
        {
            logger.LogError("Error: BP start number cannot be negative.", new ArgumentException());
            throw new ArgumentException("Error: BP start number cannot be negative.");
        }

        if (bpEndNumber < bpStartNumber)
        {
            logger.LogError("Error: BP end number cannot be less than BP start number.", new ArgumentException());
            throw new ArgumentException("Error: BP end number cannot be less than BP start number.");
        }
    }

    /// <summary>
    /// Displays exception information to the console and logs it.
    /// </summary>
    /// <param name="e">The exception to display.</param>
    private static void ExceptionInfo(Exception e)
    {
        //Just a nice little way for us to bubble any errors we run into up and to the user, to be handled with ease.
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(e.Message);
        logger.LogError("Error: ", e);
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    // The old ParseArgs and ShowHelp methods are removed as System.CommandLine handles this.

    #endregion

    /// <summary>
    /// The core logic of the application, executed after successful argument parsing.
    /// </summary>
    private static async Task Core()
    {
        logger.Log("Started core");
        try
        {
            // This will check
            var startingPath = Directory.GetCurrentDirectory();
            var gitHandler = new GitFolderHandler(logger);
            //If we have the git folder. Normally will error out before this if it cannot be found.
            //AS such we'll just let hte exceptions bubble up.
            var biblioPath = gitHandler.GitBiblioDirectoryCheck(DepthLevel);
            var currentPath = Directory.GetCurrentDirectory();

            logger.Log("Creating BpEntry Gatherer");
            Console.WriteLine("Creating BPEntry Gatherer");
            // Pass bpStartNumber and bpEndNumber to BPEntryGatherer if it needs them
            // Assuming BPEntryGatherer can be updated to accept these new parameters
            var BPEntryGatherer =
                new BPEntryGatherer(startYear, endYear, logger, bpStartNumber,
                    bpEndNumber); // You might need to add bpStartNumber, bpEndNumber here

            logger.Log("BPEntryGather Created.\nCreating XMLEntryGatherer.");
            Console.WriteLine("BPEntryGather created. Creating XMLEntry gatherer");
            var XMLEntryGatherer = new XMLEntryGatherer(biblioPath, logger);
            logger.Log("XML Entry Gatherer created.");

            logger.Log("Gathering XML entries");
            Console.WriteLine("XmlEntry Gatherer created, gathering XML entries.");
            var xmlEntries = XMLEntryGatherer.GatherEntries();

            currentPath = Directory.GetCurrentDirectory();

            logger.Log("Gathering BP entries.");
            Console.WriteLine("Gathered XMl Entries, gathering BP entries.");
            var bpEntries = BPEntryGatherer.GatherEntries(DepthLevel);

            logger.Log("Entries are gathered");
            Console.WriteLine("Entries have been gathered.");

            Console.Write("Preparing to start data matcher. ");
            logger.Log("Creating Datamatcher");
            var dm = new DataMatcher(xmlEntries, bpEntries, logger, ShouldCompareName);

            Console.WriteLine("Starting to match entries?");
            logger.Log("Starting to match entries");
            dm.MatchEntries();

            Console.WriteLine("Done matching entries. Now saving lists.");
            logger.Log("Finished matching entries.");
            logger.Log("Updating BPEntries before saving.");
            logger.Log("Updating PnEntries before saving.");
            var PnEntries = UpdatePnEntries(dm.PnEntriesToUpdate).ToList();

            logger.Log("Saving lists.");
            //This sets us to one level up from where the code is 
            currentPath = Directory.GetCurrentDirectory();
            var saveLocation = SaveLists(dm.BpEntriesToUpdate, PnEntries, dm.NewXmlEntriesToAdd, currentPath);
            logger.Log("Finished saving lists. ");

            logger.Log(
                "Finshied saving lists. Logger will dispose of self to allow the moving of BPXMLData to folder and BPtoPNLogs folder to final folder, then Will zip files, and then delete working areas.");
            Console.WriteLine(
                "Finshied saving lists. Logger will dispose of self to allow the moving of BPXMLData to folder and BPtoPNLogs folder to final folder, then Will zip files, and then delete working areas.");
            logger.Dispose();
            MoveBPXMLAndLogs(saveLocation);
            ZipDataDeleteWorkingDirs(saveLocation);
            Console.WriteLine("Finished saving lists.\nPress enter to exit...");
            Console.ReadLine();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    /// <summary>
    /// Zips the generated data and deletes working directories.
    /// </summary>
    /// <param name="saveLocation">The path to the directory containing data to be zipped.</param>
    private static void ZipDataDeleteWorkingDirs(string saveLocation)
    {
        var directory = Directory.GetCurrentDirectory();
        var sourcePath = Path.Combine(directory, saveLocation);
        var zipPath = Path.Combine(directory, $"{saveLocation}.zip");

        Console.WriteLine($"Zipping files from {sourcePath} to {zipPath}");

        // Make sure the target zip file doesn't already exist
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        // Wait a moment to ensure all file operations are complete
        Thread.Sleep(1000);

        try
        {
            // Use proper path combination and ensure the source directory exists
            if (Directory.Exists(sourcePath))
            {
                // Ensure all file streams are closed by forcing a GC collect
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Create the zip file
                ZipFile.CreateFromDirectory(
                    sourcePath,
                    zipPath,
                    CompressionLevel.Optimal,
                    false); // Don't include the base directory in the archive

                Console.WriteLine($"Successfully created ZIP file at: {zipPath}");

                // Optionally delete the source directory after successful zip creation
                if (!NoDelete) Directory.Delete(sourcePath, true);
            }
            else
            {
                Console.WriteLine($"Error: Source directory {sourcePath} does not exist");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating ZIP file: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    /// <summary>
    /// Moves BP XML files and logs to the specified save location.
    /// </summary>
    /// <param name="saveLocation">The target directory for moving files.</param>
    private static void MoveBPXMLAndLogs(string saveLocation)
    {
        var directory = Directory.GetCurrentDirectory();
        var dirs = Directory.GetDirectories(directory);
        if (dirs.Contains(directory + "\\BPXMLFiles"))
        {
            Directory.Move(directory + "/BPXMLFiles/", $"{saveLocation}/BPXMLFiles");
        }

        var logDirs = Directory.GetDirectories(directory);
        if (!logDirs.Any(x => x.Contains("BPtoPNLogs")))
        {
            dirs = Directory.GetDirectories(directory + "/../");
            if (dirs.Contains(directory + "/../BPtoPNLogs"))
            {
                Directory.Move(directory + "/../BPtoPNLogs", $"{saveLocation}/BPtoPNLogs");
            }
        }
        else
        {
            Directory.Move(directory + "/BPtoPNLogs", $"{saveLocation}/BPtoPNLogs");
        }
    }

    /// <summary>
    /// Updates PN entries based on the provided update details.
    /// </summary>
    /// <param name="pnEntriesNeedingUpdates">List of update details for PN entries.</param>
    /// <returns>An enumerable of updated XMLDataEntry objects.</returns>
    private static IEnumerable<XmlDocument> UpdatePnEntries(List<UpdateDetail<XMLDataEntry>> pnEntriesNeedingUpdates)
    {
        logger.Log($"Fixing {pnEntriesNeedingUpdates.Count} PN Entries.");
        var fixedEntries = new List<XMLDataEntry>();
        foreach (var entry in pnEntriesNeedingUpdates)
        {
            logger.LogProcessingInfo(
                $"Fixed {entry.FieldName} on {entry.Entry.Title} from {entry.OldValue} to {entry.NewValue}");

            var fixedEntry = entry.Entry;
            // Using a switch statement for better readability and maintainability
            switch (entry.FieldName)
            {
                case "BPNumber":
                    fixedEntry.BPNumber = entry.NewValue;
                    break;
                case "CR":
                    fixedEntry.CR = entry.NewValue;
                    break;
                case "Index":
                    fixedEntry.Index = entry.NewValue;
                    break;
                case "IndexBis":
                    fixedEntry.IndexBis = entry.NewValue;
                    break;
                case "Internet":
                    fixedEntry.Internet = entry.NewValue;
                    break;
                case "Name":
                    fixedEntry.Name = entry.NewValue;
                    break;
                case "No":
                    // If you check the definition of XMlEntry, the number is always set to equal to the BPNumber
                    fixedEntry.BPNumber = entry.NewValue;
                    break;
                case "Publication":
                    fixedEntry.Publication = entry.NewValue;
                    break;
                case "Resume":
                    fixedEntry.Resume = entry.NewValue;
                    break;
                case "SBandSEG":
                    fixedEntry.SBandSEG = entry.NewValue;
                    break;
                case "Title":
                    fixedEntry.Title = entry.NewValue;
                    break;
                case "Annee":
                    fixedEntry.Annee = entry.NewValue;
                    break;
            }

            fixedEntries.Add(fixedEntry);
        }

        var xmlDocments = new List<XmlDocument>();

        foreach (var entry in fixedEntries)
        {
            if (xmlDocments.Any(document =>
                {
                    // Create namespace manager
                    var nsManager = new XmlNamespaceManager(document.NameTable);
                    nsManager.AddNamespace("tei", "http://www.tei-c.org/ns/1.0");

                    var bpElement = document.SelectSingleNode("//tei:idno[@type='bp']", nsManager);
                    if (bpElement != null && bpElement.InnerText == entry.BPNumber)
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
                    var nsManager = new XmlNamespaceManager(document.NameTable);
                    nsManager.AddNamespace("tei", "http://www.tei-c.org/ns/1.0");

                    var bpElement = document.SelectSingleNode("//tei:idno[@type='bp']", nsManager);
                    if (bpElement != null && bpElement.InnerText == entry.BPNumber)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                });

                ChangeXmlDocument(entry, bpElement);
            }
            else
            {
                var xmlDocument = LoadAndChangeXmlDocument(entry);
                xmlDocments.Add(xmlDocument);
            }
        }

        return xmlDocments;
    }

    /// <summary>
    /// Updates BP entries based on the provided update details.
    /// </summary>
    /// <param name="bpEntriesNeedingUpdates">List of update details for BP entries.</param>
    /// <returns>An enumerable of updated BPDataEntry objects.</returns>
    private static IEnumerable<BPDataEntry> UpdateBpEntries(List<UpdateDetail<BPDataEntry>> bpEntriesNeedingUpdates)
    {
        logger.LogProcessingInfo($"Correcting {bpEntriesNeedingUpdates.Count} Bp Entries.");
        var fixedEntries = new List<BPDataEntry>();
        foreach (var entry in bpEntriesNeedingUpdates)
        {
            logger.LogProcessingInfo(
                $"Fixed {entry.FieldName} on {entry.Entry.Title} from {entry.OldValue} to {entry.NewValue}");

            var fixedEntry = entry.Entry;
            // Using a switch statement for better readability and maintainability
            switch (entry.FieldName)
            {
                case "BPNumber":
                    fixedEntry.BPNumber = entry.NewValue;
                    break;
                case "CR":
                    fixedEntry.CR = entry.NewValue;
                    break;
                case "Index":
                    fixedEntry.Index = entry.NewValue;
                    break;
                case "IndexBis":
                    fixedEntry.IndexBis = entry.NewValue;
                    break;
                case "Internet":
                    fixedEntry.Internet = entry.NewValue;
                    break;
                case "Name":
                    fixedEntry.Name = entry.NewValue;
                    break;
                case "No":
                    fixedEntry.No = entry.NewValue;
                    break;
                case "Publication":
                    fixedEntry.Publication = entry.NewValue;
                    break;
                case "Resume":
                    fixedEntry.Resume = entry.NewValue;
                    break;
                case "SBandSEG":
                    fixedEntry.SBandSEG = entry.NewValue;
                    break;
                case "Title":
                    fixedEntry.Title = entry.NewValue;
                    break;
                case "Annee":
                    fixedEntry.Annee = entry.NewValue;
                    break;
            }

            fixedEntries.Add(fixedEntry);
        }

        return fixedEntries;
    }

    /// <summary>
    /// Saves the compiled lists of BP, PN, and new XML entries to disk.
    /// </summary>
    /// <param name="BpEntriesToUpdate">List of BP entries to update.</param>
    /// <param name="updatedPNEntries">List of PN entries to update.</param>
    /// <param name="NewXmlEntriesToAdd">List of new XML entries to add.</param>
    /// <returns>The name of the folder where the data was saved.</returns>
    private static string SaveLists(List<UpdateDetail<BPDataEntry>> BpEntriesToUpdate,
        List<XmlDocument> updatedPNEntries, List<BPDataEntry> NewXmlEntriesToAdd, string basePath)
    {
        logger.LogProcessingInfo("Creating paths for saving lists.");
        var EndDataFolder = $"/BpToPnChecker-{DateTime.Now}".Replace(":", ".").Replace("-", "_");
        EndDataFolder = basePath + EndDataFolder;
        var BPEntryPath = $"{EndDataFolder}/BPEntriesToUpdate";
        var PnEntryPath = $"{EndDataFolder}/PNEntriesToUpdate";
        var NewXmlEntryPath = $"{EndDataFolder}/NewXmlEntries";

        logger.Log("Setting up directories for saving.");
        SetupDirectoriesForSaving(EndDataFolder, BPEntryPath, PnEntryPath, NewXmlEntryPath);

        logger.Log("Saving Bp Entries.");
        SaveBPEntries(BpEntriesToUpdate, BPEntryPath);

        logger.Log("Saving Pn Entries.");
        SavePNEntries(updatedPNEntries, PnEntryPath);

        logger.Log("Saving XML of new entries.");
        SaveNewXMlFromBPEntries(NewXmlEntriesToAdd, NewXmlEntryPath);

        return EndDataFolder;
    }

    /// <summary>
    /// Sets up the necessary directories for saving the processed data.
    /// </summary>
    /// <param name="EndDataFolder">The root folder for all saved data.</param>
    /// <param name="BPEntryPath">Path for BP entries to update.</param>
    /// <param name="PnEntryPath">Path for PN entries to update.</param>
    /// <param name="NewXmlEntryPath">Path for new XML entries.</param>
    private static void SetupDirectoriesForSaving(string EndDataFolder, string BPEntryPath, string PnEntryPath,
        string NewXmlEntryPath)
    {
        logger.LogProcessingInfo(
            $"Setting up directories for saving. Paths are: {EndDataFolder} [{BPEntryPath}, {PnEntryPath}, {NewXmlEntryPath}]");
        Console.WriteLine($"Setting up saving directories [{BPEntryPath}, {PnEntryPath}, {NewXmlEntryPath}]");

        logger.LogProcessingInfo("Creating directory for saving.");
        Directory.CreateDirectory(EndDataFolder);
        logger.LogProcessingInfo("Creating BPEntry To Update Folder.");
        Directory.CreateDirectory(BPEntryPath);
        logger.LogProcessingInfo("Creating PnEnties To Update Folder.");
        Directory.CreateDirectory(PnEntryPath);
        logger.LogProcessingInfo("Creating New Xml Enties for PN Folder.");
        Directory.CreateDirectory(NewXmlEntryPath);
    }

    /// <summary>
    /// Saves new XML entries to the specified path.
    /// </summary>
    /// <param name="NewXmlEntriesToAdd">List of new BPDataEntry objects to be saved as XML.</param>
    /// <param name="path">The directory path where the XML files will be saved.</param>
    private static void SaveNewXMlFromBPEntries(List<BPDataEntry> NewXmlEntriesToAdd, string path)
    {
        logger.LogProcessingInfo("Saving XMl Entries..");
        Console.WriteLine("Saving Xml Entries");
        foreach (var newXml in NewXmlEntriesToAdd)
        {
            // Sanitize file path by replacing invalid characters
            var filePath = Path.Combine(path, $"{newXml.Title}.xml")
                .Replace("\"", "")
                .Replace(":", "_")
                .Replace(".", "_")
                .Replace(" ", "_");
            Console.WriteLine($"Saving {newXml.Title} to {filePath}");
            logger.LogProcessingInfo($"Saving  {newXml.Title} to {filePath}");
            WriteBPXmlEntry(newXml, filePath);
        }
    }

    private static void WriteBPXmlEntry(BPDataEntry entry, string path)
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
    private static void SavePNEntries(List<XmlDocument> xmlDocuments, string path)
    {
        logger.LogProcessingInfo("Saving PN Entries");
        Console.WriteLine("Saving Pn Entries");
        foreach (var xmlDocument in xmlDocuments)
        {
            var nsManager = new XmlNamespaceManager(xmlDocument.NameTable);
            nsManager.AddNamespace("tei", "http://www.tei-c.org/ns/1.0");

            var bpNumb = xmlDocument.SelectSingleNode("//tei:idno[@type='bp']", nsManager);

            var filePath = Path.Combine(path, $"{bpNumb.InnerText}.xml")
                .Replace("\"", "")
                .Replace(":", ".");
            Console.WriteLine($"Saving {bpNumb.InnerText} to {filePath}");
            logger.LogProcessingInfo($"Saving  {bpNumb.InnerText} to {filePath}");
            WritePNEntry(xmlDocument, filePath);
            // Assuming WriteEntry can handle XMLDataEntry or there's an overload
            // For now, casting to BPDataEntry if ToXML() is common, or create a new WriteEntry for XMLDataEntry
            // For this example, assuming XMLDataEntry has a ToXML() method similar to BPDataEntry
        }
    }

    /// <summary>
    /// Saves updated BP entries to the specified path.
    /// </summary>
    /// <param name="bpEntriesToUpdate">List of BPDataEntry objects to be saved as XML.</param>
    /// <param name="path">The directory path where the XML files will be saved.</param>
    private static void SaveBPEntries(List<UpdateDetail<BPDataEntry>> bpEntriesToUpdate, string path)
    {
        //Whedn saving save as: BP NUmber + field name + what the change will be

        logger.Log("Saving BP Entries");
        Console.WriteLine("Saving Bp Entries");
        var filePath = path + "/BPEntriesToUpdate.txt";
        var sb = new StringBuilder();
        foreach (var bpEntries in bpEntriesToUpdate)
        {
            var bpText =
                $"BP #: {bpEntries.Entry.BPNumber}. Changed {bpEntries.FieldName} _from_ [{bpEntries.OldValue ?? "[BLANK]"}] _to_ [{bpEntries.NewValue}]. ";
            sb.Append(bpText + "\n");

            logger.LogProcessingInfo($"Updated {bpText}");

            // Sanitize file path by replacing invalid characters
        }

        WriteBPEntry(sb, filePath);
    }

    private static void WriteBPEntry(StringBuilder finalTexts, string path)
    {
        try
        {
            if (File.Exists(path)) path = path.Replace(".txt", " (2).txt");
            File.WriteAllText(path, finalTexts.ToString());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static void WritePNEntry(XmlDocument xmlDocument, string path)
    {
        try

        {
            // If the file already exists, append a (2) to the filename to avoid overwriting
            if (File.Exists(path)) path = path.Replace(".xml", " (2).xml");
            var writer = new XmlTextWriter(path, Encoding.Default);
            writer.Formatting = Formatting.Indented;
            writer.Indentation = 4;

            xmlDocument.WriteTo(writer);
            writer.Flush();
            writer.Dispose();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static XmlDocument LoadAndChangeXmlDocument(XMLDataEntry entry)
    {
        var xmlDocument = new XmlDocument();
        xmlDocument.Load(entry.PNFileName);
        return ChangeXmlDocument(entry, xmlDocument);
    }

    private static XmlDocument ChangeXmlDocument(XMLDataEntry entry, XmlDocument xmlDocument)
    {
        var root = xmlDocument.DocumentElement;

        // Create namespace manager
        var nsManager = new XmlNamespaceManager(xmlDocument.NameTable);
        nsManager.AddNamespace("tei", "http://www.tei-c.org/ns/1.0");

        // Get BP number using XPath
        var bpElement = root.SelectSingleNode("//tei:idno[@type='bp']", nsManager);
        var name = root.SelectSingleNode("//tei:seg[@subtype='nom']", nsManager);
        var index = root.SelectSingleNode("//tei:seg[@subtype='index']", nsManager);
        var indexBis = root.SelectSingleNode("//tei:seg[@subtype='indexBis']", nsManager);
        var title = root.SelectSingleNode("//tei:seg[@subtype='titre']", nsManager);
        var publisher = root.SelectSingleNode("//tei:seg[@subtype='publication']", nsManager);
        var resume = root.SelectSingleNode("//tei:seg[@subtype='resume']", nsManager);
        var sbandSeg = root.SelectSingleNode("//tei:seg", nsManager);
        var cr = root.SelectSingleNode("//tei:seg[@subtype='cr']", nsManager);
        var internet = root.SelectSingleNode("//tei:seg[@subtype='internet']", nsManager);

        if (bpElement != null && entry.HasBPNum)
        {
            bpElement.InnerText = entry.BPNumber;
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
            newBpElement.InnerText = entry.BPNumber;

            // Insert as first child of root element
            root.AppendChild(newBpElement);
        }


        if (name != null && entry.HasName)
        {
            name.InnerText = entry.Name;
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
            subtypeAttr.Value = "original";
            newNameElement.Attributes.Append(type);

            // Set name text
            newNameElement.InnerText = entry.Name;

            // Insert as first child of root element
            root.AppendChild(newNameElement);
        }

        if (index != null && entry.HasIndex)
        {
            index.InnerText = entry.Index;
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
            subtypeAttr.Value = "original";
            newNameElement.Attributes.Append(type);

            // Set name text
            newNameElement.InnerText = entry.Index;

            // Insert as first child of root element
            root.AppendChild(newNameElement);
        }

        if (indexBis != null && entry.HasIndexBis)
        {
            indexBis.InnerText = entry.IndexBis;
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
            subtypeAttr.Value = "original";
            newNameElement.Attributes.Append(type);

            // Set name text
            newNameElement.InnerText = entry.IndexBis;

            // Insert as first child of root element
            root.AppendChild(newNameElement);
        }

        if (title != null && entry.HasTitle)
        {
            title.InnerText = entry.Title;
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
            subtypeAttr.Value = "original";
            newNameElement.Attributes.Append(type);

            // Set name text
            newNameElement.InnerText = entry.Title;

            // Insert as first child of root element
            root.AppendChild(newNameElement);
        }

        if (publisher != null && entry.HasPublication)
        {
            publisher.InnerText = entry.Publication;
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
            subtypeAttr.Value = "original";
            newNameElement.Attributes.Append(type);

            // Set resp attribute
            var respAttr = xmlDocument.CreateAttribute("resp");
            respAttr.Value = "#BP";
            newNameElement.Attributes.Append(respAttr);

            // Set name text
            newNameElement.InnerText = entry.Publication;

            // Insert as first child of root element
            root.AppendChild(newNameElement);

            logger.Log($"adding note entry to xml for {entry.BPNumber}");
            var noteNode = root.SelectSingleNode("//note[@resp=\"#bp\"]");
            if (noteNode != null)
            {
                noteNode.InnerText = entry.Publication;
            }
            else
            {
                var newNoteElement = xmlDocument.CreateElement("note", "http://www.tei-c.org/ns/1.0");
                newNoteElement.Attributes.Append(respAttr);
                newNoteElement.InnerText = entry.Publication ?? "NULL ERROR ON PUBLICATION ENTRY";
                root.AppendChild(newNoteElement);
            }
        }

        if (resume != null && entry.HasResume)
        {
            resume.InnerText = entry.Resume;
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
            subtypeAttr.Value = "original";
            newNameElement.Attributes.Append(type);

            // Set resp attribute
            var respAttr = xmlDocument.CreateAttribute("resp");
            respAttr.Value = "#BP";
            newNameElement.Attributes.Append(respAttr);

            // Set name text
            newNameElement.InnerText = entry.Resume;

            // Insert as first child of root element
            root.AppendChild(newNameElement);
        }

        if (sbandSeg != null && entry.HasSBandSEG)
        {
            sbandSeg.InnerText = entry.SBandSEG;
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
            subtypeAttr.Value = "original";
            newNameElement.Attributes.Append(type);

            // Set name text
            newNameElement.InnerText = entry.SBandSEG;

            // Insert as first child of root element
            root.AppendChild(newNameElement);
        }

        if (cr != null && entry.HasCR)
        {
            cr.InnerText = entry.CR;
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
            subtypeAttr.Value = "original";
            newNameElement.Attributes.Append(type);

            // Set name text
            newNameElement.InnerText = entry.CR;

            // Insert as first child of root element
            root.AppendChild(newNameElement);
        }

        if (internet != null && entry.HasInternet)
        {
            internet.InnerText = entry.Internet;
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
            subtypeAttr.Value = "original";
            newNameElement.Attributes.Append(type);

            // Set name text
            newNameElement.InnerText = entry.Internet;

            // Insert as first child of root element
            root.AppendChild(newNameElement);
        }

        return xmlDocument;
    }
}

//Debug TODO's

//Done
//TODO Add to the readme discussion of how to run  the project, make sure to talk about hte directory,
//Figure out why the white space isn't being trimmed when coming from BP
//TODO disable the name collision check. Done, added a flag to the program startup so that one can restore it if one wants
//TODO figure out why its readding elements which it shoulnd't be? -- This should've been fixed by fixing the bug where it wans't grabbing the seg stuff right.
//I believe I figured out the white space problem, now everything should be getting cut down to one space
//TODO check why 1932-0016 isn't grabbing correctly
//TODO check 1932-0019
//TODo look inot whitespace and shared whitespace stuff is the same
//TODO check the BPEntryGatherer is actually culling extra text, bcz entry 0009 is having a spacing issue.
//Big TODO's
//TODO Remove debug settings for running the program, thereby assuming its built in debug on dotnet build

//Simple TODO's

//In progress
//TODO fix the file path stuff on saving the files so that its not hte idp data directory.
//TODO Fix the file saving path stuff, mostly working, just need to get the logging lined up okay
//Then its onto file path work, fixing hte file path stuff so that its all unified and working well
//on the file path, just need to clean up the logging for it. 
//Then ocne I have the directory stuff done and tested, it'll be updating the readme with path info, fixing the debug settings
// and then we should be good with this version I think. 