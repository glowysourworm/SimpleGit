namespace SimpleGit.Model
{
    public class GitCommit
    {
        public string Sha { get; set; }
        public string Message { get; set; }
        public string Author { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public GitCommit()
        {
            this.Author = string.Empty;
            this.Sha = string.Empty;
            this.Message = string.Empty;
            this.Timestamp = DateTimeOffset.MinValue;
        }
    }
}
