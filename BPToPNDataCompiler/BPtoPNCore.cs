using System.Text.RegularExpressions;

namespace DefaultNamespace;

public class BPtoPNCore
{
    private static int startYear = 1932;
#if DEBUG
    private static int endYear = 1932;
#else
    private static int endYear = DateTime.Now.Year - 1;
#endif

    #region main and arg parsing

    public static async Task Main(string[] args)
    {
        try
        {
            //Check the args we got.
            //Check that we have the Biblio data from PN.
            var shouldRun = ParseArgs(args);
            if (startYear > endYear)
            {
                throw new ArgumentException("Error end year cannot be greater than start year.");
            }

            if (startYear < 1932)
            {
                throw new ArgumentException("Error, start year cannot be less than 1932.");
            }

            if (endYear > DateTime.Now.Year - 1)
            {
                throw new ArgumentException(
                    $"Error, the end year cannot be greater than the current system year -1 (Currently: {DateTime.Now.Year - 1})");
            }

            if (shouldRun) Core();
        }
        catch (ArgumentException e)
        {
            ExceptionInfo(e);
        }
        catch (DirectoryNotFoundException e)
        {
            ExceptionInfo(e);
        }
        catch (Exception e)
        {
            ExceptionInfo(e);
        }
    }

    private static void ExceptionInfo(Exception e)
    {
        //Just a nice little way for us to bubble any errors we run into up and to the user, to be handled with ease.
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(e.Message);
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    private static bool ParseArgs(string[] args)
    {
        //First case is no args, or the help menu arg
        if (args.Length == 0) return true;
        if ((args.Length == 1 && (args[0].ToLower() == "-h" || args[0].ToLower() == "help")))
        {
            ShowHelp();
            return false;
        }

        //If its not the help menu, check if its a number, and if so assume that is the start year
        if (args.Length == 1)
        {
            var regex = new Regex(@"(19|20)\d\d");
            if (regex.IsMatch(args[0]))
            {
                if (int.TryParse(args[0], out startYear))
                {
                    return true;
                }

                throw new ArgumentException($"Error, {args[0]} is not an accepted value. " +
                                            $"Please enter a number between 1932-{DateTime.Now.Year - 1}\nuse -h or help " +
                                            $"for more information.");
            }
            else
            {
                throw new ArgumentException(
                    $"Error, {args[0]} is not an accepted value. Please enter a number between 1932-{DateTime.Now.Year - 1}\nuse -h or help " +
                    $"for more information.");
            }
        }

        //Two args
        //Options are:
        //1) -s {number}
        //2) start {number}
        //3) -e {number}
        //4) end {number}
        //5) {number} {number}
        if (args.Length == 2)
        {
            var firstArg = args[0];
            var secondArg = args[1];
            var numbRegex = new Regex(@"(19|20)\d\d");

            if (firstArg.ToLower() == "-s" || firstArg.ToLower() == "start")
            {
                if (numbRegex.IsMatch(secondArg))
                {
                    if (int.TryParse(secondArg, out startYear))
                    {
                        if (startYear > endYear)
                        {
                            endYear = startYear + 1;
                        }

                        return true;
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"Error, value {secondArg} is invalid. It must be a number between 1932-{DateTime.Now.Year - 1}");
                    }
                }

                else
                {
                    throw new ArgumentException(
                        $"Error, value {secondArg} is invalid. It must be a number between 1932-{DateTime.Now.Year - 1}");
                }
            }

            if (firstArg.ToLower() == "-e" || firstArg.ToLower() == "end")
            {
                if (numbRegex.IsMatch(secondArg))
                {
                    if (int.TryParse(secondArg, out endYear))
                    {
                        if (endYear < startYear)
                        {
                            throw new ArgumentException(
                                $"The end year has to be higher than the start year. The default start year is 1932. You entered {secondArg}");
                        }

                        return true;
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"Error, value {secondArg} is invalid. It must be a number between 1932-{DateTime.Now.Year - 1}");
                    }
                }

                else
                {
                    throw new ArgumentException(
                        $"Error, value {secondArg} is invalid. It must be a number between 1932-{DateTime.Now.Year - 1}");
                }
            }
            else if (numbRegex.IsMatch(firstArg) && numbRegex.IsMatch(secondArg))
            {
                if (int.TryParse(firstArg, out startYear) && int.TryParse(secondArg, out endYear))
                {
                    if (startYear > endYear)
                    {
                        throw new ArgumentException(
                            $"Error start year ({startYear}) cannot be larger than end year ({endYear})");
                    }

                    return true;
                }
                else
                {
                    throw new ArgumentException(
                        $"Error, value {secondArg} is invalid. It must be a number between 1932-{DateTime.Now.Year - 1}");
                }
            }
            else
            {
                throw new ArgumentException($"Error, invalid argument {firstArg}, could not be parsed.");
                return false;
            }
        }

        //Valid options are:
        //1) {number} end {number}
        //2) {number} -e {number}
        else if (args.Length == 3)
        {
            var firstArg = args[0];
            var secondArg = args[1];
            var thirdArg = args[2];
            var letterRegex = new Regex("(-e|end)");
            var numbRegex = new Regex(@"(19|20)\d\d");

            if (numbRegex.IsMatch(firstArg) && letterRegex.IsMatch(secondArg) && numbRegex.IsMatch(thirdArg))
            {
                if (int.TryParse(firstArg, out startYear) && int.TryParse(thirdArg, out endYear))
                {
                    return true;
                }
                else
                {
                    throw new ArgumentException("Error, one of the entered numbers could not be parsed.");
                }
            }
            else
            {
                throw new ArgumentException($"Error, one of your arguments is invalid. See -h for more info.");
                return false;
            }
        }

        //options
        //1) -s {number} -e {number}
        //2) start {number} end {number} 
        //3) -e {number} -s {number}
        //4) end {number} start {number}
        else if (args.Length == 4)
        {
            var firstArg = args[0];
            var secondArg = args[1];
            var thirdArg = args[2];
            var forthArg = args[3];

            var letterRegex = new Regex("(-e|end|-s|start)");
            var numberRegex = new Regex(@"(19|20)\d\d");

            if (letterRegex.IsMatch(firstArg) && letterRegex.IsMatch(thirdArg)
                                              && numberRegex.IsMatch(secondArg) && numberRegex.IsMatch(forthArg))
            {
                if (firstArg == "-s" || firstArg == "start")
                {
                    if (int.TryParse(secondArg, out startYear) && int.TryParse(forthArg, out endYear))
                    {
                        return true;
                    }
                    else
                    {
                        throw new ArgumentException("Error could not parse supplied numbers");
                    }
                }

                if (firstArg == "-e" || firstArg == "end")
                {
                    if (int.TryParse(secondArg, out endYear) && int.TryParse(forthArg, out startYear))
                    {
                        return true;
                    }
                    else
                    {
                        throw new ArgumentException("Error could not parse supplied numbers");
                    }
                }


                else
                {
                    throw new ArgumentException(
                        "Error, the arguments were not valid. Try -h for a list of valid arguments");
                }
            }
            else
            {
                throw new ArgumentException(
                    "Error, the arguments were not valid. Try -h for a list of valid arguments");
            }
        }

        else
        {
            throw new ArgumentException("Error, too many arguments. Use -h to see all valid argument combintations.");
        }

        return true;
    }

    private static void ShowHelp()
    {
        Console.WriteLine(
            "This data compiler must be run in the ipd.data folder, or its parent folder, and the idp.data folder must contain the biblio folder for this to work." +
            "\nIf you do not have these files, download them from: https://github.com/papyri/idp.data");
        Console.WriteLine("---------------------");
        Console.WriteLine("Options:");
        Console.WriteLine(
            $" -- No args will run the program with the default start and end years ({startYear}, {endYear})");
        Console.WriteLine("-h || help -- Displays this list.");
        Console.WriteLine("{number} -- sets the start year to number, default end year is the current system year -1.");

        Console.WriteLine(
            "-s {number} || start {number} -- set the start year to pull from. Default is 1932, which the start can not be less than");
        Console.WriteLine(
            "-e {number} || end {number} -- set the end year to pull from. Default is the current system year -1. Cannot be lower than the start year.");
        Console.WriteLine("{number} {number} -- sets the start year and the end year.");

        Console.WriteLine(
            "{number} -e {number} -- sets the start year to the first number, and the end year to the second");
        Console.WriteLine(
            "{number} end {number} -- sets the start year to the first number, and the end year to the second");

        Console.WriteLine("-s {number} -e {number} -- sets the start year and end year.");
        Console.WriteLine("start {number} end {number} -- sets the start year and end year.");
        Console.WriteLine("-e {number} -s {number} -- sets the start year and end year.");
        Console.WriteLine("end {number} start {number} -- sets the start year and end year.");
    }

    #endregion

    private static async Task Core()
    {
        Console.WriteLine($"Args parsed. Start Year: {startYear}. End Year: {endYear}.");

        //This will check 
        var gitHandler = new GitFolderHandler();
        //If we have the git folder. Normally will error out before this if it cannot be found. 
        //AS such we'll just let hte exceptions bubble up.
        var biblioPath = gitHandler.GitBiblioDirectoryCheck();


        Console.Write("Creating BPEntryGatherer. ");
        var BPEntryGatherer = new BPEntryGatherer(startYear, endYear);
        Console.Write("BPEntryGather Created.\nCreating XMLEntryGatherer. ");
        var XMLEntryGatherer = new XMLEntryGatherer(biblioPath);
        Console.WriteLine("XML Entry Gatherer created.  ");

        var bpEntryTask = BPEntryGatherer.GatherEntries();
        var xmlEntryTask = XMLEntryGatherer.GatherEntries();
        Console.WriteLine("Gathered the stuff");


        Console.Write("Preparing to start data matcher. ");
        var dm = new DataMatcher(await xmlEntryTask, await bpEntryTask);
        Console.WriteLine("Starting to match entries?");
        dm.MatchEntries();
        Console.WriteLine("Done matching entries. press any key to exit.");
        Console.ReadLine();
    }
}