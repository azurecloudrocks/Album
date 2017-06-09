using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;

namespace AzureCloudRocks.CodeSamples.Album.DataAccess
{
    public class EntityRepository<T> : IEntityRepository<T> where T : TableServiceEntity
    {
        private readonly CloudStorageAccount _cloudStorageAccount;
        private readonly string _tableName;

        public EntityRepository(CloudStorageAccount cloudStorageAccount, string tableName)
        {
            _cloudStorageAccount = cloudStorageAccount;
            _tableName = tableName;
        }

        public IQueryable<T> CreateQuery()
        {
            return GetTableServiceContext().
            CreateQuery<T>(_tableName);
        }

        public TableServiceContext GetTableServiceContext()
        {
            var client = _cloudStorageAccount.CreateCloudTableClient();
            var context = client.GetDataServiceContext();

            context.MergeOption = MergeOption.NoTracking;
            context.IgnoreMissingProperties = true;

            return context;
        }

        public T GetEntity(string Id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> GetEntities(System.Linq.Expressions.Expression<Func<T, bool>> filter)
        {
            var entities = (filter != null) ? CreateQuery().Where(filter).AsTableServiceQuery<T>().Execute() : CreateQuery().AsTableServiceQuery<T>().Execute();
            return entities;
        }

        public void InsertOrUpdate(T entity)
        {
            var ctx = GetTableServiceContext();

            try
            {
                ctx.AttachTo(_tableName, entity, null);
                ctx.UpdateObject(entity);
                ctx.SaveChangesWithRetries();
            }
            catch (DataServiceRequestException ex)
            {
                var clientEx = ex.InnerException as DataServiceClientException;

                //if emulator supports upsert, then change this logic
                if (clientEx != null && clientEx.StatusCode == 409)
                {
                    //UpdateEntity(entity);
                }
                else if (clientEx != null && clientEx.StatusCode == 404)
                {
                    _cloudStorageAccount.CreateCloudTableClient().CreateTable(_tableName);
                    ctx.SaveChangesWithRetries();
                }
                else
                {
                    throw;
                }
            }
        }

        public void Delete(string Id)
        {
            var entities = GetEntities(x=>x.PartitionKey == Id);
            var ctx = GetTableServiceContext();
            foreach (var item in entities)
            {
                ctx.AttachTo(_tableName, item, null);
                ctx.DeleteObject(item);
                ctx.SaveChangesWithRetries();
            }
        }

        public void Clear()
        {
            var entities = GetEntities(null).ToList();
            var ctx = GetTableServiceContext();
            foreach (var item in entities)
            {
                ctx.AttachTo(_tableName, item, "*");
                ctx.DeleteObject(item);
                ctx.SaveChangesWithRetries();
            }
        }
    }
}