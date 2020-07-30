using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace StarWars
{
    public class WebSocketContext
    {
        public ClaimsPrincipal User { get; set; }
    }
}