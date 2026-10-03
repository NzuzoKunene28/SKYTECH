using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SkyTech.Services
{
    public class PayStackSubscriptionService
    {
        private readonly string _secretKey;
        private const string Base = "https://api.paystack.co";

        public PayStackSubscriptionService(IConfiguration config)
        {
            _secretKey = config["PayStack:SecretKey"];
        }

        private HttpClient Client()
        {
            var c = new HttpClient();
            c.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _secretKey);
            c.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            return c;
        }

        private static bool JBool(JObject obj, string key)
        {
            var token = obj?[key];
            if (token == null) return false;
            return token.Value<bool>();
        }

        private static string JStr(JObject obj, string key)
        {
            return obj?[key]?.Value<string>();
        }

        // ── Step 1: Create a Plan. Amount is the MONTHLY RATE ONLY — deposit
        // and first month are charged separately via InitializeTransaction,
        // never routed through the plan itself. ──────────────────────────────
        public async Task<CreatePlanResult> CreatePlan(string name, double monthlyAmountRand, string interval = "monthly")
        {
            try
            {
                using var client = Client();
                var payload = new
                {
                    name,
                    amount = (int)(monthlyAmountRand * 100),
                    interval,
                    currency = "ZAR"
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{Base}/plan", content);
                var json = await response.Content.ReadAsStringAsync();
                var result = JObject.Parse(json);

                bool success = JBool(result, "status");
                string message = JStr(result, "message") ?? "Unknown error";
                string planCode = success ? result["data"]?["plan_code"]?.Value<string>() : null;

                return new CreatePlanResult { Success = success, Message = message, PlanCode = planCode };
            }
            catch (Exception ex)
            {
                return new CreatePlanResult { Success = false, Message = "Exception: " + ex.Message };
            }
        }

        // ── Step 2: Charge deposit + first month as a ONE-TIME transaction.
        // Deliberately does NOT pass `plan` here — Paystack overrides the
        // amount with the plan's amount if you do, which would charge the
        // wrong figure. A successful card payment leaves a reusable
        // authorization on the customer, used in step 4. ─────────────────────
        public async Task<InitializeTransactionResult> InitializeTransaction(string email, double amountRand, string reference)
        {
            try
            {
                using var client = Client();
                var payload = new
                {
                    email,
                    amount = (int)(amountRand * 100),
                    reference,
                    currency = "ZAR"
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{Base}/transaction/initialize", content);
                var json = await response.Content.ReadAsStringAsync();
                var result = JObject.Parse(json);

                bool success = JBool(result, "status");
                string message = JStr(result, "message") ?? "Unknown error";
                string authorizationUrl = null;
                string accessCode = null;

                if (success)
                {
                    authorizationUrl = result["data"]?["authorization_url"]?.Value<string>();
                    accessCode = result["data"]?["access_code"]?.Value<string>();
                }

                return new InitializeTransactionResult
                {
                    Success = success,
                    Message = message,
                    AuthorizationUrl = authorizationUrl,
                    AccessCode = accessCode,
                    Reference = reference
                };
            }
            catch (Exception ex)
            {
                return new InitializeTransactionResult { Success = false, Message = "Exception: " + ex.Message };
            }
        }

        // ── Step 3: After the customer completes checkout, verify the charge
        // and pull out the reusable authorization + customer codes. ──────────
        public async Task<VerifyTransactionResult> VerifyTransaction(string reference)
        {
            try
            {
                using var client = Client();
                var response = await client.GetAsync($"{Base}/transaction/verify/{reference}");
                var json = await response.Content.ReadAsStringAsync();
                var result = JObject.Parse(json);

                bool success = JBool(result, "status");
                string message = JStr(result, "message") ?? "Unknown error";
                var data = result["data"] as JObject;

                bool paid = success && data != null && JStr(data, "status") == "success";

                return new VerifyTransactionResult
                {
                    Success = paid,
                    Message = message,
                    AuthorizationCode = data?["authorization"]?["authorization_code"]?.Value<string>(),
                    Reusable = data?["authorization"]?["reusable"]?.Value<bool>() ?? false,
                    CustomerCode = data?["customer"]?["customer_code"]?.Value<string>(),
                    CustomerEmail = data?["customer"]?["email"]?.Value<string>()
                };
            }
            catch (Exception ex)
            {
                return new VerifyTransactionResult { Success = false, Message = "Exception: " + ex.Message };
            }
        }

        // ── Step 4: Attach the recurring subscription to the authorization
        // captured in step 3. `startDate` must be the date of the SECOND
        // charge (one billing cycle after the deposit+first-month payment) —
        // this is what stops Paystack immediately double-billing month one. ──
        public async Task<CreateSubscriptionResult> CreateSubscription(
            string customerCode, string planCode, string authorizationCode, DateTime startDate)
        {
            try
            {
                using var client = Client();
                var payload = new
                {
                    customer = customerCode,
                    plan = planCode,
                    authorization = authorizationCode,
                    start_date = startDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{Base}/subscription", content);
                var json = await response.Content.ReadAsStringAsync();
                var result = JObject.Parse(json);

                bool success = JBool(result, "status");
                string message = JStr(result, "message") ?? "Unknown error";
                var data = result["data"] as JObject;

                return new CreateSubscriptionResult
                {
                    Success = success,
                    Message = message,
                    SubscriptionCode = data?["subscription_code"]?.Value<string>(),
                    EmailToken = data?["email_token"]?.Value<string>() // required later to disable/enable
                };
            }
            catch (Exception ex)
            {
                return new CreateSubscriptionResult { Success = false, Message = "Exception: " + ex.Message };
            }
        }

        // ── Early lease termination / cancellation. Paystack requires BOTH
        // the subscription code and the email_token from CreateSubscription —
        // store both on LeaseAgreement, not just the subscription code. ──────
        public async Task<bool> DisableSubscription(string subscriptionCode, string emailToken)
        {
            try
            {
                using var client = Client();
                var payload = new { code = subscriptionCode, token = emailToken };

                var content = new StringContent(
                    JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{Base}/subscription/disable", content);
                var json = await response.Content.ReadAsStringAsync();
                var result = JObject.Parse(json);

                return JBool(result, "status");
            }
            catch
            {
                return false;
            }
        }
    }

    public class CreatePlanResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string PlanCode { get; set; }
    }

    public class InitializeTransactionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string AuthorizationUrl { get; set; }
        public string AccessCode { get; set; }
        public string Reference { get; set; }
    }

    public class VerifyTransactionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string AuthorizationCode { get; set; }
        public bool Reusable { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerEmail { get; set; }
    }

    public class CreateSubscriptionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string SubscriptionCode { get; set; }
        public string EmailToken { get; set; }
    }
}
