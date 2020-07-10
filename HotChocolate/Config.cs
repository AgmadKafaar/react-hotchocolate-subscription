using System.Collections.Generic;

namespace StarWars
{
    public class Config
    {
        public AuthenticationConfiguration Authentication { get; set; }

    }


    public class AuthenticationConfiguration
    {
        public string Scheme { get; set; }
        public string ClaimURL { get; set; }
        public List<string> Roles {get; set;}
        public List<string> Scopes { get; set; }
        public string ApiName { get; set; }
        public string Authority { get; set; }
        public string AuthorizationUrl { get; set; }
    }


}