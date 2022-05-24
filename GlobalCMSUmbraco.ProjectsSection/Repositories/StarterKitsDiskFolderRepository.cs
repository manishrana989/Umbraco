using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GlobalCMSUmbraco.ProjectsSection.Services;
using Umbraco.Core.Logging;

namespace GlobalCMSUmbraco.ProjectsSection.Repositories
{
    public interface IStarterKitsRepository
    {
        Dictionary<string, string> GetStarterKits(string folderRoot, out string errorMessage);
        string GetStarterKitFriendlyName(string starterKitName);
    }

    public class StarterKitsDiskFolderRepository : IStarterKitsRepository
    {
        private readonly ILogger logger;

        public StarterKitsDiskFolderRepository(ILogger logger)
        {
            this.logger = logger;
        }

        public Dictionary<string, string> GetStarterKits(string folderRoot, out string errorMessage)
        {
            var folderPath = Path.Combine(folderRoot, ProjectConstants.AppDataStarterKitsFolder);
            try
            {
                var directoryInfo = new DirectoryInfo(folderPath);
                var configFiles = directoryInfo.GetFiles("*.usync");

                var files = configFiles.ToDictionary(key => key.Name, val => GetStarterKitFriendlyName(val.Name));
                if (files.Any())
                {
                    errorMessage = string.Empty;
                    return files;
                }
            }
            catch (Exception ex)
            {
                logger.Error<ProjectsSectionService>(ex, "Failed to find .usync files in {folder}", folderPath);
            }

            errorMessage = "Failed to find any starter kits";
            return null;
        }

        public string GetStarterKitFriendlyName(string starterKitName)
        {
            return starterKitName?
                .Replace(".usync", "")
                .Replace("-", " ")
                .Replace("_", " ");
        }
    }
}