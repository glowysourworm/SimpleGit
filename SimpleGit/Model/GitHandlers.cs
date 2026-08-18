namespace SimpleGit.Model
{
    public class GitHandlers
    {
        /// <summary>
        /// Provides a callback for GitProxy calls (to the terminal)
        /// </summary>
        /// <returns>Returns true to continue execution of git.exe</returns>
        public delegate bool GitLogHandler(string message);
    }
}
