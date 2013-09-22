using System.Collections.Generic;
using CoreCI.Models;

namespace CoreCI.Server
{
    public static class EntityExtensions
    {
        public static UserEntity CloneWithoutSecrets(this UserEntity user)
        {
            if (user != null)
            {
                return new UserEntity()
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = null,
                    PasswordHash = null,
                    PasswordSalt = null,
                    PasswordHashAlgorithm = null
                };
            }

            return null;
        }

        public static ProjectEntity CloneWithoutSecrets(this ProjectEntity project)
        {
            if (project != null)
            {
                return new ProjectEntity()
                {
                    Id = project.Id,
                    UserId = project.UserId,
                    ConnectorId = project.ConnectorId,
                    Name = project.Name,
                    FullName = project.FullName,
                    Token = null,
                    IsPrivate = project.IsPrivate,
                    Options = new Dictionary<string, string>()
                };
            }

            return null;
        }

        public static TaskEntity CloneWithoutSecrets(this TaskEntity task)
        {
            if (task != null)
            {
                return new TaskEntity()
                {
                    Id = task.Id,
                    ProjectId = task.ProjectId,
                    Branch = task.Branch,
                    State = task.State,
                    WorkerId = task.WorkerId,
                    Configuration = null,
                    ExitCode = task.ExitCode,
                    CreatedAt = task.CreatedAt,
                    DispatchedAt = task.DispatchedAt,
                    StartedAt = task.StartedAt,
                    FinishedAt = task.FinishedAt,
                    Commit = task.Commit,
                    CommitUrl = task.CommitUrl,
                    CommitMessage = task.CommitMessage
                };
            }

            return null;
        }

        public static ConnectorEntity CloneWithoutSecrets(this ConnectorEntity connector)
        {
            if (connector != null)
            {
                return new ConnectorEntity()
                {
                    Id = connector.Id,
                    UserId = connector.UserId,
                    Provider = connector.Provider,
                    ProviderUserIdentifier = null,
                    Options = new Dictionary<string, string>(),
                    CreatedAt = connector.CreatedAt,
                    ModifiedAt = connector.ModifiedAt
                };
            }

            return null;
        }
    }
}
