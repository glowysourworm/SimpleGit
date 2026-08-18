namespace SimpleGit.Model
{
    public class GitBranchStatus
    {
        /// <summary>
        /// Number of commits ahead or behind this remote is from a reference branch.
        /// </summary>
        public int CommitDelta { get; set; }

        /// <summary>
        /// Flag signaling that this remote is ahead by the amount of commit deltas
        /// </summary>
        public bool IsAhead { get; set; }

        /// <summary>
        /// Flag signaling that this remote is behind by the amount of commit deltas
        /// </summary>
        public bool IsBehind { get; set; }

        public GitBranchStatus()
        {
            this.CommitDelta = 0;
            this.IsBehind = false;
            this.IsAhead = false;
        }
    }
}
