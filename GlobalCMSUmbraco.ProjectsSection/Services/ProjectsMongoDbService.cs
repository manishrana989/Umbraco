using GlobalCMSUmbraco.ProjectsSection.Models.MongoDb.GlobalCmsDb;
using GlobalCMSUmbraco.ProjectsSection.Models.MongoDb.ProjectDb;
using GlobalCMSUmbraco.ProjectsSection.ServiceInterfaces;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Services;

namespace GlobalCMSUmbraco.ProjectsSection.Services
{
    public class ProjectsMongoDbService : IProjectsMongoDbService
    {
        private readonly IEntityService entityService;
        private readonly AppCaches appCaches;
        private readonly ILogger logger;

        public ProjectsMongoDbService(IEntityService entityService, AppCaches appCaches, ILogger logger)
        {
            this.entityService = entityService;
            this.appCaches = appCaches;
            this.logger = logger;
        }

        public bool AddProject(ProjectModel project, out string failReason)
        {
            failReason = null;

            if (!GetGlobalCmsProjectsCollection(out var projectsCollection))
            {
                failReason = "Failed to connect to Global CMS projects collection";
                return false;
            }

            if (projectsCollection.Find(x => x.Code == project.Code).FirstOrDefault() != null)
            {
                failReason = $"Project code '{project.Code}' is already in use";
                return false;
            }

            if (projectsCollection.Find(x => x.MongoDbPreview == project.MongoDbPublish).FirstOrDefault() != null)
            {
                failReason = $"MongoDb publish name '{project.MongoDbPublish}' is already in use";
                return false;
            }

            if (projectsCollection.Find(x => x.MongoDbPreview == project.MongoDbPreview).FirstOrDefault() != null)
            {
                failReason = $"MongoDb preview name '{project.MongoDbPreview}' is already in use";
                return false;
            }

            if (string.IsNullOrEmpty(project.Id))
            {
                // before adding to MongoDb generate a guid to use as an Id
                project.Id = Guid.NewGuid().ToString();
            }


            projectsCollection.InsertOne(project);
            logger.Info<ProjectsMongoDbService>("MongoDb added {ProjectCode} to GlobalCms Projects collection",
                project.Code);

            var _ = InitMongoDb(project);

            return true;
        }

        private bool InitMongoDb(ProjectModel project)
        {
            try
            {
                // create the DBs for Publish & Preview
                foreach (var collection in new[] { Common.Constants.MongoDb.CollectionNames.Content, Common.Constants.MongoDb.CollectionNames.Media, Common.Constants.MongoDb.CollectionNames.Dictionary })
                {
                    foreach (var database in new[] { project.MongoDbPreview, project.MongoDbPublish })
                    {
                        CreateMongoDbCollectionIfNotExists(database, collection);
                    }
                }
            }
            catch (Exception exc)
            {
                logger.Error<ProjectsMongoDbService>(exc, exc.Message);
                return false;
            }
            return true;
        }

        public List<ProjectModel> GetProjectsList()
        {
            if (!GetGlobalCmsProjectsCollection(out var projectsCollection))
            {
                logger.Error<ProjectsMongoDbService>("Failed to connect to Global CMS projects collection");
                return new List<ProjectModel>();
            }

            var allProjects = projectsCollection.Find(_ => true)
                .ToList()
                .OrderBy(x => x.Code)
                .ToList();

            return allProjects;
        }

        public List<ProjectModel> GetProjectsList(IUser currentUser)
        {
            var allProjects = GetProjectsList();

            var calculatedStartIds = currentUser.CalculateContentStartNodeIds(entityService, appCaches);
            if (calculatedStartIds.Length == 1 && calculatedStartIds[0] == -1)
            {
                // admin allowed access to everything
                return allProjects;
            }
            else
            {
                // only add projects whose content start node this user has access to
                return allProjects
                    .Where(x => x.ContentNodeId.HasValue && calculatedStartIds.Contains(x.ContentNodeId.Value))
                    .ToList();
            }
        }

        public ProjectModel GetProjectById(string id)
        {
            if (!GetGlobalCmsProjectsCollection(out var projectsCollection))
            {
                return null;
            }

            return projectsCollection.Find(GetFilterById(id)).FirstOrDefault();
        }

        public ProjectModel GetProjectByCode(string projectCode)
        {
            if (!GetGlobalCmsProjectsCollection(out var projectsCollection))
            {
                return null;
            }

            var filterByProjectCode = Builders<ProjectModel>.Filter.Where(x => x.Code == projectCode);
            return projectsCollection.Find(filterByProjectCode).FirstOrDefault();
        }

        public ProjectModel GetProjectByContentNodeId(int contentNodeId)
        {
            if (!GetGlobalCmsProjectsCollection(out var projectsCollection))
            {
                return null;
            }

            var filterByContentStartNodeId = Builders<ProjectModel>.Filter.Where(x => x.ContentNodeId == contentNodeId);
            return projectsCollection.Find(filterByContentStartNodeId).FirstOrDefault();
        }

        public ProjectModel GetProjectByMediaNodeId(int mediaNodeId)
        {
            if (!GetGlobalCmsProjectsCollection(out var projectsCollection))
            {
                return null;
            }

            var filterByMediaStartNodeId = Builders<ProjectModel>.Filter.Where(x => x.MediaNodeId == mediaNodeId);
            return projectsCollection.Find(filterByMediaStartNodeId).FirstOrDefault();
        }


        public bool SaveProject(ProjectModel project)
        {
            if (!GetGlobalCmsProjectsCollection(out var projectsCollection))
            {
                return false;
            }

            var replaceResult = projectsCollection.ReplaceOne(GetFilterById(project.Id), project);
            // ModifiedCount will only be 1 if something was updated. MatchedCount needs to be 1 to confirm that the project was found
            return replaceResult.IsAcknowledged && replaceResult.MatchedCount == 1;
        }

        public bool DeleteProjectFromProjectsList(string id)
        {
            if (!GetGlobalCmsProjectsCollection(out var projectsCollection))
            {
                return false;
            }

            var deleteResult = projectsCollection.DeleteOne(GetFilterById(id));
            return deleteResult.DeletedCount == 1;
        }

        public ParametersModel GetParametersForProject(string dbName)
        {
            var client = GetClient();
            var db = client?.GetDatabase(dbName);
            var collection = db?.GetCollection<ParametersModel>(ProjectConstants.ProjectDbParametersCollection);
            return collection?.Find(_ => true).FirstOrDefault();
        }

        public void SaveParametersForProject(string dbName, ParametersModel parameters)
        {
            var client = GetClient();
            var db = client?.GetDatabase(dbName);
            var collection = db?.GetCollection<ParametersModel>(ProjectConstants.ProjectDbParametersCollection);
            var replaceResult = collection.ReplaceOne(_ => true, parameters, new ReplaceOptions
            {
                IsUpsert = true
            });
        }

        public void CreateMongoDbCollectionIfNotExists(string dbName, string collectionName)
        {
            // should have better error handling for connection failures
            var client = GetClient();
            var db = client?.GetDatabase(dbName);
            if (db == null)
            {
                return;
            }

            var filter = new BsonDocument("name", collectionName);
            var options = new ListCollectionNamesOptions { Filter = filter };

            if (!db.ListCollectionNames(options).Any())
            {
                db.CreateCollection(collectionName);
            }
        }

        public void DeleteProjectMongoDbDatabases(string publishDbName, string previewDbName)
        {
            var client = GetClient();
            if (client == null)
            {
                return;
            }

            client.DropDatabase(publishDbName);
            client.DropDatabase(previewDbName);
        }

        private bool GetGlobalCmsProjectsCollection(out IMongoCollection<ProjectModel> projectsCollection)
        {
            projectsCollection = null;

            var db = GetGlobalCmsMongoDatabase();
            if (db == null)
            {
                return false;
            }

            projectsCollection = db.GetCollection<ProjectModel>(ProjectConstants.GlobalCmsProjectsCollection);
            return true;
        }

        private IMongoDatabase GetGlobalCmsMongoDatabase()
        {
            var client = GetClient();
            var db = client?.GetDatabase(ProjectConstants.GlobalCmsMongoDbDatabase);
            return db;
        }

        private FilterDefinition<ProjectModel> GetFilterById(string id)
        {
            return Builders<ProjectModel>.Filter.Where(x => x.Id == id);
        }

        private static IMongoClient _staticClient;
        protected static IMongoClient GetClient()
        {
            if (_staticClient == null)
            {
                ConnectionStringSettings connectionString = ConfigurationManager.ConnectionStrings["mongoDb"];

                if (string.IsNullOrWhiteSpace(connectionString?.ConnectionString))
                {
                    return null;
                }

                _staticClient = new MongoClient(connectionString.ConnectionString);
            }
            return _staticClient;
        }
    }
}
