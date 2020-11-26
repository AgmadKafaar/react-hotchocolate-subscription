using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.Server;
using StarWars.Data;
using StarWars.Types;
using StarWars.AuthClasses;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;

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

            services.AddHttpContextAccessor();

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

            services.AddSingleton<ISocketConnectionInterceptor<HttpContext>, AuthenticationSocketConnectionInterceptor>();

            services
                .AddAuthentication(options => {
                    options.DefaultScheme = ApiAuthenticationSchemes.DefaultScheme;
                    options.DefaultAuthenticateScheme = ApiAuthenticationSchemes.DefaultScheme;
                })

                .AddJwtBearer(ApiAuthenticationSchemes.WebsocketScheme, default)

                .AddJwtBearer(ApiAuthenticationSchemes.DefaultScheme, options =>
                {
                    options.Authority = _config.Authentication.Authority;
                    options.Audience = _config.Authentication.Audience;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidTypes = new[] { "at+jwt" }
                    };

                    options.ForwardDefaultSelector = ApiAuthenticationSchemes.ForwardWebsocket();

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = (context) =>
                        {
                            context.Token = ApiAuthenticationSchemes.TokenRetriever()(context.Request);
                            return Task.CompletedTask;
                        }
                    };
                })
                .AddOAuth2Introspection(ApiAuthenticationSchemes.IntrospectionScheme, options =>
                {
                    options.Authority = "https://ids-nhi-test.azurewebsites.net";
                    options.TokenRetriever = ApiAuthenticationSchemes.TokenRetriever();

                    options.ClientId = "";

                    options.ClientSecret = "";
                });

            // Add GraphQL Services
            services
                .AddGraphQLServer()
                
                .AddAuthorization()
                
                .AddQueryType<QueryType>()
                .AddMutationType<MutationType>()
                .AddSubscriptionType<SubscriptionType>()

                .AddType<HumanType>()
                .AddType<DroidType>()
                .AddType<EpisodeType>()

                .AddInMemorySubscriptions()

                .UseField(next => async context =>
                {
                    var hca = context.Services.GetRequiredService<IHttpContextAccessor>();

                    context.ContextData["ClaimsPrincipal"] = hca.HttpContext.User;

                    await next(context);
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(Cors);

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseWebSockets();

            app.UsePlayground();

            app.UseEndpoints(endpoints => endpoints.MapGraphQL());
        }
    }
}
