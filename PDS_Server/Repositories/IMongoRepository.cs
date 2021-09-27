using MongoDB.Bson;
using PDS_Server.Elements;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PDS_Server.Repositories
{
    public interface IMongoRepository<T> where T : DbElement
    {
        Task<ObjectId> Create(T instance, string teamId = null);
        Task<T> Get(ObjectId objectId, string teamId = null);
        Task<IEnumerable<T>> Get(string teamId = null);
        Task<bool> Update(T instance, string teamId = null);
        Task<bool> Delete(ObjectId objectId, string teamId = null);
        Task<bool> Delete(ObjectId[] objectIds, string teamId = null);
    }
}
