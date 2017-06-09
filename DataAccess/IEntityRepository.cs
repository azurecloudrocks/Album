using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace AzureCloudRocks.CodeSamples.Album.DataAccess
{
    public interface IEntityRepository<T>
    {
        T GetEntity(string Id);
        IEnumerable<T> GetEntities(Expression<Func<T, bool>> filter);
        void InsertOrUpdate(T entity);
        void Delete(string Id);
        void Clear();
        //object GetEntities(Func<Models.VinEntity, bool> func);

        //object GetEntities(Func<Models.VinEntity, bool> func);
    }
}