using SimpleGit.Model;

namespace SimpleGit.Interface
{
    /// <summary>
    /// Local command line git.exe proxy. This should be used for referencing local repositories (using Libgit2Sharp);
    /// and making fetch requests from remotes using clone and fetch.
    /// </summary>
    public interface IGitProxy : IDisposable
    {
        /// <summary>
        /// Recurses one directory level (from the base directory) into individual project directories looking for
        /// .git repository folders. For each found, the proxy will return a GitRepository instance. 
        /// </summary>
        /// <param name="baseDirectory">The base directory to start recursion.</param>
        /// <returns>A collection of GitRepository instances, one per .git folder found</returns>
        Task<IEnumerable<GitRepositoryResponse>> OpenMany(GitRepositoryRequest request);

        /// <summary>
        /// Opens a repository from a local disk drive (or network disk drive; but on a file system)
        /// </summary>
        Task<GitRepositoryResponse> Open(GitRepositoryRequest request);

        /// <summary>
        /// Fetches a git repository's files from its primary remote
        /// </summary>
        /// <param name="logHandler">Log handler for your program to receive log messages during the fetch</param>
        /// <returns>An updated set of metadata for the repository</returns>
        Task<GitRepositoryResponse> Fetch(GitRepositoryRequest request, GitHandlers.GitLogHandler logHandler);

        /// <summary>
        /// Clones a git repository from a remote url. Returns the repository's metadata.
        /// </summary>
        Task<GitRepositoryResponse> Clone(GitRepositoryRequest request, GitHandlers.GitLogHandler logHandler);
    }
}
