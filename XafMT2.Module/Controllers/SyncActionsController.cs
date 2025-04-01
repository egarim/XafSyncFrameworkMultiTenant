using BIT.Data.Sync;
using BIT.Data.Sync.Client;
using BIT.Data.Sync.Xpo;
using DevExpress.ExpressApp.Actions;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XafMT2.Module.Controllers
{
    public class SyncActionsController:BIT.Data.Sync.Xaf.XafSyncControllerBase
    {
        public SyncActionsController()
        {

        }

       
        protected  async override void Pull_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var cnx= this.Application.ServiceProvider.GetService<DevExpress.ExpressApp.MultiTenancy.IConnectionStringProvider>();

            ISyncDataStore store = (ISyncDataStore)   XpoDefault.GetConnectionProvider(cnx.GetConnectionString(), DevExpress.Xpo.DB.AutoCreateOption.SchemaAlreadyExists);

            var r = await store.PullAsync();

        }
      

        protected async override void Push_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var cnx = this.Application.ServiceProvider.GetService<DevExpress.ExpressApp.MultiTenancy.IConnectionStringProvider>();

            string connectionString = cnx.GetConnectionString();

            IDisposable[] disposables = null;
            var store = (ISyncDataStore)SyncDataStore.CreateProviderFromString(connectionString, AutoCreateOption.None, out disposables);
            var test = await store.PushAsync(default);
           


        }
    }
}
