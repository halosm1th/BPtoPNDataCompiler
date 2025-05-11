namespace DefaultNamespace;

public class GitFolderHandler
{
    private static readonly string IDP_DATA_DATA = "idp.data";
    private static readonly string BIBLIO = "Biblio";

    public bool GitBiblioDirectoryCheck()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        //If the current directory isn't the IDP data or biblio directory, throw an error
        if (!currentDirectory.Contains(IDP_DATA_DATA) && !currentDirectory.Contains(BIBLIO))
        {
            //check if a subdirectory is actually the IDP directory, and if so, lets move to that.
            //We can check the idp directory for the biblio directory in a bit
            var avaiableDirs = Directory.GetDirectories(currentDirectory);
            if (avaiableDirs.Any(x => x.Contains(IDP_DATA_DATA)))
            {
                currentDirectory = avaiableDirs.First(x => x.Contains(IDP_DATA_DATA));
                Directory.SetCurrentDirectory(currentDirectory);
            }
            else
            {
                throw new DirectoryNotFoundException("Error cannot find the directory idp.data.\n " +
                                                     "Please run the program either in the idp.data directory, " +
                                                     "or in the parent directory of idp.data.\n" +
                                                     "If you do not have the idp.data, get it from: https://github.com/papyri/idp.data" +
                                                     $"The current directory is: {currentDirectory}.");
            }
        }

        return BiblioDirectoryCheck(currentDirectory);
    }

    private bool BiblioDirectoryCheck(string currentDirectory)
    {
        //Get all the folders in our current folder, which should be the idp.data folder. 
        var availableDirectors = Directory.GetDirectories(currentDirectory);

        //Check if hte directory we're in has the biblio directory
        if (availableDirectors.Any(x => x.Contains(BIBLIO)))
        {
            //if it does, change the current directory to the biblio dir
            currentDirectory = availableDirectors.First(x => x.Contains(BIBLIO));
            Directory.SetCurrentDirectory(currentDirectory);
            return true;
        }

        //if we can't find the biblio directory, throw an error
        throw new DirectoryNotFoundException("Error the biblio folder could be found. " +
                                             "Pleasure ensure the ipd.data directory contains" +
                                             " the biblio directory.");

        return false;
    }
}