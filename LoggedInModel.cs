using System.Collections.Generic;
using System.Security.Claims;

namespace azuread_sample
{
    public class LoggedInModel
    {
        public string Upn { get; set; }
        public IEnumerable<Claim> Claims { get; set; }
    }
}