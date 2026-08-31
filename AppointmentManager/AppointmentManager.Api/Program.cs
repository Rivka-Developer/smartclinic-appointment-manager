// =====================================
// קובץ: Program.cs
// שכבה: API (נקודת הכניסה של כל האפליקציה)
// תפקיד: נקודת ההתחלה של השרת.
//         אחראי על:
//         1. הגדרת כל השירותים (Dependency Injection Container)
//         2. הגדרת ה-Middleware Pipeline (שרשרת העיבוד של בקשות HTTP)
//         3. Seeding: וידוא שהגדרות מערכת קיימות ב-DB
//         4. הגדרת משימות רקע (Hangfire)
// =====================================

using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using AppointmentManager.Api.Middleware;
using AppointmentManager.Application;
using AppointmentManager.Application.Interfaces;
using AppointmentManager.Application.Services;
using AppointmentManager.Domain.Entities;
using AppointmentManager.Domain.Interfaces;
using AppointmentManager.Infrastructure;
using AppointmentManager.Infrastructure.Repositories;
using AppointmentManager.Infrastructure.Services;
using Hangfire;                                        // לניהול משימות רקע
using Hangfire.PostgreSql;                              // אחסון Hangfire על PostgreSQL
using Microsoft.AspNetCore.Authentication.JwtBearer;  // לאימות JWT
using AutoMapper;
using Microsoft.EntityFrameworkCore;                  // לחיבור PostgreSQL
using Microsoft.IdentityModel.Tokens;                 // לאמצעי אבטחת JWT
using Microsoft.OpenApi.Models;                        // להגדרת Swagger
using Serilog;                                         // לרישום לוגים

// ===== הגדרת לוגר ראשוני לפני בניית האפליקציה =====
// Bootstrap Logger = לוגר זמני שפועל רק עד שהאפליקציה האמיתית מוכנה
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console() // כתיבה ל-Console
    .CreateBootstrapLogger();

// ===== יצירת ה-Builder (מכין את האפליקציה) =====
var builder = WebApplication.CreateBuilder(args);

// הגדרת Serilog כלוגר הראשי של האפליקציה
// ReadFrom.Configuration = קריאת הגדרות לוגים מ-appsettings.json
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()); // הוספת מידע הקשר לכל לוג

// ===== אימות מפתח ה-JWT =====
var jwtKey = builder.Configuration["Jwt:Key"];
// בדיקת תקינות המפתח: חייב להיות מוגדר ולפחות 32 תווים (דרישה של HMAC-SHA256)
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException(
        "Jwt:Key חייב להיות מוגדר ולפחות 32 תווים. " +
        "בסביבת פיתוח: הגדר ב-appsettings.Development.json. " +
        "בסביבת ייצור: הגדר כמשתנה סביבה בשם Jwt__Key (שתי קווים תחתונות).");

// ===== 1. הגדרת בסיס הנתונים =====
// AddDbContext = רישום ApplicationDbContext כ-Scoped Service
// UseNpgsql = שימוש ב-PostgreSQL (Connection String מה-appsettings)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== 2. רישום ה-Repositories =====
// AddScoped = מופע חדש לכל HTTP Request (מחיקה בסוף הבקשה)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IWorkShiftRepository, WorkShiftRepository>();
builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>(); // מנהל כל ה-Repositories יחד

// ===== 3. רישום שירותי ה-Application =====
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IWorkShiftService, WorkShiftService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IChatBotService, ChatBotService>();
builder.Services.AddScoped<ISwapOfferRepository, SwapOfferRepository>();
builder.Services.AddScoped<ISwapOfferService, SwapOfferService>();

// ===== 4. Memory Cache =====
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("Gemini", client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ===== 5. הגדרת Middleware גלובלי =====
// GlobalExceptionHandler = תפיסת שגיאות לא-מטופלות ברחבי האפליקציה
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(); // פורמט תגובת שגיאה סטנדרטי (RFC 7807)

// ===== 5. הגדרות כלליות =====
// AutoMapper: קריאת MappingProfile שמגדיר את כללי המיפוי
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

// Controllers: הגדרת JSON Serialization לכתיבת Enums כמחרוזות (לדוגמה: "Admin" במקום 0)
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer(); // נדרש ל-Swagger

// ===== 6. הגדרת Swagger עם תמיכה ב-JWT =====
// Swagger = ממשק ויזואלי לתיעוד ובדיקת ה-API (זמין ב-/swagger)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Appointment Manager API", Version = "v1" });

    // הוספת תמיכה בכניסת JWT Token ב-Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,          // ה-Token מגיע בכותרת HTTP
        Description = "הזן 'Bearer' ורווח ואז את ה-Token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>() // רשימת Scopes ריקה
        }
    });
});

// ===== 7. הגדרת CORS =====
// CORS = Cross-Origin Resource Sharing - אישור גישה מדומיינים אחרים (Frontend)
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
if (allowedOrigins.Length == 0 && !builder.Environment.IsDevelopment())
    throw new InvalidOperationException(
        "AllowedOrigins חייב להיות מוגדר בסביבת ייצור. " +
        "הגדר ב-appsettings.Production.json או כמשתנה סביבה.");

builder.Services.AddCors(options => {
    options.AddPolicy("AllowAngular", policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
        else
            // בפיתוח בלבד: localhost:4200
            policy.WithOrigins("http://localhost:4200").AllowAnyMethod().AllowAnyHeader().AllowCredentials();
    });
});

// ===== 8. הגדרת Authentication (אימות JWT) =====
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
        // קרא את ה-JWT מ-Cookie במקום מכותרת Authorization
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                ctx.Token = ctx.Request.Cookies["access_token"];
                return Task.CompletedTask;
            }
        };
    });

// Authorization = בדיקת הרשאות לפי Roles (Admin/Client)
builder.Services.AddAuthorization();

// ===== 9. Health Checks =====
// נקודת קצה /health שבודקת שהשרת ובסיס הנתונים תקינים
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database"); // בדיקת חיבור לבסיס נתונים

// ===== 10. Rate Limiting =====
// הגבלת קצב הבקשות - מגינה מפני התקפות Brute Force על Login/Register
builder.Services.AddRateLimiter(options =>
{
    // הגבלת קצב התחברות/הרשמה – מגינה מפני Brute Force
    options.AddFixedWindowLimiter("AuthLimiter", o =>
    {
        o.PermitLimit = 3;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 0;
    });

    // הגבלת קצב קביעת תורים – מגינה מפני spam קביעות
    options.AddFixedWindowLimiter("BookingLimiter", o =>
    {
        o.PermitLimit = 2;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 0;
    });

    // הגבלת קצב צ'אטבוט – מגינה מפני שימוש מוגזם ב-Gemini API
    options.AddFixedWindowLimiter("ChatLimiter", o =>
    {
        o.PermitLimit = 15;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 0;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ===== 11. הגדרת Hangfire (משימות רקע) =====
#pragma warning disable CS0618 // UsePostgreSqlStorage(connectionString) יוסר ב-2.0 לטובת עומס-יתר עם IConnectionFactory; ה-API הנוכחי עדיין תקין ונתמך
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection"))
    .UseFilter(new Hangfire.AutomaticRetryAttribute { Attempts = 3, DelaysInSeconds = [60, 300, 900] }));
#pragma warning restore CS0618

builder.Services.AddHangfireServer();

// ===== בניית האפליקציה =====
var app = builder.Build();

// ===== Seeding: יצירת נתונים ראשוניים אם אינם קיימים =====
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate(); // ביצוע מיגרציות DB שטרם בוצעו

    // אם אין הגדרות מערכת - צור עם ברירות מחדל
    if (!context.Settings.Any())
    {
        context.Settings.Add(new SystemSettings
        {
            BufferTime = 5,                              // 5 דקות בין תורים
            MinGapSize = 15,                             // חור מינימלי 15 דקות
            CancellationDeadlineHours = 24,              // ביטול עד 24 שעות לפני
            MorningMaxDuration = 120,                    // בוקר: עד 2 שעות
            EveningMaxDuration = 40,                     // ערב: עד 40 דקות
            EveningStartTime = new TimeSpan(16, 0, 0),  // ערב מתחיל ב-16:00
            AdminContactEmail = "newfaces.office@gmail.com",
            BusinessName = "SmartClinic"                 // שם עסק ברירת מחדל
        });
        context.SaveChanges();
    }
    else
    {
        var existing = context.Settings.First();
        if (string.IsNullOrWhiteSpace(existing.AdminContactEmail))
        {
            existing.AdminContactEmail = "newfaces.office@gmail.com";
            context.SaveChanges();
        }
    }
}

// ===== הגדרת ה-Middleware Pipeline =====
// הסדר חשוב! כל בקשה עוברת דרך ה-Middleware לפי הסדר שהוגדר.

app.UseExceptionHandler();      // הפעלת GlobalExceptionHandler ראשון (לתפוס כל שגיאה)

if (app.Environment.IsDevelopment()) // Swagger רק בסביבת פיתוח
{
    app.UseSwagger();
    app.UseSwaggerUI(); // ממשק ויזואלי ב-/swagger
}

app.UseSerilogRequestLogging(); // לוג לכל בקשת HTTP (שיטה, נתיב, Status Code, זמן)
app.UseRouting();               // הפעלת מנגנון ניתוב (routing)
app.UseCors("AllowAngular");    // הפעלת מדיניות CORS
app.UseRateLimiter();           // הפעלת Rate Limiting

app.UseHttpsRedirection();      // הפניה מ-HTTP ל-HTTPS

app.UseAuthentication();        // בדיקת JWT Token
app.UseAuthorization();         // בדיקת הרשאות (לאחר שהמשתמש זוהה)

app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
{
    Authorization = [new HangfireAuthorizationFilter()]
});

app.MapControllers();           // קישור נתיבי URL ל-Controllers
app.MapHealthChecks("/health"); // נתיב /health לבדיקת בריאות השרת


// ===== הגדרת משימות הרקע של Hangfire =====
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    // משימה 1: תזכורות תורים - פעם בשעה
    // Cron.Hourly = ביטוי Cron "0 * * * *" = ב-0 דקות של כל שעה
    recurringJobManager.AddOrUpdate<IBackgroundJobService>(
        "appointment-reminders",                     // שם המשימה (מזהה ייחודי)
        service => service.SendAppointmentRemindersAsync(),
        Cron.Hourly);                                // תדירות: כל שעה

    // משימה 2: דוח יומי - כל יום ב-20:00 לפי שעון ישראל
    // MisfireHandling.Ignorable: אם השרת היה מכובה בשעה 20:00, המשימה תדולג ולא תרוץ בהפעלה הבאה
    var israelTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");
    recurringJobManager.AddOrUpdate<IBackgroundJobService>(
        "daily-admin-report",
        service => service.SendDailyAdminReportAsync(),
        "0 20 * * *",
        new RecurringJobOptions
        {
            TimeZone = israelTimeZone,
            MisfireHandling = MisfireHandlingMode.Ignorable
        });
}

// ===== הפעלת השרת =====
app.Run();
