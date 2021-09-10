using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Mvc;

namespace azuread_sample
{
    public class MainController : Controller
    {
        private readonly HttpClient _httpClient;

        public MainController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpGet("~/"), ActionName("Home")]
        public async Task<IActionResult> HomeAsync()
        {
            if (!User?.Identities.Any(identity => identity.IsAuthenticated) ?? false)
            {
                return View("NotLoggedIn");
            }

            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            var at = await HttpContext.GetTokenAsync(MicrosoftAccountDefaults.AuthenticationScheme, "access_token");
            var orgFormatted = string.Empty;
            if (at is not null)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/organization");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", at);
                var resp = await _httpClient.SendAsync(request);
                var organization = await resp.Content.ReadAsStringAsync();

                var doc = JsonDocument.Parse(organization);
                orgFormatted = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });

                // request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/users"); requires User.Read.All
            }

            return View("LoggedIn", new LoggedInModel { Email = email, Organization = orgFormatted });
        }

        [HttpGet("~/login")]
        public IActionResult Login([FromQuery] string prompt)
        {
            // By default the client will be redirect back to the URL that issued the challenge (/login), send them to the home page instead (/).
            var properties = new MicrosoftChallengeProperties { RedirectUri = "/" };
            if (prompt == "login" || prompt == "consent")
            {
                properties.Prompt = prompt;
            }

            return Challenge(properties, MicrosoftAccountDefaults.AuthenticationScheme);
        }

        [HttpGet("~/logout"), ActionName("Logout")]
        public async Task<IActionResult> LogoutAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return View("LoggedOut");
        }
    }
}