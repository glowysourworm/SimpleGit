namespace SimpleGit.Model
{
    /// <summary>
    /// Represents a git commit history (delta) between and including two commits.
    /// </summary>
    public class GitCommitHistory
    {
        /// <summary>
        /// Branch name for the commit history
        /// </summary>
        public string BranchName { get; set; }

        /// <summary>
        /// Sha ID for the older of two commits
        /// </summary>
        public string ShaOlder { get; set; }

        /// <summary>
        /// Sha ID for the newer of two commits
        /// </summary>
        public string ShaNewer { get; set; }

        /// <summary>
        /// Commits between (and including ShaOlder and ShaNewer)
        /// </summary>
        public List<GitCommit> Commits { get; set; }

        public GitCommitHistory()
        {
            this.BranchName = string.Empty;
            this.ShaOlder = string.Empty;
            this.ShaNewer = string.Empty;
            this.Commits = new List<GitCommit>();
        }

    }
}
