using GlobalCMSUmbraco.ProjectsSection.Models.MongoDb.GlobalCmsDb;
using GlobalCMSUmbraco.ProjectsSection.Models.MongoDb.ProjectDb;
using System.Collections.Generic;
using Umbraco.Core.Models.Membership;

namespace GlobalCMSUmbraco.ProjectsSection.ServiceInterfaces
{
    public interface IProjectsMongoDbService
    {
        /// <summary>
        /// Add a new project to MongoDb
        /// </summary>
        /// <param name="project">Project to add</param>
        /// <param name="failReason">If add failed, return the reason why</param>
        /// <returns>True of the project was added</returns>
        bool AddProject(ProjectModel project, out string failReason);

        /// <summary>
        /// Get the list of projects (ACL ignored)
        /// </summary>
        /// <returns>List of MongoDb projects</returns>
        List<ProjectModel> GetProjectsList();

        /// <summary>
        /// Get the list of projects from that this user has access to (determined by content root node)
        /// </summary>
        /// <param name="currentUser">Current backoffice user</param>
        /// <returns>List of MongoDb projects</returns>
        List<ProjectModel> GetProjectsList(IUser currentUser);

        /// <summary>
        /// Retrieve a project by its id, returns null if no match found
        /// </summary>
        ProjectModel GetProjectById(string projectId);

        /// <summary>
        /// Retrieve a project by its code, returns null if no match found
        /// </summary>
        ProjectModel GetProjectByCode(string projectCode);

        /// <summary>
        /// Retrieve a project by its content start node id, returns null if no match found
        /// </summary>
        ProjectModel GetProjectByContentNodeId(int contentNodeId);

        /// <summary>
        /// Retrieve a project by its media start node id, returns null if no match found
        /// </summary>
        ProjectModel GetProjectByMediaNodeId(int mediaNodeId);

        /// <summary>
        /// Update an existing project on MongoDb, returning true if 1 record updated, otherwise returns false
        /// </summary>
        bool SaveProject(ProjectModel project);

        /// <summary>
        /// Delete an existing project from the global CMS projects list, returning true if 1 record deleted
        /// </summary>
        bool DeleteProjectFromProjectsList(string id);

        /// <summary>
        /// Get the first parameters document in the collection for this database
        /// </summary>
        ParametersModel GetParametersForProject(string dbName);

        /// <summary>
        /// Save the parameters document for this database
        /// </summary>
        void SaveParametersForProject(string dbName, ParametersModel parameters);

        /// <summary>
        /// Create a new collection on MongoDb if it doesn't already exist in this database
        /// </summary>
        void CreateMongoDbCollectionIfNotExists(string dbName, string collectionName);

        /// <summary>
        /// Delete project databases by specifying their publish and preview names
        /// </summary>
        void DeleteProjectMongoDbDatabases(string publishDbName, string previewDbName);
    }
}
