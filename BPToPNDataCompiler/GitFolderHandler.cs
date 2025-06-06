namespace DefaultNamespace;

public class GitFolderHandler
{
    private static readonly string IDP_DATA_DATA = "idp.data";
    private static readonly string BIBLIO = "Biblio";

    public GitFolderHandler(Logger logger)
    {
        this.logger = logger;
    }

    private Logger logger { get; set; }

    public string GitBiblioDirectoryCheck()
    {
        //TODO FIx file path going up
        //TODO make sure that this file path change works
        logger.Log($"Moving up from program directory before starting search. ({Directory.GetCurrentDirectory()})");
        Directory.SetCurrentDirectory(Directory.GetCurrentDirectory() + "/../../");
        Console.WriteLine($"{Directory.GetCurrentDirectory()}");
        logger.Log("Checking if PN folders exist");
        Console.WriteLine("Checking if PN folders exist.");
        var currentDirectory = ScanForGitDirectory();

        Console.WriteLine("Checking if Biblio Directory exists.");
        return BiblioDirectoryCheck(currentDirectory);
    }

    private string ScanForGitDirectory()
    {
        logger.LogProcessingInfo("Scanning for git directors");
        var currentDirectory = Directory.GetCurrentDirectory();
        logger.LogProcessingInfo($"Current directory: {currentDirectory}");
        //If the current directory isn't the IDP data or biblio directory, throw an error
        if (!currentDirectory.Contains(IDP_DATA_DATA) && !currentDirectory.Contains(BIBLIO))
        {
            logger.LogProcessingInfo(
                "Current path does not contain IDP_DATA folder or Biblio folder.\nChecking subdirectory folders");
            //check if a subdirectory is actually the IDP directory, and if so, lets move to that.
            //We can check the idp directory for the biblio directory in a bit
            var avaiableDirs = Directory.GetDirectories(currentDirectory);
            if (avaiableDirs.Any(x => x.Contains(IDP_DATA_DATA)))
            {
                logger.LogProcessingInfo("Found IDP_DATAchanging directory to that folder.");
                logger.Log("IDP-Data found, changing to that directory.");
                currentDirectory = avaiableDirs.First(x => x.Contains(IDP_DATA_DATA));
                Directory.SetCurrentDirectory(currentDirectory);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(
                    $"idp.data directory has been found. Program working directory set to: {currentDirectory}");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            else
            {
                logger.Log("Could not find IDP-data directory");
                logger.LogProcessingInfo("Could not find IDP-Data directory.");
                throw new DirectoryNotFoundException("Error cannot find the directory idp.data.\n " +
                                                     "Please run the program either in the idp.data directory, " +
                                                     "or in the parent directory of idp.data.\n" +
                                                     "If you do not have the idp.data, get it from: https://github.com/papyri/idp.data " +
                                                     $"The current directory is: {currentDirectory}.");
            }
        }

        return currentDirectory;
    }

    private string BiblioDirectoryCheck(string currentDirectory)
    {
        logger.LogProcessingInfo("Finding biblio directory");
        logger.Log("Finding biblio directory");
        //Get all the folders in our current folder, which should be the idp.data folder. 
        var availableDirectors = Directory.GetDirectories(currentDirectory);


        //Check if hte directory we're in has the biblio directory
        if (availableDirectors.Any(x => x.Contains(BIBLIO)))
        {
            //if it does, change the current directory to the biblio dir
            currentDirectory = availableDirectors.First(x => x.Contains(BIBLIO));
            Directory.SetCurrentDirectory(currentDirectory);
            logger.LogProcessingInfo($"Biblio directory found, changing to that directory ({currentDirectory}).");
            logger.Log("Found biblio directory, changing to it.");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Git Biblio has been found. Program working directory set to: {currentDirectory}");
            Console.ForegroundColor = ConsoleColor.Gray;
            return currentDirectory;
        }

        logger.Log("Could not find biblio directory");
        logger.LogProcessingInfo("Could not find biblio directory");
        //if we can't find the biblio directory, throw an error
        throw new DirectoryNotFoundException("Error the biblio folder could be found. " +
                                             "Pleasure ensure the ipd.data directory contains" +
                                             " the biblio directory.");
    }
}