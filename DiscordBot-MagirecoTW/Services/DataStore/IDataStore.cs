using System;
using System.Collections.Generic;
using System.Text;

namespace MitamaBot.Services.DataStore
{
    //保留更換至其他 SQL 的活路

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T1">The type of Object</typeparam>
    /// <typeparam name="T2">The type of PrimaryKey</typeparam>
    public interface IDataStore<T1, T2>
    {
        T2 AddItem(T1 item);
        bool UpsertItem(T1 item, T2 id);
        bool UpdateItem(T1 item, T2 id);
        bool DeleteItem(T2 id);
        IEnumerable<T1> GetItems();
        T1 GetItem(T2 id);
        IEnumerable<T1> FindItems(System.Linq.Expressions.Expression<Func<T1, bool>> predicate);
    }
}
