namespace SimpleGit.Model
{
    /// <summary>
    /// Git repository request will be for clients that support basic authentication (Github, etc...); and
    /// will allow basic details to be passed to several git services, when needed.
    /// </summary>
    public class GitRepositoryRequest
    {
        public enum RequestType
        {
            /// <summary>
            /// The service will open local repositories to get metadata. This will be read-only.
            /// </summary>
            LocalReadSingle = 0,

            /// <summary>
            /// The service will open local repositories to get metadata. This will be read-only.
            /// </summary>
            LocalReadAll = 1,

            /// <summary>
            /// The service will load repositoires from your github account
            /// </summary>
            GithubReadSingle = 2,

            /// <summary>
            /// The service will load repositoires from your github account
            /// </summary>
            GithubReadAll = 3,

            /// <summary>
            /// Perform a fetch from the HEAD remote
            /// </summary>
            Fetch = 4,

            /// <summary>
            /// Clone a git repository locally, into the base directory (working directory will be created on clone)
            /// </summary>
            Clone = 5
        }

        /// <summary>
        /// Type of git request
        /// </summary>
        public RequestType Type { get; set; }

        /// <summary>
        /// User (basic authentication)
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Password (basic authentication)
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Url of a fetch or clone request
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Name of repository (needed even for clone requests). This can be found from calling other
        /// git service prior to cloning.
        /// </summary>
        public string RepositoryName { get; set; }

        /// <summary>
        /// Working directory for the git repository.
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Directory just above working directory. This will be where clones are put after completing the git request.
        /// </summary>
        public string BaseDirectory { get; set; }

        /// <summary>
        /// Handler for logging during repository request operations
        /// </summary>
        public GitHandlers.GitLogHandler LogHandler { get; set; }
    }
}
