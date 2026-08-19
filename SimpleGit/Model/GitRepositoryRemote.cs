namespace SimpleGit.Model
{
    /// <summary>
    /// Represents a remote for a git repository. The remotes for THIS repository are labled
    /// as Parents. This property will have a list of parent GitRemote instances.
    /// </summary>
    public class GitRepositoryRemote : GitRepository
    {
        public GitRepositoryRemote(string name) : base(name)
        {
            this.Url = string.Empty;
        }

        /// <summary>
        /// Url for reading the repository data
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Name of remote repository
        /// </summary>
        public string OwnerName { get; set; }

        /// <summary>
        /// Is remote repository fork of another repository?
        /// </summary>
        public bool IsFork { get; set; }
    }
}
