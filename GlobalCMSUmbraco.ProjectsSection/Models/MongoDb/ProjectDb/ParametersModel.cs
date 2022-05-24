using MongoDB.Bson.Serialization.Attributes;

namespace GlobalCMSUmbraco.ProjectsSection.Models.MongoDb.ProjectDb
{
    /// <summary>
    /// Class defining the structure of each Project database 'Params' collection. For settings that are managed by CMS but needed by front-end.
    /// </summary>
    [BsonIgnoreExtraElements]
    public class ParametersModel
    {
        /// <summary>
        /// Maps API key to be used in the front-end website
        /// </summary>
        public string MapsApiKey { get; set; }
    }
}
