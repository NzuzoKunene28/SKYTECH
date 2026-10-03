using SkyTech.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SkyTech.Services
{
    public class PayStackService
    {
        private readonly string SecretKey;
        private const string BaseUrl = "https://api.paystack.co";

        public PayStackService(IConfiguration configuration)
        {
            SecretKey = configuration["PayStack:SecretKey"];
        }

        private HttpClient CreateClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", SecretKey);
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        public async Task<PayStackInitResponse> InitializeTransaction(
            string email, decimal amountInRand, string reference, string callbackUrl)
        {
            using var client = CreateClient();
            var payload = new
            {
                email,
                amount = (int)(amountInRand * 100),
                reference,
                callback_url = callbackUrl
            };
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{BaseUrl}/transaction/initialize", content);
            return JsonConvert.DeserializeObject<PayStackInitResponse>(await response.Content.ReadAsStringAsync());
        }

        public async Task<PayStackVerifyResponse> VerifyTransaction(string reference)
        {
            using var client = CreateClient();
            var response = await client.GetAsync($"{BaseUrl}/transaction/verify/{reference}");
            return JsonConvert.DeserializeObject<PayStackVerifyResponse>(await response.Content.ReadAsStringAsync());
        }
    }
}