using MongoDB.Bson;
using MongoDB.Driver;
using PDS_Server.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace PDS_Server.Repositories
{
    public class MongoRepository<T> : IMongoRepository<T>
        where T : DbElement
    {
        private readonly IMongoClient Client;
        private Dictionary<string, IMongoCollection<T>> Collections = new Dictionary<string, IMongoCollection<T>>();
        public MongoRepository(IMongoClient client)
        {
            Client = client;
        }
        private IMongoCollection<T> GetCollection(string teamId = null)
        {
            IMongoCollection<T> collection;
            string teamKey = teamId != null ? $"rt-{teamId}" : "rt-db";
            if (!Collections.TryGetValue(teamKey, out collection))
            {
                collection = Client.GetDatabase(teamKey).GetCollection<T>(typeof(T).Name.Remove(0, 2));
                Collections.Add(teamKey, collection);
            }
            return collection;
        }
        public async Task<ObjectId> Create(T element, string teamId)
        {
            await GetCollection(teamId).InsertOneAsync(element);
            return element.Id;
        }
        public async Task<bool> Delete(ObjectId objectId, string teamId)
        {
            FilterDefinition<T> filter = Builders<T>.Filter.Eq(c => c.Id, objectId);
            DeleteResult result = await GetCollection(teamId).DeleteOneAsync(filter);
            return result.DeletedCount == 1;
        }
        public async Task<bool> Delete(ObjectId[] objectIds, string teamId)
        {
            DeleteResult result = await GetCollection(teamId).DeleteManyAsync(_ => objectIds.Contains(_.Id));
            return result.DeletedCount == objectIds.Length;
        }

        public async Task<T> Get(ObjectId objectId, string teamId)
        {
            FilterDefinition<T> filter = Builders<T>.Filter.Eq(c => c.Id, objectId);
            T element = await GetCollection(teamId).Find(filter).FirstOrDefaultAsync();
            return element;
        }

        public async Task<IEnumerable<T>> Get(string teamId)
        {
            IEnumerable<T> elements = await GetCollection(teamId).Find(_ => true).ToListAsync();
            return elements;
        }
        public async Task<bool> Update(T element, string teamId)
        {
            FilterDefinition<T> filter = Builders<T>.Filter.Eq(c => c.Id, element.Id);
            UpdateDefinitionBuilder<T> updateBuilder = Builders<T>.Update;
            UpdateDefinition<T> update = null;
            Type type = element.GetType();
            IList<PropertyInfo> props = new List<PropertyInfo>(type.GetProperties());
            foreach (PropertyInfo prop in props)
            {
                if (prop.Name == "Id") continue;
                object propValue = prop.GetValue(element, null);
                if (propValue != null)
                    update = update == null ? updateBuilder.Set(prop.Name, propValue) : update.Set(prop.Name, propValue);
            }
            if (update != null)
            {
                UpdateResult result = await GetCollection(teamId).UpdateOneAsync(filter, update);
                return result.ModifiedCount == 1;
            }
            return false;
        }
    }
}
