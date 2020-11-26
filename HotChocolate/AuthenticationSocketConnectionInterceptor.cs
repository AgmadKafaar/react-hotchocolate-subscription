using HotChocolate.Server;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StarWars.AuthClasses;

namespace StarWars
{
    public class AuthenticationSocketConnectionInterceptor : ISocketConnectionInterceptor<HttpContext>
    {
        private readonly IAuthenticationSchemeProvider _schemeProvider;

        public AuthenticationSocketConnectionInterceptor(IAuthenticationSchemeProvider schemeProvider)
        {
            _schemeProvider = schemeProvider;
        }

        public async Task<ConnectionStatus> OnOpenAsync(
            HttpContext context,
            IReadOnlyDictionary<string, object> properties,
            CancellationToken cancellationToken)
        {
            if (properties.TryGetValue(AccessToken.PayloadKey, out var token) && token is string)
            {
                context.Items[AccessToken.ContextKey] = token;

                context.Features.Set<IAuthenticationFeature>(new AuthenticationFeature
                {
                    OriginalPath = context.Request.Path,
                    OriginalPathBase = context.Request.PathBase
                });

                // Give any IAuthenticationRequestHandler schemes a chance to handle the request

                var handlers = context.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();

                foreach (var scheme in await _schemeProvider.GetRequestHandlerSchemesAsync())
                {
                    var handler = handlers.GetHandlerAsync(context, scheme.Name) as IAuthenticationRequestHandler;

                    if (await handler?.HandleRequestAsync())
                        return ConnectionStatus.Reject();
                }

                var defaultScheme = await _schemeProvider.GetDefaultAuthenticateSchemeAsync();

                if (defaultScheme != null)
                {
                    var result = await context.AuthenticateAsync(defaultScheme.Name);

                    if (result?.Principal != null)
                    {
                        context.User = result.Principal;
                        return ConnectionStatus.Accept();
                    }
                }
            }

            return ConnectionStatus.Reject();
        }
    }
}