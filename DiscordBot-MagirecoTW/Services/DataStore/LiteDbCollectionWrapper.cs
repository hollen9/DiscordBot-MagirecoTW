﻿using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace MitamaBot.Services.DataStore
{
    public abstract class LiteDbCollectionWrapper<T> : IDataStore<T, BsonValue>
    {
        private LiteDatabase Database { get; set; }
        private LiteCollection<T> Collection { get; set; }

        public LiteDbCollectionWrapper(LiteDatabase database)
        {
            Database = database;
            Collection = Database.GetCollection<T>();
        }

        public BsonValue AddItem(T item)
        {
            return Collection.Insert(item);
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
        public IEnumerable<T> FindItems(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            var items = Collection.Find(predicate);
            return items;
        }
    }
}
