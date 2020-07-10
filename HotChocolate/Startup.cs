using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IdentityModel.AspNetCore.OAuth2Introspection;
using IdentityServer4.AccessTokenValidation;
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Playground;
using HotChocolate.AspNetCore.Voyager;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Server;
using HotChocolate.Subscriptions;
using StarWars.Data;
using StarWars.Types;
using System;
using System.Threading.Tasks;

namespace StarWars
{
    public class Startup
    {
        private readonly Config _config;
        public Startup(IConfiguration configuration)
        {
            _config = new Config();
            configuration.Bind(_config);
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        readonly string Cors = "_cors";

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // Add the custom services like repositories etc ...
            services.AddSingleton<CharacterRepository>();
            services.AddSingleton<ReviewRepository>();

            services.AddCors(options =>
            {
                options.AddPolicy(Cors,
                builder =>
                {
                    builder.WithOrigins("*")
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                });
            });

            // Custom policy
            services.AddAuthorization();

            services
                .AddAuthentication(options => {
                    options.DefaultScheme = IdentityServerAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultAuthenticateScheme = IdentityServerAuthenticationDefaults.AuthenticationScheme;
                })
                .AddJwtBearer("Websockets", ctx => { })
                .AddIdentityServerAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.ForwardDefaultSelector = context =>
                    {
                        if (!context.Items.ContainsKey(AuthenticationSocketInterceptor.HTTP_CONTEXT_WEBSOCKET_AUTH_KEY) && context.Request.Headers.TryGetValue("Upgrade", out var value) && value.Count > 0 && value[0] is string stringValue &&  stringValue == "websocket")
                            {
                                return "Websockets";
                            }
                        return IdentityServerAuthenticationDefaults.AuthenticationScheme;
                    };
                    options.TokenRetriever = new Func<HttpRequest, string>(req =>
                    {
                        if (req.HttpContext.Items.TryGetValue(AuthenticationSocketInterceptor.HTTP_CONTEXT_WEBSOCKET_AUTH_KEY, out object token) && token is string stringToken)
                        {
                            return stringToken;
                        }

                        var fromHeader = TokenRetrieval.FromAuthorizationHeader();
                        var fromQuery = TokenRetrieval.FromQueryString();  // Query string auth
                        string tokenHttp = fromHeader(req) ?? fromQuery(req);
                        return tokenHttp;
                    });

                    options.Authority = _config.Authentication.Authority;
                    options.ApiName = _config.Authentication.ApiName;
                    options.RequireHttpsMetadata = false;
                });

            services.AddSingleton<ISocketConnectionInterceptor<HttpContext>, AuthenticationSocketInterceptor>();

            // Add in-memory event provider
            services.AddInMemorySubscriptionProvider();

            // Add GraphQL Services
            services.AddGraphQL(sp => SchemaBuilder.New()
                .AddServices(sp)

                // Adds the authorize directive and
                // enable the authorization middleware.
                .AddAuthorizeDirectiveType()

                .AddQueryType<QueryType>()
                .AddMutationType<MutationType>()
                .AddSubscriptionType<SubscriptionType>()
                .AddType<HumanType>()
                .AddType<DroidType>()
                .AddType<EpisodeType>()
                .Create());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(Cors);
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseRouting();

            app
                .UseWebSockets()
                .UseGraphQL()
                .UsePlayground()
                .UseVoyager();
        }
    }
}
