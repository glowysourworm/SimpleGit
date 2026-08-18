using Octokit;

using SimpleGit.Interface;
using SimpleGit.Model;

namespace SimpleGit.Component
{
    public class GithubProxy : IGithubProxy
    {
        // See github application auth section
        private static string ClientID = "Iv23lip0XnqaZeqfW5di";
        private static string ClientSecret = "";
        private static string ApplicationName = "SimpleGitManager";     // This is registered at Github (just for my account at the moment)

        public Task<IEnumerable<GitRepositoryRemote>> GetAllGithubRepositories(string user, string password)
        {
            return Task.Run(async () =>
            {
                var client = CreateClient();

                var repositories = await client.Repository.GetAllForUser(user);
                var result = new List<GitRepositoryRemote>();

                foreach (var repository in repositories)
                {
                    result.Add(await Map(repository, client));
                }

                return (IEnumerable<GitRepositoryRemote>)result;
            });
        }

        public Task<GitRepositoryRemote?> GetGithubRepository(string user, string password, long repositoryId)
        {
            return Task.Run(async () =>
            {
                var client = CreateClient();

                var repository = await client.Repository.Get(repositoryId);

                return (await Map(repository, client));
            });
        }

        public Task<GitRepositoryRemote?> GetGithubRepository(string user, string password, string repositoryName)
        {
            return Task.Run(async () =>
            {
                var client = CreateClient();

                var repositories = await client.Repository.GetAllForUser(user);

                foreach (var repository in repositories)
                {
                    if (repository.Name == repositoryName)
                        return (await Map(repository, client));
                }

                return null;
            });
        }

        public Task<GitCommitHistory> GetGithubCommitHistory(string user, string password, long repositoryId, string branchName, string sha1, string sha2)
        {
            return Task.Run(async () =>
            {
                var client = CreateClient();

                // Commits From...
                var commits = await client.Repository.Commit.GetAll(repositoryId, new CommitRequest()
                {
                    Sha = sha1    // ONLY FOR THE COMMIT'S BRANCH! (via the SHA)
                });

                // Commits To... (for requested branch)
                var commit1 = commits.First(x => x.Sha == sha1);

                // Commits After...
                var commitDelta = commits.Where(x => x.Commit.Author.Date > commit1.Commit.Author.Date);

                return new GitCommitHistory()
                {
                    BranchName = branchName,
                    Commits = commitDelta.Select(x => new GitCommit()
                    {
                        Author = x.Author.Login,
                        Message = x.Commit.Message,
                        Sha = x.Sha,
                        Timestamp = x.Commit.Author.Date

                    }).ToList(),
                    ShaOlder = sha1,
                    ShaNewer = sha2
                };
            });
        }

        private Task<GitRepositoryRemote> Map(Octokit.Repository repository, GitHubClient client)
        {
            return Task.Run(async () =>
            {
                // Query for ALL branches
                var branches = await client.Repository.Branch.GetAll(repository.Id);

                // Query for HEAD branch
                var head = await client.Repository.Branch.Get(repository.Id, repository.DefaultBranch);

                // Query for last commit (tip)
                var lastCommit = await client.Repository.Commit.Get(repository.Id, head.Commit.Ref);

                return new GitRepositoryRemote(repository.Id, repository.Name)
                {
                    Branches = branches.Select(x => new GitBranch()
                    {
                        IsHead = x.Name == repository.DefaultBranch,
                        LastCommit = new GitCommit()
                        {
                            Author = x.Commit.User.Name,
                            Message = lastCommit.Commit.Message,
                            Sha = x.Commit.Sha,
                            Timestamp = repository.PushedAt ?? DateTime.MinValue             // THIS SHOULD BE PER BRANCH (???)
                        },
                        Name = x.Name
                    }).ToList(),
                    IsFork = repository.Fork,
                    OwnerName = repository.Owner.Name,
                    Parents = new List<GitRemote>() { new GitRemote(repository.Parent.Id, repository.Parent.Name, repository.Parent.Url) },
                    Size = repository.Size,
                    Url = repository.Url
                };
            });
        }

        // Setup github client with proper application credentials (more TODO)
        private GitHubClient CreateClient()
        {
            return new GitHubClient(new ProductHeaderValue(ApplicationName));
        }

        public void Dispose()
        {

        }
    }
}
