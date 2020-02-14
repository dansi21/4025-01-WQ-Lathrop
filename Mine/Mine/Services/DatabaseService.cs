using SQLite;
using System;
using System.Linq;
using System.Threading.Tasks;
using Mine.Models;
using System.Collections.Generic;

namespace Mine.Services
{
    /// <summary>
    /// Database Services
    /// Will write to the local data store
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DatabaseService
    {
        /// <summary>
        /// Set the class to load on demand
        /// Saves app boot time
        /// </summary>
        static readonly Lazy<SQLiteAsyncConnection> lazyInitializer = new Lazy<SQLiteAsyncConnection>(() =>
        {
            return new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
        });

        static SQLiteAsyncConnection Database => lazyInitializer.Value;
        static bool initialized = false;

        /// <summary>
        /// Constructor
        /// All the database to start up
        /// </summary>
        public DatabaseService()
        {
            InitializeAsync().SafeFireAndForget(false);
        }

        /// <summary>
        /// Create the Table if it does not exist
        /// </summary>
        /// <returns></returns>
        async Task InitializeAsync()
        {
            if (!initialized)
            {
                if (!Database.TableMappings.Any(m => m.MappedType.Name == typeof(ItemModel).Name))
                {
                    await Database.CreateTablesAsync(CreateFlags.None, typeof(ItemModel)).ConfigureAwait(false);
                    initialized = true;
                }
            }
        }
        public Task<int> CreateAsync(ItemModel data)
        {
            return Database.InsertAsync(data);
        }
        public Task<ItemModel> ReadAsync(string id)
        {
            return Database.Table<ItemModel>().Where(i => i.Id.Equals(id)).FirstOrDefaultAsync();
        }
        public Task<int> UpdateAsync(ItemModel item) {
            return Database.UpdateAsync(item);
        }
        public Task<int> DeleteAsync(ItemModel item)
        {
            return Database.DeleteAsync(item);
        }
        public Task<List<ItemModel>> IndexAsync() {
            return Database.Table<ItemModel>().ToListAsync();
        }
    }
}