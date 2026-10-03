
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SkyTech.JobPostServices;
using SkyTech.Models;
using SkyTech.Models.Interviews;
using SkyTech.Models.JobPostServices;
using SkyTech.Models.Repair_Item_Services;
using SkyTech.Models.SkytechWorkers;
using SkyTech.Services;
using System.Globalization;
using System.Text;



var builder = WebApplication.CreateBuilder(args);

var defaultCulture = new CultureInfo("en-ZA");
CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

// Force dot as decimal separator for model binding
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
// MVC
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
// SQL
//this is a way of pulling the connection string in json file
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");  //this "DefaultConnection" must match with the one in json file connection string

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.CommandTimeout(120)));


//Hangfire Services here Buddies (Do not touch nizosha)
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(Hangfire.CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
builder.Services.AddScoped<PayStackTransferService>();



// I moved this from the bottom to up heereeeeeeeeeeeeeee
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


// Your Services class (email + DI)

builder.Services.AddScoped<LeaseBillingService>();
builder.Services.AddScoped<ILeaseBillingNotifier>(sp => sp.GetRequiredService<EmailServices>()); // wire this up once the interface is implemented on your real email service
builder.Services.AddScoped<LeaseReturnService>();
builder.Services.AddHttpClient<ILeaseDamageAssessmentService, OpenRouterLeaseDamageAssessmentService>();

builder.Services.AddScoped<EmailServices>();
builder.Services.AddHttpClient<IAIJobPostDraftService, OpenRouterJobPostDraftService>();
builder.Services.AddHttpClient<ICvExtractionService, OpenRouterCvExtractionService>();
builder.Services.AddScoped<PayStackService>();
builder.Services.AddScoped<IRosterSchedulingService, RosterSchedulingService>();
builder.Services.AddScoped<RecruitmentService>();
builder.Services.AddHttpClient<ISkillMatchService, OpenRouterSkillMatchService>();
builder.Services.AddScoped<JitsiService>();
builder.Services.AddHttpClient<ICollateralImageAnalysisService, OpenRouterCollateralImageAnalysisService>();
builder.Services.AddScoped<ICollateralVerificationService, OpenRouterCollateralVerificationService>();
builder.Services.AddScoped<IFraudDetectionService, FraudDetectionService>();
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();


builder.Services.AddScoped<WorkingDayCalculator>();
builder.Services.AddScoped<LeaveRequestService>();
// ── Face verification (check-in/out) ────────────────────────────────────
// Singleton: FaceDetector/FaceEmbedder load ONNX models in their
// constructors — expensive, so this should only happen once, not per request.
builder.Services.AddSingleton<SkyTech.Services.FaceRecognitionService>();

// ── Mobile attendance (check-in/out) — JWT auth for the Android app ─────────
// Separate from the web portal's cookie/session auth above; both coexist,
// selected per-controller via [Authorize(AuthenticationSchemes = "Bearer")].
builder.Services.AddScoped<JwtTokenService>();

var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSection["Key"]!))
        };
    });

var app = builder.Build();


app.UseHangfireDashboard("/hangfire");
// Visit /hangfire while the app is running to see job history — useful to
// screenshot/demo, and to manually "Trigger now" a run instead of waiting
// for the schedule, which is handy for testing without faking dates in SQL.


Hangfire.RecurringJob.AddOrUpdate<LeaseBillingService>(
    "lease-billing-daily",
    svc => svc.RunDailyBillingCheckAsync(),
    Hangfire.Cron.Minutely);
// Runs once a day. Change to Cron.Hourly or a custom cron string while
// testing if you don't want to wait a full day between runs.

Hangfire.RecurringJob.AddOrUpdate<LeaseReturnService>(
    "lease-return-reminder-daily",
    svc => svc.RunReturnReminderCheckAsync(),
    Hangfire.Cron.Minutely);


var build = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;


// Okay Team This here will repopulate the dropdown list for Technicians specializations and on repair requsts
using (var scope = app.Services.CreateScope())
{
    var DatabaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (!DatabaseContext.RepairItemCategories.Any())
    {
        DatabaseContext.RepairItemCategories.AddRange(
                new Repair_Item_Category { CategoryName = "Phones" },
                new Repair_Item_Category { CategoryName = "Laptops" },
                new Repair_Item_Category { CategoryName = "Consoles" }
            );

        DatabaseContext.SaveChanges();
    }

    // ── TEMPORARY: seed a Loan for testing Settle Loan (UC-05) ──
    // Remove once real Apply → Assess → Disburse data exists to test against.
    if (!DatabaseContext.Loans.Any())
    {
        var application = DatabaseContext.LoanApplications
            .FirstOrDefault(a => a.Status == LoanApplicationStatus.Disbursed);

        if (application != null)
        {
            var loan = new Loan
            {
                LoanApplicationId = application.LoanApplicationId,
                PrincipalAmount = application.RequestedAmount,
                InterestRate = 0.05m,
                TotalRepayable = application.RequestedAmount * 1.05m,
                OutstandingBalance = application.RequestedAmount * 1.05m,
                DisbursedOn = DateTime.UtcNow.AddDays(-10),
                DueDate = DateTime.UtcNow.AddMonths(6),
                Status = LoanRepaymentStatus.Active
            };
            DatabaseContext.Loans.Add(loan);
            DatabaseContext.SaveChanges();
        }
    }
}
// NOTE: no WorkplaceLocation seed block here anymore — geofencing is now
// per-Branch (Latitude/Longitude/RadiusMeters on Branch), so each branch's
// coordinates need to be set individually (via a Branch edit screen, or a
// direct DB update for now) rather than a single global seeded row.

var supportedCultures = new[] { new CultureInfo("en-ZA") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-ZA"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/ShoppingPage/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();       // ✅ serves wwwroot (CSS/JS)
app.UseRouting();
app.UseSession();           // ✅ must be BEFORE MapControllerRoute
app.UseAuthentication();    // ✅ added — must be BEFORE UseAuthorization, was missing entirely
app.UseAuthorization();

//Currency in RANDS
var cultureInfo = new System.Globalization.CultureInfo("en-ZA");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

//route config for core
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Customer}/{action=ShoppingPage}/{id?}"); // starts on the shopping page

app.Run();

