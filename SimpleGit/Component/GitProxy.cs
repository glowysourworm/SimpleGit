using System.Diagnostics;

using SimpleGit.Interface;
using SimpleGit.Model;

namespace SimpleGit.Component
{
    /// <summary>
    /// Class that represents a git.exe terminal proxy
    /// </summary>
    public class GitProxy : IGitProxy
    {
        private static readonly string GIT = "git.exe";
        private static readonly string GIT_FETCH = "fetch -v";
        private static readonly string GIT_CLONE_FORMAT = "clone {0}";

        private delegate bool TerminalProcessCallback(Process git, string message, bool isError);

        public GitProxy()
        {
        }

        public Task<GitRepositoryResponse> Fetch(GitRepositoryRequest request, GitHandlers.GitLogHandler logHandler)
        {
            return Task.Run(async () =>
            {
                logHandler("Fetch started: " + request.Url);

                var success = Call(request.WorkingDirectory, GIT, GIT_FETCH, (git, message, isError) =>
                {
                    return logHandler(message);
                });

                if (success)
                    logHandler("Fetch completed successfully!");
                else
                    logHandler("Fetch completed with errors");

                return await Open(request);
            });
        }

        public Task<GitRepositoryResponse> Clone(GitRepositoryRequest request, GitHandlers.GitLogHandler logHandler)
        {
            return Task.Run(async () =>
            {
                logHandler("Cloning from: " + request.Url);

                var success = Call(request.BaseDirectory, GIT, string.Format(GIT_CLONE_FORMAT, request.Url), (git, message, isError) =>
                {
                    return logHandler(message);
                });

                if (success)
                    logHandler("Clone completed successfully!");
                else
                    logHandler("Clone completed with errors");

                return await Open(request);
            });
        }

        public Task<IEnumerable<GitRepositoryResponse>> OpenMany(GitRepositoryRequest request)
        {
            return Task.Run(async () =>
            {
                var result = new List<GitRepositoryResponse>();

                foreach (var directory in Directory.GetDirectories(request.BaseDirectory))
                {
                    var gitPath = Path.Combine(directory, ".git");

                    // Check for .git folder
                    if (Directory.Exists(gitPath))
                    {
                        var response = await Open(request);

                        result.Add(response);
                    }
                }

                return (IEnumerable<GitRepositoryResponse>)result;
            });
        }

        public Task<GitRepositoryResponse> Open(GitRepositoryRequest request)
        {
            return Task.Run(async () =>
            {
                // Get remote repository first
                var result = new GitRepositoryResponse();
                var remote = await GetRepositoryRemote(request.User, request.Password, request.RepositoryId);
                var gitPath = CreateGitPath(request);

                if (request.RepositoryId != remote.Id)
                    throw new ArgumentException("Repository ID's for request do not match between local and remote");

                using (var gitRepo = new LibGit2Sharp.Repository(gitPath))
                {
                    var local = new GitRepositoryLocal(request.RepositoryId, request.RepositoryName);

                    local.GitPath = gitPath;
                    local.Remotes = gitRepo.Network.Remotes.Select(x => new GitRemote(remote.Id, x.Name, x.Url)).ToList();
                    local.Size = 0;
                    local.WorkingDirectory = gitRepo.Info.WorkingDirectory;
                    local.Branches = gitRepo.Branches.Select(x => new GitBranch()
                    {
                        IsHead = x.IsCurrentRepositoryHead,
                        LastCommit = new GitCommit()
                        {
                            Message = x.Tip.Message,
                            Sha = x.Tip.Sha,
                            Timestamp = x.Tip.Author.When,
                            Author = x.Tip.Author.Name
                        },
                        Name = x.CanonicalName,
                        RemoteName = x.RemoteName
                    }).ToList();

                    // Remote
                    var remoteName = gitRepo.Head.RemoteName;

                    // Local -> Common Ancestor with Remote?
                    //       -> Yes (take commits after common ancestor)
                    //       -> No  (Error)
                    if (!gitRepo.Commits.Any(x => x.Id.Sha == remote.GetHead().LastCommit.Sha))
                        throw new Exception("No common ancestor between local and remote repositories:  " + request.RepositoryName);

                    // Common Ancestor
                    var commonAncestor = gitRepo.Commits.First(x => x.Id.Sha == remote.GetHead().LastCommit.Sha);
                    var commonAncestorSha = commonAncestor.Sha;
                    var commitLocal = gitRepo.Head.Tip;
                    var commitRemote = remote.GetHead().LastCommit;

                    // Commit History (remote)
                    var commitRemoteHistory = await GetRepositoryRemoteHistory(request.User,
                                                                               request.Password,
                                                                               request.RepositoryId,
                                                                               gitRepo.Head.RemoteName,
                                                                               commonAncestor.Sha,
                                                                               commonAncestorSha);
                    // Commit Hisotry (local)
                    var commitLocalHistory = new GitCommitHistory()
                    {
                        BranchName = gitRepo.Head.CanonicalName,
                        ShaOlder = commonAncestorSha,
                        ShaNewer = commitLocal.Sha
                    };

                    // HEAD -> Tip
                    commitLocalHistory.Commits.Add(new GitCommit()
                    {
                        Author = commitLocal.Author.Name,
                        Message = commitLocal.Message,
                        Sha = commitLocal.Sha,
                        Timestamp = commitLocal.Author.When
                    });

                    // Walking Backwards...
                    while (commitLocal.Parents != null &&
                           commitLocal.Parents.Any() &&
                           commitLocal.Sha != commonAncestorSha)
                    {
                        if (commitLocal.Parents.Count() > 1)
                            throw new Exception("Invalid parent commit count");

                        else
                        {
                            // -> Parent (up the tree)
                            commitLocal = commitLocal.Parents.First();

                            commitLocalHistory.Commits.Add(new GitCommit()
                            {
                                Author = commitLocal.Author.Name,
                                Message = commitLocal.Message,
                                Sha = commitLocal.Sha,
                                Timestamp = commitLocal.Author.When
                            });
                        }
                    }

                    // Branch Status
                    var branchStatus = new GitBranchStatus()
                    {
                        CommitDelta = commitLocalHistory.Commits.Count - commitRemoteHistory.Commits.Count,
                        IsAhead = commitLocalHistory.Commits.Count > 0,
                        IsBehind = commitRemoteHistory.Commits.Count > 0
                    };

                    result.Local = local;
                    result.Remote = remote;
                    result.Status = branchStatus;
                }

                return result;
            });
        }

        /// <summary>
        /// Calls terminal with command line; and outputs to the callback. Return true if exit code was zero.
        /// </summary>
        private bool Call(string workingDirectory, string executable, string arguments, TerminalProcessCallback callback)
        {
            try
            {
                var process = new Process();
                process.StartInfo.FileName = executable;                   // Assume Environment Variables ($Path) contains the process
                process.StartInfo.WorkingDirectory = workingDirectory;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;                 // Assumes shell enviroment (probably the terminal windows is used to, not ours)

                process.EnableRaisingEvents = true;

                // Info
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!callback(process, e.Data, false))
                        process.Close();
                };

                // Error
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!callback(process, e.Data, false))
                        process.Close();
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private Task<GitRepositoryRemote> GetRepositoryRemote(string user, string password, long repositoryId)
        {
            using (var githubProxy = new GithubProxy())
            {
                return githubProxy.GetGithubRepository(user, password, repositoryId);
            }
        }

        private Task<GitCommitHistory> GetRepositoryRemoteHistory(string user, string password, long repositoryId, string branchName, string sha1, string sha2)
        {
            using (var githubProxy = new GithubProxy())
            {
                return githubProxy.GetGithubCommitHistory(user, password, repositoryId, branchName, sha1, sha2);
            }
        }

        private string CreateGitPath(GitRepositoryRequest request)
        {
            return Path.Combine(request.BaseDirectory, request.RepositoryName, ".git");
        }

        public void Dispose()
        {

        }
    }
}
