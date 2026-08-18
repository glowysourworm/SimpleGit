namespace SimpleGit.Model
{
    /// <summary>
    /// Repository response that represents a typical situation with a local repository cloned
    /// from a remote repository. There may be no local repository (remote only), or no remote
    /// repository (local only). The rest of the information must be inferred from the repository
    /// metadata.
    /// </summary>
    public class GitResponseData
    {
        /// <summary>
        /// Available only if the repository has been cloned locally; or only exists locally.
        /// </summary>
        public GitRepositoryLocal? Local { get; set; }

        /// <summary>
        /// Primary remote repository; or null if the repository is local-only
        /// </summary>
        public GitRepositoryRemote? Remote { get; set; }

        /// <summary>
        /// Branch status between two repositories - this is to compare the HEAD branch; and will
        /// let you know if the repository is up to date. The status references local v.s. remote.
        /// </summary>
        public GitBranchStatus? Status { get; set; }
    }


    public class GitRepositoryResponse
    {
        /// <summary>
        /// Indicates that this is a multiple repository response
        /// </summary>
        public bool IsMultipleResponse { get; set; }

        /// <summary>
        /// Response data for multiple repositories
        /// </summary>
        public List<GitResponseData> MultipleResponseData { get; set; }

        /// <summary>
        /// Response data for single repository request
        /// </summary>
        public GitResponseData? SingleResponseData { get; set; }

        public GitRepositoryResponse()
        {
            this.IsMultipleResponse = false;
            this.MultipleResponseData = new List<GitResponseData>();
            this.SingleResponseData = null;
        }
    }
}
