namespace SimpleGit.Model
{
    public class GitRepositoryLocal : GitRepository
    {
        public GitRepositoryLocal(long id, string name) : base(id, name)
        {
            this.GitPath = string.Empty;
            this.WorkingDirectory = string.Empty;
            this.Remotes = new List<GitRemote>();
        }

        /// <summary>
        /// Path (full path) of the .git folder for the repository.
        /// </summary>
        public string GitPath { get; set; }

        /// <summary>
        /// Path of the .git folder's working directory (this usually follows the project/repository name)
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// List of all remotes for the repository
        /// </summary>
        public List<GitRemote> Remotes { get; set; }

        /// <summary>
        /// Returns GitRemote for the HEAD
        /// </summary>
        public GitRemote GetOrigin()
        {
            return this.Remotes.First(x => x.Name == "origin");
        }

        /// <summary>
        /// The remotes will have an "upstream" repo that is the next from "origin"
        /// </summary>
        public bool IsFork()
        {
            return this.Remotes.Any(x => x.Name == "upstream");
        }
    }
}
