using Mine.Services;
using Mine.Models;
using Mine.Views;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Xamarin.Forms;
using System.Linq;

namespace Mine.ViewModels
{
    /// <summary>
    /// Index View Model
    /// Manages the list of data records
    /// </summary>
    public class ItemIndexViewModel : BaseViewModel
    {
        //Singleton
        public static volatile ItemIndexViewModel instance;
        public static readonly object syncRoot = new object();
        
        public static ItemIndexViewModel Instance {
            get {
                if (instance == null) {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new ItemIndexViewModel();
                        }
                    }
                }
                return instance;
            }
        }



        // The Data set of records
        public ObservableCollection<ItemModel> Dataset { get; set; }

        /// <summary>
        /// Connection to the Data store
        /// </summary>
        /// 
        public IDataStore<ItemModel> DataMock => new MockDataStore();
        public IDataStore<ItemModel> DataSQL => new DatabaseService();
        public IDataStore<ItemModel> DataStore;

        public int CurrentDataSource = 0;
        // Command to force a Load of data
        public Command LoadDatasetCommand { get; set; }



        private bool _needsRefresh;

        /// <summary>
        /// Constructor
        /// 
        /// The constructor subscribes message listeners for crudi operations
        /// </summary>
        public ItemIndexViewModel()
        {
            Title = "Items";

            Dataset = new ObservableCollection<ItemModel>();
            LoadDatasetCommand = new Command(async () => await ExecuteLoadDataCommand());

            // Register the Create Message
            MessagingCenter.Subscribe<ItemCreatePage, ItemModel>(this, "Create", async (obj, data) =>
            {
                await Add(data as ItemModel);
            });
            MessagingCenter.Subscribe<ItemDeletePage, ItemModel>(this, "Delete", async (obj, data) =>
            {
                await Delete(data as ItemModel);
            });
            MessagingCenter.Subscribe<ItemUpdatePage, ItemModel>(this, "Update", async (obj, data) =>
            {
                await Update(data as ItemModel);
            });
            MessagingCenter.Subscribe<AboutPage, int>(this, "SetDataSource", async (obj, data) =>
            {
                SetDataSource(data);
            });
            MessagingCenter.Subscribe<AboutPage, bool>(this, "WipeDataList", async (obj, data) =>
             {
                 WipeDataList();
             });

        }


        public void WipeDataList()
        {
            DataStore.WipeDataList();
            SetNeedsRefresh(true);
        }

        public bool SetDataSource(int isSQL)
        {
            if (isSQL == 1)
            {
                DataStore = DataSQL;
                CurrentDataSource = 1;
            }
            else
            {
                DataStore = DataMock;
                CurrentDataSource = 0;
            }

            // Set Flag for Refresh
            SetNeedsRefresh(true);

            return true;
        }

        /// <summary>
        /// API to add the Data
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// 
        public async Task<bool> Add(ItemModel data)
        {
            Dataset.Add(data);
            var result = await DataStore.CreateAsync(data);

            return true;
        }
        public async Task<bool> Delete(ItemModel data)
        {

            var record = await Read(data.Id);
            if (record == null)
                return false;
            Dataset.Remove(data);
            var result = await DataStore.DeleteAsync(data.Id);

            return result;
        }
        public async Task<ItemModel> Read(string id)
        {
            var result = await DataStore.ReadAsync(id);
            return result;
        }
        public async Task<bool> Update(ItemModel data) {
            var record = await Read(data.Id);
            if (record == null)
                return false;
            record.Update(data);
            var result = await DataStore.UpdateAsync(record);
            await ExecuteLoadDataCommand();
            return result;
        }


        #region Refresh
        // Return True if a refresh is needed
        // It sets the refresh flag to false
        public bool NeedsRefresh()
        {
            if (_needsRefresh)
            {
                _needsRefresh = false;
                return true;
            }

            return false;
        }

        // Sets the need to refresh
        public void SetNeedsRefresh(bool value)
        {
            _needsRefresh = value;
        }

        // Command that Loads the Data
        private async Task ExecuteLoadDataCommand()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;

            try
            {
                Dataset.Clear();
                var dataset = await DataStore.IndexAsync(true);

                // Example of how to sort the database output using a linq query.
                // Sort the list
                dataset = dataset
                    .OrderBy(a => a.Name)
                    .ThenBy(a => a.Description)
                    .ToList();

                foreach (var data in dataset)
                {
                    Dataset.Add(data);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Force data to refresh
        /// </summary>
        public void ForceDataRefresh()
        {
            // Reset
            var canExecute = LoadDatasetCommand.CanExecute(null);
            LoadDatasetCommand.Execute(null);
        }
        #endregion Refresh
    }
}