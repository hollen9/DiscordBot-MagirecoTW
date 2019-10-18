using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace MitamaBot.Services.DataStore
{
    public abstract class LiteDbCollectionWrapper<T>
    {
        private LiteDatabase Database { get; set; }
        private LiteCollection<T> Collection { get; set; }

        public LiteDbCollectionWrapper(LiteDatabase database)
        {
            Database = database;
            Collection = Database.GetCollection<T>();
        }

        public bool UpsertItem(T item, BsonValue id)
        {
            var existed = Collection.FindById(id);
            if (existed == null)
            {
                Collection.Insert(item);
                return true;
            }
            else if (!item.Equals(existed))
            {
                var result = Collection.Update(item);
                return result;
            }
            else
            {
                return false;
            }
        }
        public bool DeleteItem(BsonValue id)
        {
            return Collection.Delete(id);
        }
        public IEnumerable<T> GetItems()
        {
            return Collection.FindAll();
        }
        public T GetItem(BsonValue id)
        {
            var existed = Collection.FindById(id);
            return existed;
        }
    }
}
