using BIT.Data.Sync.Imp;
using BIT.Data.Sync.Server;
using BIT.Data.Sync.Xpo.DeltaStore;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.MultiTenancy;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.Authentication.ClientServer;
using DevExpress.ExpressApp.WebApi.Services;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using DevExpress.Xpo;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using XafMT2.WebApi.JWT;
namespace XafMT2.WebApi;

public class Startup {
    public Startup(IConfiguration configuration) {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services) {
        services.AddScoped<IAuthenticationTokenProvider, JwtTokenProviderService>();

        services.AddXafWebApi(builder =>
        {
            builder.AddXpoServices();

            SyncServerNode Node1 = CreateNode(InMemoryDataStoreProvider.ConnectionString,"Node1");
            //HACK to add a node with a database
            //SyncServerNode Node1 = CreateNode("Integrated Security=SSPI;Pooling=false;Data Source=(localdb)\\mssqllocaldb;Initial Catalog=Node1", "Node1");
            SyncServerNode Node2 = CreateNode(InMemoryDataStoreProvider.ConnectionString, "Node2");
            SyncServerNode Node3 = CreateNode(InMemoryDataStoreProvider.ConnectionString, "Node3");
            SyncServerNode Node4 = CreateNode(InMemoryDataStoreProvider.ConnectionString, "Node4");

            builder.Services.AddSingleton<ISyncServer>(new SyncServer(Node1, Node2, Node3, Node4));


            builder.ConfigureOptions(options =>
            {
                // Make your business objects available in the Web API and generate the GET, POST, PUT, and DELETE HTTP methods for it.
                // options.BusinessObject<YourBusinessObject>();
            });

            builder.Modules
                .AddReports(options =>
                {
                    options.ReportDataType = typeof(DevExpress.Persistent.BaseImpl.ReportDataV2);
                })
                .AddValidation()
                .Add<XafMT2.Module.XafMT2Module>();


            builder.AddMultiTenancy()
#if !RELEASE
                .WithTenantDatabaseUpdater()
#endif
                .WithHostDatabaseConnectionString(Configuration.GetConnectionString("ConnectionString"))
#if EASYTEST
                .WithHostDatabaseConnectionString(Configuration.GetConnectionString("EasyTestConnectionString"))
#endif
                .WithMultiTenancyModelDifferenceStore(options =>
                {
#if !RELEASE
                    options.UseTenantSpecificModel = false;
#endif
                })
                .WithTenantResolver<TenantByEmailResolver>();

            builder.ObjectSpaceProviders
                .AddSecuredXpo((serviceProvider, options) =>
                {
                    string connectionString = serviceProvider.GetRequiredService<IConnectionStringProvider>().GetConnectionString();
                    options.ConnectionString = connectionString;
                    options.ThreadSafe = true;
                    options.UseSharedDataStoreProvider = true;
                })
                .AddNonPersistent();

            builder.Security
                .UseIntegratedMode(options =>
                {
                    options.Lockout.Enabled = true;

                    options.RoleType = typeof(PermissionPolicyRole);
                    // ApplicationUser descends from PermissionPolicyUser and supports the OAuth authentication. For more information, refer to the following topic: https://docs.devexpress.com/eXpressAppFramework/402197
                    // If your application uses PermissionPolicyUser or a custom user type, set the UserType property as follows:
                    options.UserType = typeof(XafMT2.Module.BusinessObjects.ApplicationUser);
                    // ApplicationUserLoginInfo is only necessary for applications that use the ApplicationUser user type.
                    // If you use PermissionPolicyUser or a custom user type, comment out the following line:
                    options.UserLoginInfoType = typeof(XafMT2.Module.BusinessObjects.ApplicationUserLoginInfo);
                    options.UseXpoPermissionsCaching();
                    options.Events.OnSecurityStrategyCreated += securityStrategy =>
                    {
                        ((SecurityStrategy)securityStrategy).PermissionsReloadMode = PermissionsReloadMode.CacheOnFirstAccess;
                    };
                })
                .AddPasswordAuthentication(options =>
                {
                    options.IsSupportChangePassword = true;
                });

            builder.AddBuildStep(application =>
            {
                application.ApplicationName = "SetupApplication.XafMT2";
                application.CheckCompatibilityType = DevExpress.ExpressApp.CheckCompatibilityType.DatabaseSchema;
#if DEBUG
                if (System.Diagnostics.Debugger.IsAttached && application.CheckCompatibilityType == CheckCompatibilityType.DatabaseSchema)
                {
                    application.DatabaseUpdateMode = DatabaseUpdateMode.UpdateDatabaseAlways;
                    application.DatabaseVersionMismatch += (s, e) =>
                    {
                        e.Updater.Update();
                        e.Handled = true;
                    };
                }
#endif
            });
        }, Configuration);

        services
            .AddControllers()
            .AddOData((options, serviceProvider) => {
                options
                    .AddRouteComponents("api/odata", new EdmModelBuilder(serviceProvider).GetEdmModel())
                    .EnableQueryFeatures(100);
            });

        services.AddAuthentication()
            .AddJwtBearer(options => {
                options.TokenValidationParameters = new TokenValidationParameters() {
                    ValidateIssuerSigningKey = true,
                    //ValidIssuer = Configuration["Authentication:Jwt:Issuer"],
                    //ValidAudience = Configuration["Authentication:Jwt:Audience"],
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Authentication:Jwt:IssuerSigningKey"]))
                };
            });

        services.AddAuthorization(options => {
            options.DefaultPolicy = new AuthorizationPolicyBuilder(
                JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .RequireXafAuthentication()
                    .Build();
        });

        services.AddSwaggerGen(c => {
            c.EnableAnnotations();
            c.SwaggerDoc("v1", new OpenApiInfo {
                Title = "XafMT2 API",
                Version = "v1",
                Description = @"Use AddXafWebApi(options) in the XafMT2.WebApi\Startup.cs file to make Business Objects available in the Web API."
            });
            c.AddSecurityDefinition("JWT", new OpenApiSecurityScheme() {
                Type = SecuritySchemeType.Http,
                Name = "Bearer",
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement() {
                {
                    new OpenApiSecurityScheme() {
                        Reference = new OpenApiReference() {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "JWT"
                        }
                    },
                    new string[0]
                },
            });
        });

        services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(o => {
            //The code below specifies that the naming of properties in an object serialized to JSON must always exactly match
            //the property names within the corresponding CLR type so that the property names are displayed correctly in the Swagger UI.
            //XPO is case-sensitive and requires this setting so that the example request data displayed by Swagger is always valid.
            //Comment this code out to revert to the default behavior.
            //See the following article for more information: https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializeroptions.propertynamingpolicy
            o.JsonSerializerOptions.PropertyNamingPolicy = null;
        });
    }

    private static SyncServerNode CreateNode(string ConnectionString,string NodeId)
    {
        var NodeDal=XpoDefault.GetDataLayer( ConnectionString, DevExpress.Xpo.DB.AutoCreateOption.DatabaseAndSchema);

        NodeDal.UpdateSchema(false);

        XpoDeltaStore xPODeltaStore = new XpoDeltaStore(NodeDal, new XpoSequenceService(new YearSequencePrefixStrategy(), NodeDal));
        SyncServerNode syncServerNode = new SyncServerNode(xPODeltaStore, null, NodeId);
        return syncServerNode;
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
        if(env.IsDevelopment()) {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "XafMT2 WebApi v1");
            });
        }
        else {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. To change this for production scenarios, see: https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        app.UseHttpsRedirection();
        app.UseRequestLocalization();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints => {
            endpoints.MapControllers();
            endpoints.MapXafEndpoints();
        });
    }
}
