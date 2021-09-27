using PDS_Server.Elements.Communications;
using PDS_Server.Elements.Revit;
using System;
using System.Linq;

namespace PDS_Server.Elements
{
    public static class Extentions
    {
        public static bool IsExpired(this DbAccount user)
        {
            if (user.Access.Ticks > DateTime.UtcNow.Ticks)
            {
                return false;
            }
            return true;
        }

        public static dynamic ToResponse(this DbAccount user)
        {
            return new
            {
                Id = user.Id.ToString(),
                FirstName = user.FirstName,
                LastName = user.SecondName,
                Team = user.Team.ToString(),
                ImageData = user.ImageData,
                Role = user.Role
            };
        }
        public static dynamic ToResponse(this DbApplication application)
        {
            return new
            {
                Link = application.Link,
                Version = application.Version,
                Changelog = application.Changelog
            };
        }
        public static dynamic ToResponse(this DbDocument application)
        {
            return new
            {
                Id = application.Id.ToString(),
                Department = application.Department == null ? null : application.Department.ToString(),
                FoundByUser = application.FoundByUser == null ? null : application.FoundByUser.ToString(),
                FullPath = application.FullPath,
                IsCloudModel = application.IsCloudModel,
                Name = application.Name,
                NamSyncCounte = application.SyncCount,
                ServerGuid = application.ServerGuid,
                Project = application.Project == null ? null : application.Project.ToString(),
            };
        }
        public static dynamic ToResponse(this DbClashResult clashResult)
        {
            return new
            {
                Id = clashResult.Id.ToString(),
                Name = clashResult.Name,
                ItemsDone = clashResult.ItemsDone,
                ItemsCount = clashResult.ItemsCount,
                LastChange = clashResult.LastChange,
                CreationTime = clashResult.CreationTime,
                Description = clashResult.Description,
                Group = clashResult.Group.ToString(),
                Clashes = clashResult.Clashes
            };
        }
        public static dynamic ToResponse(this DbGroupOfClashes clashResult)
        {
            return new
            {
                Id = clashResult.Id.ToString(),
                Project = clashResult.Project,
                Name = clashResult.Name,
                Description = clashResult.Description,
                Progress = clashResult.Progress,
                Items = clashResult.Items.Select(x => x.ToString()),
                IsClosed = clashResult.IsClosed
            };
        }
        public static dynamic ToResponse(this DbChat chat)
        {
            return new
            {
                Id = chat.Id.ToString(),
                LastChange = chat.LastChange,
                Messages = chat.Messages != null ? chat.Messages.Select(x => x.ToResponse()) : null
            };
        }
        public static dynamic ToResponse(this DbMessage clashResult)
        {
            return new
            {
                To = clashResult.To.ToString(),
                From = clashResult.From.ToString(),
                Message = clashResult.Message,
                Time = clashResult.Time
            };
        }
    }
}
