using System;
using System.IO;
using static System.Environment;

using Microsoft.Extensions.Configuration;

using MongoDB.Driver;
using MongoDB.Bson.Serialization;

namespace Ionburst.Apps.IonFS.Repo.Mongo
{
    public class MongoDBWrapper
    {
        private readonly IConfiguration _configuration;

        internal MongoClient MongoAtlas;
        internal IMongoDatabase MongoDB;

        internal MongoDBWrapper(string databaseName, string repoName)
        {
            string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                .AddJsonFile($"{GetFolderPath(SpecialFolder.UserProfile, SpecialFolderOption.None)}/.ionfs/appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            string connectionString = _configuration[$"IonFS:RepositoryConnections:{repoName}"];
            MongoAtlas = new MongoClient($"{connectionString}/{databaseName}?retryWrites=true&w=majority");

            try
            {
                BsonClassMap.RegisterClassMap<MongoDBIonFSMetadata>();
            }
            catch (ArgumentException e)
            {
                // Been done
            }
            catch (MongoException e)
            {
                throw new Exception("MongoDB exception", e);
            }
            catch (Exception e)
            {
                throw new Exception("System exception", e);
            }

            MongoDB = MongoAtlas.GetDatabase(databaseName);
        }
    }
}
