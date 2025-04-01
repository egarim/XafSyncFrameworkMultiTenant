using BIT.Data.Sync.Xpo;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Design;
using DevExpress.ExpressApp.MultiTenancy;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Win.ApplicationBuilder;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using DevExpress.XtraEditors;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;

namespace XafMT2.Win;

public class ApplicationBuilder : IDesignTimeApplicationFactory {
    public static WinApplication BuildApplication(string connectionString) {
        var builder = WinApplication.CreateBuilder();
        // Register custom services for Dependency Injection. For more information, refer to the following topic: https://docs.devexpress.com/eXpressAppFramework/404430/
        // builder.Services.AddScoped<CustomService>();
        // Register 3rd-party IoC containers (like Autofac, Dryloc, etc.)
        // builder.UseServiceProviderFactory(new DryIocServiceProviderFactory());
        // builder.UseServiceProviderFactory(new AutofacServiceProviderFactory());

        builder.UseApplication<XafMT2WindowsFormsApplication>();
        builder.Modules
            .AddCloningXpo()
            .AddConditionalAppearance()
            .AddReports(options => {
                options.EnableInplaceReports = true;
                options.ReportDataType = typeof(DevExpress.Persistent.BaseImpl.ReportDataV2);
                options.ReportStoreMode = DevExpress.ExpressApp.ReportsV2.ReportStoreModes.XML;
            })
            .AddValidation(options => {
                options.AllowValidationDetailsAccess = false;
            })
            .Add<XafMT2.Module.XafMT2Module>()
            .Add<XafMT2WinModule>();

        builder.AddMultiTenancy()
            .WithHostDatabaseConnectionString(connectionString)
            .WithMultiTenancyModelDifferenceStore(options => {
#if !RELEASE
                options.UseTenantSpecificModel = false;
#endif
            })
            .WithTenantResolver<TenantByEmailResolver>();

        builder.ObjectSpaceProviders
            .AddSecuredXpo((application, options) => {
                string connectionString = application.ServiceProvider.GetRequiredService<IConnectionStringProvider>().GetConnectionString();
                options.ConnectionString = connectionString;

                //options.ConnectionString = connectionString;
                //SyncDataStore.Register();
                //var Ds = XpoDefault.GetConnectionProvider(connectionString, AutoCreateOption.DatabaseAndSchema) as SyncDataStore;
                //options.UseCustomDataStoreProvider(new CustomSyncFrameworkDataSpaceProvider(connectionString));

            })
            .AddNonPersistent();
        builder.Security
            .UseIntegratedMode(options => {
                options.Lockout.Enabled = true;

                options.RoleType = typeof(PermissionPolicyRole);
                options.UserType = typeof(XafMT2.Module.BusinessObjects.ApplicationUser);
                options.UserLoginInfoType = typeof(XafMT2.Module.BusinessObjects.ApplicationUserLoginInfo);
                options.UseXpoPermissionsCaching();
                options.Events.OnSecurityStrategyCreated += securityStrategy => {
                   // Use the 'PermissionsReloadMode.NoCache' option to load the most recent permissions from the database once
                   // for every Session instance when secured data is accessed through this instance for the first time.
                   // Use the 'PermissionsReloadMode.CacheOnFirstAccess' option to reduce the number of database queries.
                   // In this case, permission requests are loaded and cached when secured data is accessed for the first time
                   // and used until the current user logs out.
                   // See the following article for more details: https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.Security.SecurityStrategy.PermissionsReloadMode.
                   ((SecurityStrategy)securityStrategy).PermissionsReloadMode = PermissionsReloadMode.NoCache;
                };
            })
            .AddPasswordAuthentication();
        builder.AddBuildStep(application => {
            application.ConnectionString = connectionString;
#if DEBUG
            if(System.Diagnostics.Debugger.IsAttached && application.CheckCompatibilityType == CheckCompatibilityType.DatabaseSchema) {
                application.DatabaseUpdateMode = DatabaseUpdateMode.UpdateDatabaseAlways;
            }
#endif
        });
        var winApplication = builder.Build();
        return winApplication;
    }

    XafApplication IDesignTimeApplicationFactory.Create()
        => BuildApplication(XafApplication.DesignTimeConnectionString);
}
