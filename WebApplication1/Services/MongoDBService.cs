/*
 * File: MongoDBService.cs
 * Description: MongoDB service for database operations
 * Author: [Your Team Name]
 * Date: [Current Date]
 */

using MongoDB.Driver;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    /// <summary>
    /// MongoDB service for database operations
    /// </summary>
    public class MongoDBService
    {
        private readonly IMongoDatabase _database;

        public MongoDBService(IConfiguration configuration)
        {
            var connectionString = configuration["MongoDB:ConnectionString"];
            var databaseName = configuration["MongoDB:DatabaseName"];

            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        /// <summary>
        /// Collection for EV Owners (mobile app users)
        /// </summary>
        public IMongoCollection<User> EVOwners =>
            _database.GetCollection<User>("evowners");

        /// <summary>
        /// Collection for Web Application Users (staff)
        /// </summary>
        public IMongoCollection<WebUser> WebUsers =>
            _database.GetCollection<WebUser>("webusers");

        // Keep the old Users collection for backward compatibility during transition
        public IMongoCollection<User> Users =>
            _database.GetCollection<User>("users");
    }
}
