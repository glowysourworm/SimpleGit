namespace SimpleGit.Model
{
    /// <summary>
    /// Git Repository: All information is about the current HEAD and its commit history. The GitProxy will fetch all 
    /// branches in the repository using:  {git fetch -v}
    /// </summary>
    public abstract class GitRepository
    {
        /// <summary>
        /// Gets name of the repository
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Collection of branches in the repository
        /// </summary>
        public List<GitBranch> Branches { get; set; }

        /// <summary>
        /// Size (in bytes) of the repository 
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Returns the head branch for the repository
        /// </summary>
        public GitBranch GetHead()
        {
            return this.Branches.First(x => x.IsHead);
        }

        public GitRepository(string name)
        {
            this.Name = name;
            this.Branches = new List<GitBranch>();
            this.Size = 0;
        }
    }
}
