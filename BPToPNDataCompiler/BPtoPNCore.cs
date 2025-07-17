using System.CommandLine;
using System.CommandLine.IO;
using System.IO.Compression;
using DefaultNamespace;

// ReSharper disable InconsistentNaming

namespace BPtoPNDataCompiler;

public class BPtoPNCore
{
    // Static fields to hold the parsed argument values
    private static int startYear = 1932;
    private static int endYear = DateTime.Now.Year - 1;
    private static int bpStartNumber = 1; // New: Default beginning number for BP data

    static int bpEndNumber = 9999; // New: Default finishing number for BP data

    /// <summary>
    /// The core logic of the application, executed after successful argument parsing.
    /// </summary>
    private static void Core()
    {
        logger?.Log("Started core");
        try
        {
            // This will check
            var startingPath = Directory.GetCurrentDirectory();
            var gitHandler = new GitFolderHandler(logger);
            //If we have the git folder. Normally will error out before this if it cannot be found.
            //AS such we'll just let hte exceptions bubble up.
            var biblioPath = gitHandler.GitBiblioDirectoryCheck(DepthLevel);
            var currentPath = Directory.GetCurrentDirectory();

            logger?.Log("Creating BpEntry Gatherer");
            Console.WriteLine("Creating BPEntry Gatherer");
            // Pass bpStartNumber and bpEndNumber to BPEntryGatherer if it needs them
            // Assuming BPEntryGatherer can be updated to accept these new parameters
            var BPEntryGatherer =
                new BPEntryGatherer(startYear, endYear, logger, bpStartNumber,
                    bpEndNumber); // You might need to add bpStartNumber, bpEndNumber here

            logger?.Log("BPEntryGather Created.\nCreating XMLEntryGatherer.");
            Console.WriteLine("BPEntryGather created. Creating XMLEntry gatherer");
            var XMLEntryGatherer = new XMLEntryGatherer(biblioPath, logger);
            logger?.Log("XML Entry Gatherer created.");

            logger?.Log("Gathering XML entries");
            Console.WriteLine("XmlEntry Gatherer created, gathering XML entries.");
            var xmlEntries = XMLEntryGatherer.GatherEntries();

            currentPath = Directory.GetCurrentDirectory();

            logger?.Log("Gathering BP entries.");
            Console.WriteLine("Gathered XMl Entries, gathering BP entries.");
            var bpEntries = BPEntryGatherer.GatherEntries(DepthLevel);

            logger?.Log("Entries are gathered");
            Console.WriteLine("Entries have been gathered.");

            Console.Write("Preparing to start data matcher. ");
            logger?.Log("Creating Datamatcher");
            var dm = new DataMatcher(xmlEntries, bpEntries, currentPath, logger, ShouldCompareName, RunDataMatcher);

            Console.WriteLine("Starting to match entries?");
            logger?.Log("Starting to match entries");
            var saveLocation = dm.MatchEntries();

            Console.WriteLine("Done matching entries. Now saving lists.");
            logger?.Log("Finished matching entries.");
            logger?.Log("Updating BPEntries before saving.");

            logger?.Log(
                "Finshied saving lists. Logger will dispose of self to allow the moving of BPXMLData to folder and BPtoPNLogs folder to final folder, then Will zip files, and then delete working areas.");
            Console.WriteLine(
                "Finshied saving lists. Logger will dispose of self to allow the moving of BPXMLData to folder and BPtoPNLogs folder to final folder, then Will zip files, and then delete working areas.");
            logger?.Dispose();
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
        var fileName = Path.GetFileName(saveLocation);
        int count = 0;

        if (Directory.GetFiles(directory).Any(x => x.Contains($"{fileName}")))
        {
            count = Directory.GetFiles(directory).Count(x => x.Contains($"{fileName}")) + 1;
        }

        var countText = count > 0 ? $" ({count})" : "";

        var zipPath = Path.Combine(directory, $"{saveLocation}{countText}.zip");

        Console.WriteLine($"Zipping files from {sourcePath} to {zipPath}");

        // Make sure the target zip file doesn't already exist
        if (File.Exists(zipPath))
        {
            //TODO confirm if we should delete
            //File.Delete(zipPath);
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

                if (File.Exists(zipPath))
                {
                    var pth = Directory.GetParent(sourcePath).FullName;
                    int cnt = Directory.GetFiles(pth).Count(x => x.Contains(saveLocation + ".zip"));
                    cnt++;
                    zipPath = zipPath.Replace(".zip", $" ({cnt}).zip");
                }

                // Create the zip file
                ZipFile.CreateFromDirectory(
                    sourcePath,
                    zipPath,
                    CompressionLevel.Optimal,
                    false); // Don't include the base directory in the archive

                Console.WriteLine($"Successfully created ZIP file at: {zipPath}");

                // Optionally delete the source directory after successful zip creation
                if (Delete) Directory.Delete(sourcePath, true);
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
        if (dirs.Contains(directory + "BPXMLFiles/"))
        {
            var tempStartDir = Path.Combine(directory, "BPXMLFiles/");
            if (dirs.Contains(saveLocation + "/BPXMLFiles"))
            {
                var count = dirs.Count(x => x.Contains(saveLocation + "\\BPXMLFiles/")) + 1;
                var tempEndDir = Path.Combine(saveLocation, $"BPXMLFiles ({count})/");
                Directory.Move(tempStartDir, tempEndDir);
            }

            else
            {
                var tempEndDir = Path.Combine(saveLocation, "BPXMLFiles/");
                Directory.Move(tempStartDir, tempEndDir);
            }
        }

        var logDirs = Directory.GetDirectories(directory);
        if (!logDirs.Any(x => x.Contains("BPtoPNLogs")))
        {
            dirs = Directory.GetDirectories(directory + "/../");
            if (dirs.Contains(directory + "/../BPtoPNLogs"))
            {
                if (dirs.Contains(saveLocation + "/BPtoPNLogs"))
                {
                    var count = dirs.Count(x => x.Contains(saveLocation + "/BPtoPNLogs/")) + 1;
                    var tempEndDir = Path.Combine(saveLocation, $"BPtoPNLogs ({count})/");
                    Directory.Move(directory + "/../BPtoPNLogs", tempEndDir);
                }

                else
                {
                    Directory.Move(directory + "/../BPtoPNLogs", $"{saveLocation}/BPtoPNLogs");
                }
            }
        }
        else
        {
            dirs = Directory.GetDirectories(saveLocation);
            if (dirs.Any(x => x.Contains("BPtoPNLogs")))
            {
                var count = dirs.Count(x => x.Contains("BPtoPNLogs")) + 1;
                var tempEndDir = Path.Combine(saveLocation, $"BPtoPNLogs ({count})/");
                Directory.Move(directory + "/BPtoPNLogs", tempEndDir);
            }
            else
            {
                Directory.Move(directory + "/BPtoPNLogs", $"{saveLocation}/BPtoPNLogs");
            }
        }
    }

    #region main and arg parsing

    public static Logger? logger { get; private set; }
    private static bool ShouldCompareName { get; set; } = false;
    public static string DepthLevel = "/..";
    private static bool Delete = false;
    public static bool RunDataMatcher = false;

    public static void Main(string[] args)
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
            startYearOption.AddAlias("-t");

            var shouldCompareAuthorNames = new Option<bool>(
                name: "--compare-author-names",
                getDefaultValue: () => ShouldCompareName,
                description:
                "Sets if the author names should be a field to be compared. Defaults to false."
            );
            shouldCompareAuthorNames.AddAlias("-c"); // Add alias using AddAlias method

            var endYearOption = new Option<int>(
                name: "--end-year",
                getDefaultValue: () => endYear,
                description:
                $"Sets the end year for data compilation. Use -e or --end-year. Default is the current system year -1 (Currently: {DateTime.Now.Year - 1}). Cannot be lower than the start year."
            );
            endYearOption.AddAlias("-e"); // Add alias using AddAlias method
            endYearOption.AddAlias("-d"); // Add alias using AddAlias method


            var noDelete = new Option<bool>(
                name: "delete",
                getDefaultValue: () => Delete,
                description:
                $"If used, will delete the resulting folder that is zipped. By default this folder is zipped and not deleted after the program is run"
            );
            noDelete.AddAlias("-nd");

            var bpStartNumberOption = new Option<int>(
                name: "--bp-start-number",
                getDefaultValue: () => bpStartNumber,
                description:
                "Sets the beginning number for BP data processing. Use -bps or --bp-start-number. Default is 0. Cannot be negative."
            );
            bpStartNumberOption.AddAlias("-bps"); // Add alias using AddAlias method
            bpStartNumberOption.AddAlias("-b"); // Add alias using AddAlias method

            var noDataMatcher = new Option<bool>(
                name: "--no-data-matcher",
                getDefaultValue: () => RunDataMatcher,
                description:
                "Disables the data matcher ui, basically just running a count without having the user do anything"
            );

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
                    noDataMatcher,
                };


            // Set the handler for the root command. This action will be executed when the command is invoked.
            rootCommand.SetHandler((context) =>
            {
                var showHelp = context.ParseResult.GetValueForOption(helpOption);
                if (showHelp)
                {
                    if (rootCommand.Description != null) context.Console.Out.WriteLine(rootCommand.Description);
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
                Delete = context.ParseResult.GetValueForOption(noDelete);
                RunDataMatcher = context.ParseResult.GetValueForOption(noDataMatcher);

                // Perform custom validation after parsing
                ValidateYears();
                ValidateBpNumbers();

                logger.Log("Parsing args completed.");
                Console.WriteLine($"Args parsed. Start Year: {startYear}, End Year: {endYear}.");
                Console.WriteLine($"BP Start Number: {bpStartNumber}, BP End Number: {bpEndNumber}.");
                logger.Log($"Start Year: {startYear}, End Year: {endYear}");
                logger.Log($"BP Start Number: {bpStartNumber}, BP End Number: {bpEndNumber}");

                Console.WriteLine("Have you pulled the latest version of IDP_DATA? (y/n)");
                var input = Console.ReadLine().ToLower();
                if (input == "y")
                {
                    // If all validations pass, proceed with the core application logic
                    Core();
                }
                else
                {
                    Console.WriteLine("If you don't have the most up to date info, this program should not run. " +
                                      "Please git pull for the newest info and then run me.");
                }
            });


            // Invoke the command line parser with the provided arguments
            // System.CommandLine will automatically handle help (-h or --help) and validation errors.
            rootCommand.Invoke(args);
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
            logger?.LogError("Error: End year cannot be greater than start year.", new ArgumentException());
            throw new ArgumentException("Error: End year cannot be greater than start year.");
        }

        if (startYear < 1932)
        {
            logger?.LogError("Error: Start year cannot be less than 1932.", new ArgumentException());
            throw new ArgumentException("Error: Start year cannot be less than 1932.");
        }

        if (endYear > DateTime.Now.Year - 1)
        {
            logger?.LogError(
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
            logger?.LogError("Error: BP start number cannot be negative.", new ArgumentException());
            throw new ArgumentException("Error: BP start number cannot be negative.");
        }

        if (bpEndNumber < bpStartNumber)
        {
            logger?.LogError("Error: BP end number cannot be less than BP start number.", new ArgumentException());
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
        logger?.LogError("Error: ", e);
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    // The old ParseArgs and ShowHelp methods are removed as System.CommandLine handles this.

    #endregion
}




//Make sure that the save routine to make sure its running evne if the data matcherUI doesn't launch
//Test 2020-20 2020-30