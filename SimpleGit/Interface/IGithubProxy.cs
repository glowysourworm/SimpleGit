using SimpleGit.Model;

namespace SimpleGit.Interface
{
    /// <summary>
    /// Github metadata service proxy
    /// </summary>
    public interface IGithubProxy : IDisposable
    {
        /// <summary>
        /// Gets the commit history (delta) between two commits. The first commit SHA should be older than the second SHA.
        /// </summary>
        Task<GitCommitHistory> GetGithubCommitHistory(string user, string password, string repositoryName, string branchName, string sha1, string sha2);

        /// <summary>
        /// Returns github repository metadata for the specified repository
        /// </summary>
        /// <param name="user">Your Github account username</param>
        /// <param name="password">Your Github account password</param>
        /// <param name="repositoryId">Repository Id</param>
        Task<GitRepositoryRemote?> GetGithubRepository(string user, string password, string repositoryName);

        /// <summary>
        /// Returns all repositories' metadata for the specified user
        /// </summary>
        /// <param name="user">Your Github account username</param>
        /// <param name="password">Your Github account password</param>
        Task<IEnumerable<GitRepositoryRemote>> GetAllGithubRepositories(string user, string password);
    }
}
