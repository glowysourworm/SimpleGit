using LibGit2Sharp;

namespace SimpleGit
{
    public class GitRemote
    {
        public string Name { get; set; }
        public string Url { get; set; }

        public GitRemote()
        {
            this.Name = string.Empty;
            this.Url = string.Empty;
        }
    }

    public class GitRepository
    {
        public string Name { get; set; }
        public string GitPath { get; set; }
        public string WorkingDirectory { get; set; }
        public DateTimeOffset LastCommit { get; set; }
        public DateTimeOffset LastFetch { get; set; }
        public bool IsFork { get; set; }

        /// <summary>
        /// Size (in bytes) of the repository folder (GitPath)
        /// </summary>
        public uint Size { get; set; }

        public List<GitRemote> Remotes { get; set; }

        public GitRepository()
        {
            this.Name = string.Empty;
            this.GitPath = string.Empty;
            this.WorkingDirectory = string.Empty;
            this.LastCommit = DateTimeOffset.MinValue;
            this.LastFetch = DateTimeOffset.MinValue;
            this.IsFork = false;
            this.Size = 0;
            this.Remotes = new List<GitRemote>();
        }

        public static GitRepository Load(string gitPath)
        {
            var result = new GitRepository();

            using (var gitRepo = new Repository(gitPath))
            {
                result.GitPath = gitPath;
                result.IsFork = false;
                result.LastCommit = gitRepo.Head.Tip.Author.When;
                result.LastFetch = Directory.GetParent(gitPath).LastWriteTime;
                result.Name = Directory.GetParent(gitPath).Name;
                result.Remotes = gitRepo.Network.Remotes.Select(x => new GitRemote()
                {
                    Name = x.Name,
                    Url = x.Url

                }).ToList();

                result.Size = 0;
                result.WorkingDirectory = Directory.GetParent(gitPath).FullName;
            }

            return result;
        }
    }
}
