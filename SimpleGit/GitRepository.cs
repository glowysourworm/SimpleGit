using LibGit2Sharp;

namespace SimpleGit
{
    public class GitRemote
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string PushUrl { get; set; }

        public GitRemote()
        {
            this.Name = string.Empty;
            this.Url = string.Empty;
            this.PushUrl = string.Empty;
        }
    }

    /// <summary>
    /// Git Repository: All information is about the current HEAD and its commit history. The GitProxy will fetch all 
    /// branches in the repository using:  {git fetch -v}
    /// </summary>
    public class GitRepository
    {
        public string Name { get; set; }
        public string GitPath { get; set; }
        public string WorkingDirectory { get; set; }
        public DateTimeOffset LastCommitLocal { get; set; }
        public DateTimeOffset LastCommitRemote { get; set; }
        public bool IsAhead { get; set; }
        public bool IsBehind { get; set; }
        public string HeadRemoteName { get; set; }

        /// <summary>
        /// Number of commits ahead or behind HEAD remote (for primary branch)
        /// </summary>
        public int CommitDelta { get; set; }

        /// <summary>
        /// Size (in bytes) of the repository folder (GitPath)
        /// </summary>
        public uint Size { get; set; }

        /// <summary>
        /// List of all remotes for the repository
        /// </summary>
        public List<GitRemote> Remotes { get; set; }

        /// <summary>
        /// Returns GitRemote for the HEAD
        /// </summary>
        public GitRemote GetHeadRemote()
        {
            return this.Remotes.First(x => x.Name == this.HeadRemoteName);
        }

        /// <summary>
        /// The HEAD remote origin will have an "upstream" repo that is 
        /// </summary>
        public bool IsFork()
        {
            var headRemote = this.GetHeadRemote();

            // Going to go with this until I see git's documentation. github CLI would be
            // a faster route to get some of this correctly.
            //
            if (headRemote.Name != "origin")
                return false;

            return this.Remotes.Any(x => x.Name != this.HeadRemoteName &&
                                         x.Url != headRemote.Url &&
                                         x.Name == "upstream");
        }

        public GitRepository()
        {
            this.Name = string.Empty;
            this.GitPath = string.Empty;
            this.WorkingDirectory = string.Empty;
            this.LastCommitLocal = DateTimeOffset.MinValue;
            this.LastCommitRemote = DateTimeOffset.MinValue;
            this.HeadRemoteName = string.Empty;
            this.CommitDelta = 0;
            this.IsAhead = false;
            this.IsBehind = false;
            this.Size = 0;
            this.Remotes = new List<GitRemote>();
        }

        public static GitRepository Load(string gitPath)
        {
            var result = new GitRepository();

            using (var gitRepo = new Repository(gitPath))
            {
                result.Name = Directory.GetParent(gitPath).Name;
                result.GitPath = gitPath;
                result.LastCommitLocal = gitRepo.Head.Tip.Author.When;
                result.Remotes = gitRepo.Network.Remotes.Select(x => new GitRemote()
                {
                    Name = x.Name,
                    Url = x.Url

                }).ToList();

                // Remote (HEAD)
                var remote = gitRepo.Network.Remotes.First(x => x.Name == gitRepo.Head.RemoteName);

                var aheadBy = gitRepo.Head.TrackingDetails.AheadBy ?? 0;
                var behindBy = gitRepo.Head.TrackingDetails.BehindBy ?? 0;

                result.HeadRemoteName = remote.Name;
                result.LastCommitRemote = gitRepo.Head.TrackingDetails.CommonAncestor.Author.When;
                result.CommitDelta = Math.Max(aheadBy, behindBy);
                result.IsBehind = behindBy > aheadBy;
                result.IsAhead = aheadBy > behindBy;

                // Directory details (local)
                result.WorkingDirectory = Directory.GetParent(gitPath).FullName;

                // TODO
                result.Size = 0;

            }

            return result;
        }
    }
}
