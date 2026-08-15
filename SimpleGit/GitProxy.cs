using System.Diagnostics;

namespace SimpleGit
{
    public class GitHandlers
    {
        /// <summary>
        /// Provides a callback for GitProxy calls (to the terminal)
        /// </summary>
        /// <returns>Returns true to continue execution of git.exe</returns>
        public delegate bool GitLogHandler(string message);
    }

    /// <summary>
    /// Class that represents a git.exe terminal proxy
    /// </summary>
    public class GitProxy
    {
        private static readonly string GIT = "git.exe";
        private static readonly string GIT_ARGUMENTS = "fetch -v";

        private delegate bool TerminalProcessCallback(Process git, string message, bool isError);

        private readonly GitRepository _repository;

        public GitProxy(GitRepository repository)
        {
            _repository = repository;
        }

        public void Fetch(GitHandlers.GitLogHandler logHandler)
        {
            logHandler("Fetch started for " + _repository.Name);

            var success = Call(_repository.WorkingDirectory, GIT, GIT_ARGUMENTS, (git, message, isError) =>
            {
                return logHandler(message);
            });

            logHandler("Fetch for " + _repository.Name + " completed successfully!");
        }

        /// <summary>
        /// Calls terminal with command line; and outputs to the callback. Return true if exit code was zero.
        /// </summary>
        private bool Call(string workingDirectory, string executable, string arguments, TerminalProcessCallback callback)
        {
            try
            {
                var process = new Process();
                process.StartInfo.FileName = executable;                   // Assume Environment Variables ($Path) contains the process
                process.StartInfo.WorkingDirectory = workingDirectory;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;                 // Assumes shell enviroment (probably the terminal windows is used to, not ours)

                process.EnableRaisingEvents = true;

                // Info
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!callback(process, e.Data, false))
                        process.Close();
                };

                // Error
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!callback(process, e.Data, false))
                        process.Close();
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

    }
}
