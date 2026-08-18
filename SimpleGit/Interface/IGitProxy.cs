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
        /// Processes request:  1) Local (single|multiple)(read), 2) Remote (single|multiple)(read), 3) Fetch, 4) Clone
        /// </summary>
        /// <returns>A collection of data to represent the local / remote repositories</returns>
        Task<GitRepositoryResponse> Process(GitRepositoryRequest request);
    }
}
