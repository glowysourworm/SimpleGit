namespace SimpleGit.Model
{
    public class GitRemote
    {
        /// <summary>
        /// Id of remote repository
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Name of the remote
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Url (for git fetch or clone)
        /// </summary>
        public string Url { get; set; }

        public GitRemote(long id, string name, string url)
        {
            this.Id = id;
            this.Name = name;
            this.Url = url;
        }
    }
}
