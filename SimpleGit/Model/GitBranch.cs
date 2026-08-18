namespace SimpleGit.Model
{
    public class GitBranch
    {
        /// <summary>
        /// Name of the branch
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Name of the remote for this branch
        /// </summary>
        public string RemoteName { get; set; }

        /// <summary>
        /// Indicates that this is the head branch
        /// </summary>
        public bool IsHead { get; set; }

        /// <summary>
        /// Last commit to this branch (also known as the "tip")
        /// </summary>
        public GitCommit LastCommit { get; set; }

        public GitBranch()
        {
            this.Name = string.Empty;
            this.RemoteName = string.Empty;
            this.IsHead = false;
            this.LastCommit = new GitCommit();
        }
    }
}
