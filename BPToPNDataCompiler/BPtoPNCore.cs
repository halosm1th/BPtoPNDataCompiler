﻿using System.IO.Compression;
using System.Text.RegularExpressions;
using BPtoPNDataCompiler;
using System.CommandLine; // Required for command-line argument parsing
using System.CommandLine.Parsing; // Required for parsing results

namespace DefaultNamespace;

public class BPtoPNCore
{
    // Static fields to hold the parsed argument values
    private static int startYear = 1932;
#if DEBUG
    private static int endYear = 1932;
#else
    private static int endYear = DateTime.Now.Year - 1;
#endif
    private static int bpStartNumber = 1; // New: Default beginning number for BP data
    private static int bpEndNumber = 9999; // New: Default finishing number for BP data

    #region main and arg parsing

    public static Logger logger { get; private set; }

    public static async Task Main(string[] args)
    {
        logger = new Logger();
        try
        {
            // Define command-line options for the application
            var startYearOption = new Option<int>(
                name: "--start-year",
                getDefaultValue: () => 1932,
                description: "Sets the start year for data compilation. Use -s or --start-year. Default is 1932. Cannot be less than 1932."
            );
            startYearOption.AddAlias("-s"); // Add alias using AddAlias method

            var endYearOption = new Option<int>(
                name: "--end-year",
                getDefaultValue: () => DateTime.Now.Year - 1,
                description: $"Sets the end year for data compilation. Use -e or --end-year. Default is the current system year -1 (Currently: {DateTime.Now.Year - 1}). Cannot be lower than the start year."
            );
            endYearOption.AddAlias("-e"); // Add alias using AddAlias method

            var bpStartNumberOption = new Option<int>(
                name: "--bp-start-number",
                getDefaultValue: () => 0,
                description: "Sets the beginning number for BP data processing. Use -bps or --bp-start-number. Default is 0. Cannot be negative."
            );
            bpStartNumberOption.AddAlias("-bps"); // Add alias using AddAlias method
            bpStartNumberOption.AddAlias("-b"); // Add alias using AddAlias method

            var bpEndNumberOption = new Option<int>(
                name: "--bp-end-number",
                getDefaultValue: () => int.MaxValue,
                description: "Sets the finishing number for BP data processing. Use -bpe or --bp-end-number. Default is maximum integer value. Cannot be less than the BP start number."
            );
            bpEndNumberOption.AddAlias("-bpe"); // Add alias using AddAlias method
            bpEndNumberOption.AddAlias("-f"); // Add alias using AddAlias method

            // Create the root command for the application
            var rootCommand = new RootCommand("BP to PN Data Compiler: Compiles and updates bibliographic data from BP and PN sources.")
            {
                startYearOption,
                endYearOption,
                bpStartNumberOption,
                bpEndNumberOption
            };

            // Set the handler for the root command. This action will be executed when the command is invoked.
            rootCommand.SetHandler(async (context) =>
            {
                // Retrieve the parsed values for each option
                startYear = context.ParseResult.GetValueForOption(startYearOption);
                endYear = context.ParseResult.GetValueForOption(endYearOption);
                bpStartNumber = context.ParseResult.GetValueForOption(bpStartNumberOption);
                bpEndNumber = context.ParseResult.GetValueForOption(bpEndNumberOption);

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
            logger.LogError($"Error: The end year cannot be greater than the current system year -1 (Currently: {DateTime.Now.Year - 1})", new ArgumentException());
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
            var gitHandler = new GitFolderHandler(logger);
            //If we have the git folder. Normally will error out before this if it cannot be found.
            //AS such we'll just let hte exceptions bubble up.
            var biblioPath = gitHandler.GitBiblioDirectoryCheck();

            logger.Log("Creating BpEntry Gatherer");
            Console.WriteLine("Creating BPEntry Gatherer");
            // Pass bpStartNumber and bpEndNumber to BPEntryGatherer if it needs them
            // Assuming BPEntryGatherer can be updated to accept these new parameters
            var BPEntryGatherer = new BPEntryGatherer(startYear, endYear, logger, bpStartNumber, bpEndNumber); // You might need to add bpStartNumber, bpEndNumber here

            logger.Log("BPEntryGather Created.\nCreating XMLEntryGatherer.");
            Console.WriteLine("BPEntryGather created. Creating XMLEntry gatherer");
            var XMLEntryGatherer = new XMLEntryGatherer(biblioPath, logger);
            logger.Log("XML Entry Gatherer created.");

            logger.Log("Gathering XML entries");
            Console.WriteLine("XmlEntry Gatherer created, gathering XML entries.");
            var xmlEntries = XMLEntryGatherer.GatherEntries();
            logger.Log("Gathering BP entries.");
            Console.WriteLine("Gathered XMl Entries, gathering BP entries.");
            var bpEntries = BPEntryGatherer.GatherEntries();

            logger.Log("Entries are gathered");
            Console.WriteLine("Entries have been gathered.");

            Console.Write("Preparing to start data matcher. ");
            logger.Log("Creating Datamatcher");
            var dm = new DataMatcher(xmlEntries, bpEntries, logger);

            Console.WriteLine("Starting to match entries?");
            logger.Log("Starting to match entries");
            dm.MatchEntries();

            Console.WriteLine("Done matching entries. Now saving lists.");
            logger.Log("Finished matching entries.");
            logger.Log("Updating BPEntries before saving.");
            var BpEntries = UpdateBpEntries(dm.BpEntriesToUpdate).ToList();
            logger.Log("Updating PnEntries before saving.");
            var PnEntries = UpdatePnEntries(dm.PnEntriesToUpdate).ToList();

            logger.Log("Saving lists.");
            var saveLocation = SaveLists(BpEntries, PnEntries, dm.NewXmlEntriesToAdd);
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
                // Directory.Delete(sourcePath, true);
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
        Console.WriteLine(Directory.GetCurrentDirectory());
        var directory = Directory.GetCurrentDirectory();
        var dirs = Directory.GetDirectories(directory);
        if (dirs.Contains(directory + "\\BPXMLFiles"))
        {
            Directory.Move(directory + "/BPXMLFiles/", directory + $"/{saveLocation}/BPXMLFiles");
        }

        if (!Directory.GetDirectories(directory).Any(x => x == "BPtoPNLogs"))
        {
            dirs = Directory.GetDirectories(directory + "/../");
            if (dirs.Contains(directory + "/../BPtoPNLogs"))
            {
                Directory.Move(directory + "/../BPtoPNLogs", directory + $"/{saveLocation}/BPtoPNLogs");
            }
        }
        else
        {
            Directory.Move(directory + "/BPtoPNLogs", directory + $"/{saveLocation}/BPtoPNLogs");
        }
    }

    /// <summary>
    /// Updates PN entries based on the provided update details.
    /// </summary>
    /// <param name="pnEntriesNeedingUpdates">List of update details for PN entries.</param>
    /// <returns>An enumerable of updated XMLDataEntry objects.</returns>
    private static IEnumerable<XMLDataEntry> UpdatePnEntries(List<UpdateDetail<XMLDataEntry>> pnEntriesNeedingUpdates)
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
        return fixedEntries;
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
    /// <param name="PnEntriesToUpdate">List of PN entries to update.</param>
    /// <param name="NewXmlEntriesToAdd">List of new XML entries to add.</param>
    /// <returns>The name of the folder where the data was saved.</returns>
    private static string SaveLists(List<BPDataEntry> BpEntriesToUpdate,
        List<XMLDataEntry> PnEntriesToUpdate, List<BPDataEntry> NewXmlEntriesToAdd)
    {
        logger.LogProcessingInfo("Creating paths for saving lists.");
        var EndDataFolder = $"BpToPnChecker-{DateTime.Now:yyyyMMdd-HHmmss}"; // Use specific format for folder name
        var BPEntryPath = Path.Combine(EndDataFolder, $"BPEntriesToUpdate-{DateTime.Now:yyyyMMdd-HHmmss}");
        var PnEntryPath = Path.Combine(EndDataFolder, $"PNEntriesToUpdate-{DateTime.Now:yyyyMMdd-HHmmss}");
        var NewXmlEntryPath = Path.Combine(EndDataFolder, $"NewXmlEntries-{DateTime.Now:yyyyMMdd-HHmmss}");

        logger.Log("Setting up directories for saving.");
        SetupDirectoriesForSaving(EndDataFolder, BPEntryPath, PnEntryPath, NewXmlEntryPath);

        logger.Log("Saving Bp Entries.");
        SaveBPEntries(BpEntriesToUpdate, BPEntryPath);

        logger.Log("Saving Pn Entries.");
        SavePNEntries(PnEntriesToUpdate, PnEntryPath);

        logger.Log("Saving XML of new entries.");
        SaveNewXMlEntries(NewXmlEntriesToAdd, NewXmlEntryPath);

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
        if (Directory.GetCurrentDirectory().Contains("Biblio")) Directory.SetCurrentDirectory("..");

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
    private static void SaveNewXMlEntries(List<BPDataEntry> NewXmlEntriesToAdd, string path)
    {
        logger.LogProcessingInfo("Saving XMl Entries..");
        Console.WriteLine("Saving Xml Entries");
        foreach (var newXml in NewXmlEntriesToAdd)
        {
            // Sanitize file path by replacing invalid characters
            var filePath = Path.Combine(path, $"{newXml.Title}.xml")
                               .Replace("\"", "")
                               .Replace(":", ".");
            Console.WriteLine($"Saving {newXml.Title} to {filePath}");
            logger.LogProcessingInfo($"Saving  {newXml.Title} to {filePath}");
            WriteEntry(newXml, filePath);
        }
    }

    /// <summary>
    /// Saves updated PN entries to the specified path.
    /// </summary>
    /// <param name="pnEntriesToUpdate">List of XMLDataEntry objects to be saved as XML.</param>
    /// <param name="path">The directory path where the XML files will be saved.</param>
    private static void SavePNEntries(List<XMLDataEntry> pnEntriesToUpdate, string path)
    {
        logger.LogProcessingInfo("Saving PN Entries");
        Console.WriteLine("Saving Pn Entries");
        foreach (var pnEntries in pnEntriesToUpdate)
        {
            // Sanitize file path by replacing invalid characters
            var filePath = Path.Combine(path, $"{pnEntries.PNNumber}.xml")
                               .Replace("\"", "")
                               .Replace(":", ".");
            Console.WriteLine($"Saving {pnEntries.PNNumber} to {filePath}");
            logger.LogProcessingInfo($"Saving  {pnEntries.PNNumber} to {filePath}");
            // Assuming WriteEntry can handle XMLDataEntry or there's an overload
            // For now, casting to BPDataEntry if ToXML() is common, or create a new WriteEntry for XMLDataEntry
            // For this example, assuming XMLDataEntry has a ToXML() method similar to BPDataEntry
            WriteEntry(pnEntries, filePath); // This might need an overload or conversion
        }
    }

    /// <summary>
    /// Saves updated BP entries to the specified path.
    /// </summary>
    /// <param name="bpEntriesToUpdate">List of BPDataEntry objects to be saved as XML.</param>
    /// <param name="path">The directory path where the XML files will be saved.</param>
    private static void SaveBPEntries(List<BPDataEntry> bpEntriesToUpdate, string path)
    {
        logger.Log("Saving BP Entries");
        Console.WriteLine("Saving Bp Entries");
        foreach (var bpEntries in bpEntriesToUpdate)
        {
            // Sanitize file path by replacing invalid characters
            var filePath = Path.Combine(path, $"{bpEntries.Title}.xml")
                               .Replace("\"", "")
                               .Replace(":", ".");
            Console.WriteLine($"Saving {bpEntries.Title} to {filePath}");
            logger.LogProcessingInfo($"Saving  {bpEntries.Title} to {filePath}");
            WriteEntry(bpEntries, filePath);
        }
    }

    /// <summary>
    /// Writes a BPDataEntry object to an XML file.
    /// </summary>
    /// <param name="entry">The BPDataEntry object to write.</param>
    /// <param name="path">The file path where the XML will be saved.</param>
    private static void WriteEntry(BPDataEntry entry, string path)
    {
        var xml = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                  $"<bibl xmlns=\"http://www.tei-c.org/ns/1.0\" xml:id=\"{entry.Title}+{entry.Publication}\" type=\"book\">\n" +
                  $"{entry.ToXML()}" +
                  $"\n</bibl>";

        try
        {
            // If the file already exists, append a (2) to the filename to avoid overwriting
            if (File.Exists(path)) path = path.Replace(".xml", " (2).xml");
            File.WriteAllText(path, xml);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    /// <summary>
    /// Overload for WriteEntry to handle XMLDataEntry, assuming it also has a ToXML() method.
    /// If XMLDataEntry does not have ToXML(), this method will need to be adjusted
    /// to generate XML based on XMLDataEntry's properties.
    /// </summary>
    /// <param name="entry">The XMLDataEntry object to write.</param>
    /// <param name="path">The file path where the XML will be saved.</param>
    private static void WriteEntry(XMLDataEntry entry, string path)
    {
        // This assumes XMLDataEntry also has a ToXML() method that produces the desired XML fragment.
        // If not, you'll need to manually construct the XML string from XMLDataEntry's properties.
        var xml = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                  $"<bibl xmlns=\"http://www.tei-c.org/ns/1.0\" xml:id=\"{entry.Title}+{entry.Publication}\" type=\"book\">\n" +
                  $"{entry.ToXML()}" + // Assuming ToXML() exists for XMLDataEntry
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
}
