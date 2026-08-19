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

        public Task<GitRepositoryResponse> Process(GitRepositoryRequest request)
        {
            if (!ValidateRequest(request))
                throw new ArgumentException("Invalid IGitProxy request");

            return Task.Run(async () =>
            {
                GitResponseData? single = null;
                IEnumerable<GitResponseData> multiple = null;

                switch (request.Type)
                {
                    case GitRepositoryRequest.RequestType.LocalReadSingle:
                    case GitRepositoryRequest.RequestType.GithubReadSingle:
                    case GitRepositoryRequest.RequestType.Fetch:
                    case GitRepositoryRequest.RequestType.Clone:
                        single = await OpenSingle(request);
                        break;
                    case GitRepositoryRequest.RequestType.LocalReadAll:
                        multiple = await OpenDirectory(request);
                        break;
                    case GitRepositoryRequest.RequestType.GithubReadAll:
                        multiple = await OpenGithub(request);
                        break;
                    case GitRepositoryRequest.RequestType.Initialize:           // Proxy will infer the proper single type
                        multiple = await OpenDirectory(request);
                        break;
                    default:
                        throw new Exception("Unhandled IGitProxy request type");
                }

                return new GitRepositoryResponse()
                {
                    IsMultipleResponse = multiple != null,
                    MultipleResponseData = multiple?.ToList() ?? null,
                    SingleResponseData = single
                };
            });
        }

        private Task<IEnumerable<GitResponseData>> OpenDirectory(GitRepositoryRequest request)
        {
            return Task.Run(async () =>
            {
                var result = new List<GitResponseData>();

                foreach (var directory in Directory.GetDirectories(request.BaseDirectory))
                {
                    var gitPath = CreateGitPath(request.BaseDirectory, directory);

                    // Check for .git folder
                    if (Directory.Exists(gitPath))
                    {
                        // Individual Request(s)
                        GitRepositoryRequest repoRequest = null;

                        // Check local folder
                        using (var gitRepo = new LibGit2Sharp.Repository(gitPath))
                        {
                            var repositoryName = Directory.GetParent(gitPath).Name;

                            repoRequest = new GitRepositoryRequest()
                            {
                                BaseDirectory = request.BaseDirectory,
                                Password = request.Password,
                                Type = request.Type == GitRepositoryRequest.RequestType.Initialize ?
                                                              GitRepositoryRequest.RequestType.Initialize
                                                            : GitRepositoryRequest.RequestType.LocalReadSingle,
                                Url = request.Url,
                                User = request.User,
                                WorkingDirectory = directory,
                                RepositoryName = repositoryName
                            };
                        }

                        var response = await OpenSingle(repoRequest);

                        result.Add(response);
                    }
                }

                return (IEnumerable<GitResponseData>)result;
            });
        }

        private Task<IEnumerable<GitResponseData>> OpenGithub(GitRepositoryRequest request)
        {
            return Task.Run(async () =>
            {
                var result = new List<GitResponseData>();

                using (var proxy = new GithubProxy())
                {
                    var repositories = await proxy.GetAllGithubRepositories(request.User, request.Password);

                    foreach (var repository in repositories)
                    {
                        var response = await OpenSingle(new GitRepositoryRequest()
                        {
                            BaseDirectory = request.BaseDirectory,
                            Password = request.Password,
                            RepositoryName = repository.Name,
                            Type = GitRepositoryRequest.RequestType.GithubReadSingle,
                            Url = request.Url,
                            User = request.User,
                            WorkingDirectory = repository.Name                  // This may need to be changed
                        });

                        result.Add(response);
                    }
                }

                return (IEnumerable<GitResponseData>)result;
            });
        }

        private Task<GitResponseData> OpenSingle(GitRepositoryRequest request)
        {
            if (!ValidateRequest(request))
                throw new ArgumentException("Invalid IGitProxy request");

            return Task.Run(async () =>
            {
                // Get remote repository first
                var result = new GitResponseData();
                var success = false;

                bool localRead = false;
                bool remoteRead = false;

                string? gitPath = null;
                GitRepositoryLocal? local = null;
                GitRepositoryRemote? remote = null;
                GitCommitHistory? localHistory = null;
                GitCommitHistory? remoteHistory = null;

                switch (request.Type)
                {
                    case GitRepositoryRequest.RequestType.LocalReadSingle:
                    {
                        localRead = true;
                    }
                    break;
                    case GitRepositoryRequest.RequestType.GithubReadSingle:
                    {
                        remoteRead = true;
                    }
                    break;
                    case GitRepositoryRequest.RequestType.Fetch:
                    case GitRepositoryRequest.RequestType.Clone:
                    case GitRepositoryRequest.RequestType.Initialize:
                    {
                        localRead = true;
                        remoteRead = true;
                    }
                    break;
                    default:
                        throw new Exception("Unhandled IGitProxy request type");
                }

                // Clone:  Assume local read must be done after clone operation
                //
                if (request.Type == GitRepositoryRequest.RequestType.Clone)
                {
                    success = Call(request.BaseDirectory, GIT, string.Format(GIT_CLONE_FORMAT, request.Url), (git, message, isError) =>
                    {
                        return request.LogHandler(message);
                    });

                    if (success)
                        gitPath = CreateGitPath(request.BaseDirectory, request.WorkingDirectory);
                    else
                        throw new Exception("Error running git.exe clone " + request.Url);
                }

                // Fetch
                //
                if (request.Type == GitRepositoryRequest.RequestType.Fetch)
                {
                    success = Call(request.WorkingDirectory, GIT, GIT_FETCH, (git, message, isError) =>
                    {
                        return request.LogHandler(message);
                    });

                    if (success)
                        gitPath = CreateGitPath(request.BaseDirectory, request.WorkingDirectory);
                    else
                        throw new Exception("Error running git.exe clone " + request.Url);
                }

                // Open:  Local | Remote (github?)
                //
                if (localRead && gitPath == null)
                    gitPath = CreateGitPath(request.BaseDirectory, request.WorkingDirectory);

                if (localRead)
                    local = OpenLocalImpl(gitPath);

                if (remoteRead)
                {
                    remote = await OpenRemoteImpl(request.User, request.Password, request.RepositoryName);
                }

                // History
                if (local != null &&
                    remote != null)
                {
                    await OpenHistoryImpl(request.User, request.Password, local, remote, (localHis, remoteHis) =>
                    {
                        // Callback Setter
                        localHistory = localHis;
                        remoteHistory = remoteHis;
                    });
                }

                return OpenImpl(local, remote, localHistory, remoteHistory);
            });
        }

        #region (private) Impl Methods

        private bool ValidateRequest(GitRepositoryRequest request)
        {
            switch (request.Type)
            {
                case GitRepositoryRequest.RequestType.LocalReadSingle:
                    return !string.IsNullOrWhiteSpace(request.BaseDirectory) &&
                           !string.IsNullOrWhiteSpace(request.WorkingDirectory) &&
                           !string.IsNullOrWhiteSpace(request.RepositoryName) &&
                            Directory.Exists(request.BaseDirectory) &&
                            Directory.Exists(request.WorkingDirectory);

                case GitRepositoryRequest.RequestType.LocalReadAll:
                    return !string.IsNullOrWhiteSpace(request.BaseDirectory) &&
                            Directory.Exists(request.BaseDirectory);

                case GitRepositoryRequest.RequestType.GithubReadSingle:
                    return !string.IsNullOrWhiteSpace(request.BaseDirectory) &&
                           !string.IsNullOrWhiteSpace(request.WorkingDirectory) &&
                           !string.IsNullOrWhiteSpace(request.RepositoryName) &&
                            Directory.Exists(request.BaseDirectory) &&
                            Directory.Exists(request.WorkingDirectory) &&
                           !string.IsNullOrWhiteSpace(request.User) &&
                           !string.IsNullOrWhiteSpace(request.Password) &&
                           !string.IsNullOrWhiteSpace(request.Url);

                case GitRepositoryRequest.RequestType.Initialize:
                    return !string.IsNullOrWhiteSpace(request.BaseDirectory) &&
                            Directory.Exists(request.BaseDirectory) &&
                           !string.IsNullOrWhiteSpace(request.User) &&
                           !string.IsNullOrWhiteSpace(request.Password);

                case GitRepositoryRequest.RequestType.GithubReadAll:
                    return !string.IsNullOrWhiteSpace(request.BaseDirectory) &&
                            Directory.Exists(request.BaseDirectory) &&
                           !string.IsNullOrWhiteSpace(request.User) &&
                           !string.IsNullOrWhiteSpace(request.Password);

                case GitRepositoryRequest.RequestType.Fetch:
                    return !string.IsNullOrWhiteSpace(request.BaseDirectory) &&
                           !string.IsNullOrWhiteSpace(request.WorkingDirectory) &&
                           !string.IsNullOrWhiteSpace(request.RepositoryName) &&
                            Directory.Exists(request.BaseDirectory) &&
                            Directory.Exists(request.WorkingDirectory) &&
                           !string.IsNullOrWhiteSpace(request.User) &&
                           !string.IsNullOrWhiteSpace(request.Password) &&
                           !string.IsNullOrWhiteSpace(request.Url);

                case GitRepositoryRequest.RequestType.Clone:
                    return !string.IsNullOrWhiteSpace(request.BaseDirectory) &&
                           !string.IsNullOrWhiteSpace(request.RepositoryName) &&
                            Directory.Exists(request.BaseDirectory) &&
                           !string.IsNullOrWhiteSpace(request.User) &&
                           !string.IsNullOrWhiteSpace(request.Password) &&
                           !string.IsNullOrWhiteSpace(request.Url);
                default:
                    throw new Exception("Unhandled IGitProxy request type");
            }
        }

        // Open Remote: Url must be verified
        private Task<GitRepositoryRemote> OpenRemoteImpl(string user, string password, string repositoryName)
        {
            // Try Github First
            var result = GetRepositoryRemoteGithub(user, password, repositoryName);

            if (result == null)
                throw new Exception("Initialization of repository failed:  no way to fetch upstream unless there is auto-cloning, or it is on github");

            return result;
        }

        // Open History: Local | Remote, nulls permitted
        private Task OpenHistoryImpl(string user,
                                     string password,
                                     GitRepositoryLocal local,
                                     GitRepositoryRemote remote,
                                     Action<GitCommitHistory, GitCommitHistory> callback)
        {
            return Task.Run(async () =>
            {
                using (var gitRepo = new LibGit2Sharp.Repository(local.GitPath))
                {
                    // Remote
                    var remoteName = gitRepo.Head.RemoteName;

                    // Local -> Common Ancestor with Remote?
                    //       -> Yes (take commits after common ancestor)
                    //       -> No  (Error)
                    if (!gitRepo.Branches.Any(branch => branch.Commits.Any(x => x.Id.Sha == remote.GetHead().LastCommit.Sha)))
                        throw new Exception("No common ancestor between local and remote repositories:  " + local.Name);

                    // Common Ancestor
                    var commonAncestor = gitRepo.Branches
                                                .First(branch => branch.Commits.Any(x => x.Id.Sha == remote.GetHead().LastCommit.Sha))
                                                .Commits
                                                .First(x => x.Sha == remote.GetHead().LastCommit.Sha);

                    var commonAncestorSha = commonAncestor.Sha;
                    var commitLocal = gitRepo.Head.Tip;
                    var commitRemote = remote.GetHead().LastCommit;

                    // Commit History (remote)
                    var remoteHistory = await GetRepositoryRemoteHistoryGithub(user,
                                                                         password,
                                                                         local.Name,
                                                                         remoteName,
                                                                         commonAncestor.Sha,
                                                                         commonAncestorSha);
                    // Commit Hisotry (local)
                    var localHistory = new GitCommitHistory()
                    {
                        BranchName = gitRepo.Head.CanonicalName,
                        ShaOlder = commonAncestorSha,
                        ShaNewer = commitLocal.Sha
                    };

                    // HEAD -> Tip
                    localHistory.Commits.Add(new GitCommit()
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

                            localHistory.Commits.Add(new GitCommit()
                            {
                                Author = commitLocal.Author.Name,
                                Message = commitLocal.Message,
                                Sha = commitLocal.Sha,
                                Timestamp = commitLocal.Author.When
                            });
                        }
                    }

                    // Callback from Task operation to caller
                    callback(localHistory, remoteHistory);
                }
            });
        }

        private Task<GitRepositoryRemote?> GetRepositoryRemoteGithub(string user, string password, string repositoryName)
        {
            using (var githubProxy = new GithubProxy())
            {
                return githubProxy.GetGithubRepository(user, password, repositoryName);
            }
        }

        private Task<GitCommitHistory> GetRepositoryRemoteHistoryGithub(string user, string password, string repositoryName, string branchName, string sha1, string sha2)
        {
            using (var githubProxy = new GithubProxy())
            {
                return githubProxy.GetGithubCommitHistory(user, password, repositoryName, branchName, sha1, sha2);
            }
        }

        private GitResponseData OpenImpl(GitRepositoryLocal? local,
                                         GitRepositoryRemote? remote,
                                         GitCommitHistory? localHistory,
                                         GitCommitHistory? remoteHistory)
        {
            // Branch Status
            GitBranchStatus? branchStatus = null;

            if (localHistory != null &&
                remoteHistory != null)
            {
                // Branch Status
                branchStatus = new GitBranchStatus()
                {
                    CommitDelta = localHistory.Commits.Count - remoteHistory.Commits.Count,
                    IsAhead = localHistory.Commits.Count > 0,
                    IsBehind = remoteHistory.Commits.Count > 0
                };
            }

            return new GitResponseData()
            {
                Local = local,
                Remote = remote,
                Status = branchStatus
            };
        }

        // Open Local: Git path must be verified
        private GitRepositoryLocal OpenLocalImpl(string gitPath)
        {
            if (string.IsNullOrWhiteSpace(gitPath))
                throw new ArgumentException("Invalid Git path (local)");

            if (!Directory.Exists(gitPath))
                throw new ArgumentException("Invalid Git path (local)");

            using (var gitRepo = new LibGit2Sharp.Repository(gitPath))
            {
                var repositoryName = Directory.GetParent(gitPath).Name;

                var local = new GitRepositoryLocal(repositoryName);

                local.GitPath = gitPath;
                local.Remotes = gitRepo.Network.Remotes.Select(x => new GitRemote(x.Name, x.Url)).ToList();
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

                return local;
            }
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

        private string CreateGitPath(string baseDirectory, string workingDirectory)
        {
            return Path.Combine(baseDirectory, workingDirectory, ".git");
        }

        #endregion

        public void Dispose()
        {

        }
    }
}
