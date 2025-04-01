using BIT.Data.Sync.Xpo;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XafMT2.Module
{
    public class MySyncFrameworkDataSpaceProvider : IXpoDataStoreProvider
    {

        public bool EnableDeltaTracking { get; set; }

        /// <summary>
        /// Gets the connection string for the data store.
        /// </summary>
        public string ConnectionString { get; protected set; }

        public MySyncFrameworkDataSpaceProvider(string SyncDataStoreConnectionString)
        {
            ConnectionString = SyncDataStoreConnectionString;
        }


        /// <summary>
        /// Creates a data store that checks the database schema.
        /// </summary>
        /// <param name="disposableObjects">An array of IDisposable objects that the caller must dispose of when the data store is no longer needed.</param>
        /// <returns>The created IDataStore instance.</returns>
        public IDataStore CreateSchemaCheckingStore(out IDisposable[] disposableObjects)
        {
            disposableObjects = Array.Empty<IDisposable>();
            // return this.dataStore.DataDataStore;
            return CreateDataStore().DataDataStore;
        }

        ISyncDataStore dataStore;
        private ISyncDataStore CreateDataStore()
        {

            if(dataStore==null)
            {
                dataStore = XpoDefault.GetConnectionProvider(ConnectionString, AutoCreateOption.DatabaseAndSchema) as ISyncDataStore;
            }
          
            dataStore.EnableDeltaTracking = this.EnableDeltaTracking;
            return dataStore;
        }

        /// <summary>
        /// Creates a data store that can update the database schema.
        /// </summary>
        /// <param name="allowUpdateSchema">A boolean value indicating whether the data store should be allowed to update the database schema.</param>
        /// <param name="disposableObjects">An array of IDisposable objects that the caller must dispose of when the data store is no longer needed.</param>
        /// <returns>The created IDataStore instance.</returns>
        public IDataStore CreateUpdatingStore(bool allowUpdateSchema, out IDisposable[] disposableObjects)
        {
            disposableObjects = Array.Empty<IDisposable>();
            return CreateDataStore().DataDataStore;
        }

        /// <summary>
        /// Creates a data store for performing data operations.
        /// </summary>
        /// <param name="disposableObjects">An array of IDisposable objects that the caller must dispose of when the data store is no longer needed.</param>
        /// <returns>The created IDataStore instance.</returns>
        public IDataStore CreateWorkingStore(out IDisposable[] disposableObjects)
        {
            disposableObjects = Array.Empty<IDisposable>();
            //return this.dataStore;
            return CreateDataStore();
        }


    }
}
