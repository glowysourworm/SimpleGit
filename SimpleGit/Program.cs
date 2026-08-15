using System.Diagnostics;

using LibGit2Sharp;

using SimpleWpf.Extensions;

namespace SimpleGit
{
    public class Program
    {
        private static readonly string GIT = "git.exe";

        static void Output(string message, bool endOfLine = true, ConsoleColor foreground = ConsoleColor.White)
        {
            if (Console.ForegroundColor != foreground)
                Console.ForegroundColor = foreground;

            if (endOfLine)
                Console.WriteLine(message);

            else
                Console.Write(message);
        }

        /// <summary>
        /// Calls terminal with command line; and outputs to the callback. Return true if exit code was zero.
        /// </summary>
        static bool Call(string executable, string arguments)
        {
            try
            {
                var process = new Process();
                process.StartInfo.FileName = executable;                   // Assume Environment Variables ($Path) contains the process
                process.StartInfo.Arguments = arguments;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = true;                   // Assumes shell enviroment (probably the terminal windows is used to, not ours)

                // Info
                process.OutputDataReceived += (message, e) =>
                {
                    Output(e.Data, true, ConsoleColor.Yellow);
                };

                // Error
                process.ErrorDataReceived += (message, e) =>
                {
                    Output(e.Data, true, ConsoleColor.Red);
                };

                process.Start();
                process.WaitForExit();

                Output("Git process exited with code " + process.ExitCode);

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Output("Error running command:  " + executable + " " + arguments);
                Output("");
                Output(ex.Message);

                return false;
            }
        }

        /// <summary>
        /// Returns paths to all git repositories in base directory - recursing only at the top level.
        /// </summary>
        static IEnumerable<GitRepository> GetRepositories(string baseDirectory)
        {
            // Libgit2Sharp:  This project (at the time SimpleGit was built) was very new. So, the
            //                use of their proxy, and unmanaged libraries, wasn't showing all the 
            //                problems as simple log callbacks. 
            //
            //                That's a little too frustrating when trying to manage a git repo. So,
            //                our use here is for local directories to gather information. Then, we'll
            //                call git.exe on the command line.
            //

            var result = new List<GitRepository>();

            foreach (var directory in Directory.GetDirectories(baseDirectory))
            {
                var gitPath = Path.Combine(directory, ".git");

                // Check for .git folder
                if (Directory.Exists(gitPath))
                {
                    using (var gitRepo = new Repository(gitPath))
                    {
                        var repository = new GitRepository()
                        {
                            Name = Directory.GetParent(gitPath).Name,           // Implicit git repo name (not usually stored in the .git repo information)                            
                            GitPath = gitPath,
                            IsFork = false,
                            LastCommit = gitRepo.Head.Tip.Author.When,
                            LastFetch = Directory.GetLastWriteTime(gitPath),    // Still looking for way to get last fetch from git
                            Size = 0,
                            WorkingDirectory = gitRepo.Info.WorkingDirectory,
                            Remotes = gitRepo.Network.Remotes.Select(gitRemote =>
                            {
                                return new GitRemote()
                                {
                                    Name = gitRemote.Name,
                                    Url = gitRemote.Url
                                };

                            }).ToList()
                        };

                        result.Add(repository);
                    }
                }
            }

            return result;
        }

        static void PrintHelp()
        {
            Output("Welcome to SimpleGit! This application operates on your base git folder to manage multiple git repositories.");
            Output("");
            Output("Usage:  SimpleGit.exe [options] [directory] (defaults to current directory)");
            Output("");
            Output("Options:");
            Output("\t -list \t\t\t Outputs list of all repositories in this folder's top level (recurses only top level directories)");
            Output("\t -fetch [options] \t Fetches from all remotes for the repositories listed by the -list command");
            Output("");
        }

        static string? GetArgument(string argument, string[] args)
        {
            for (int index = 0; index < args.Length - 1; index++)
            {
                if (argument == args[index])
                    return args[index + 1];
            }

            return null;
        }

        static int IndexOf(string argument, string[] args)
        {
            var arg = GetArgument(argument, args);

            if (arg != null)
                return args.IndexOf(arg);
            else
                return -1;
        }

        static bool IsCommand(string argument)
        {
            return argument == "-list" || argument == "-fetch";
        }

        static void List(string directory)
        {
            Output("Simple Git:  -list " + directory);
            Output("");

            var repositories = GetRepositories(directory);

            foreach (var repository in repositories)
            {
                var output = string.Format("{0}\t{1}\t{2}", repository.Name.ForceLengthLeft(30, false), repository.GitPath.ForceLengthRight(50, true), repository.LastFetch.ToString("yyyy-mm-dd hh:mm:ss tt"));

                Output(output);
            }

            Output("");
            Output("Total Repository Count:  " + repositories.Count());
            Output("");
        }

        static void Main(string[] args)
        {
            var directory = Directory.GetCurrentDirectory();

            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }

            // Directory
            if (!IsCommand(args[args.Length - 1]))
                directory = args[args.Length - 1];

            // List (first argument)
            if (GetArgument("-list", args) != null)
            {
                List(directory);
            }

            // Fetch (first argument)
            else if (GetArgument("-fetch", args) != null)
            {

            }

            else
                PrintHelp();
        }
    }
}
