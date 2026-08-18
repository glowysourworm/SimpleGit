namespace SimpleGit.Model
{
    public class GitRemote
    {
        /// <summary>
        /// Name of the remote
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Url (for git fetch or clone)
        /// </summary>
        public string Url { get; set; }

        public GitRemote(string name, string url)
        {
            this.Name = name;
            this.Url = url;
        }
    }
}
