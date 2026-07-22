// =====================================
// קובץ: AppointmentServiceTests.cs
// שכבה: Tests → Services
// תפקיד: בדיקות יחידה (Unit Tests) לשירות ניהול התורים (AppointmentService).
//         בדיקות אלה מוודאות שהלוגיקה העסקית המרכזית פועלת נכון:
//         - קביעת תורים (BookAppointmentAsync): תאריך עבר, מרווח לא תקין, חריץ תפוס,
//           חריץ פנוי, משך מוגזם.
//         - ביטול תורים (CancelAppointmentAsync): לא נמצא, הרשאה שגויה,
//           ביטול מאוחר מדי, ביטול בזמן, ביטול על ידי מנהל/ת.
//         - היסטוריית תורים (GetUserHistoryAsync): תורים קיימים, רשימה ריקה.
// =====================================

using AppointmentManager.Application.DTOs;
using AppointmentManager.Application.Interfaces;
using AppointmentManager.Application.Services;
using AppointmentManager.Domain;
using AppointmentManager.Domain.Common;
using AppointmentManager.Domain.Entities;
using AppointmentManager.Domain.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;

namespace AppointmentManager.Tests.Services;

/// <summary>
/// מחלקת בדיקות ל-AppointmentService.
/// משתמשת ב-Mocks לכל התלויות כדי לבדוק כל תרחיש בבידוד מוחלט.
/// </summary>
public class AppointmentServiceTests
{
    // ─── Mocks (חיקויים של תלויות) ───────────────────────────────────────────

    // _uowMock = Mock של IUnitOfWork - מחקה את כל גישת בסיס הנתונים
    private readonly Mock<IUnitOfWork> _uowMock = new();

    // _availMock = Mock של IAvailabilityService - מחקה בדיקת זמינות חריצים
    private readonly Mock<IAvailabilityService> _availMock = new();

    // _mapperMock = Mock של IMapper - מחקה המרת DTOs לישויות ולהפך
    private readonly Mock<IMapper> _mapperMock = new();

    // _emailMock = Mock של IEmailService - מחקה שליחת אימיילים (לא שולחים בבדיקות)
    private readonly Mock<IEmailService> _emailMock = new();

    // _appRepoMock = Mock של IAppointmentRepository - מחקה שאילתות תורים
    private readonly Mock<IAppointmentRepository> _appRepoMock = new();

    // _usersMock = Mock של IUserRepository - מחקה שאילתות משתמשים
    private readonly Mock<IUserRepository> _usersMock = new();

    // _settingsMock = Mock של ISettingsRepository - מחקה הגדרות מערכת
    private readonly Mock<ISettingsRepository> _settingsMock = new();

    // _svc = השירות הנבדק (System Under Test)
    private readonly AppointmentService _svc;

    // ─── הגדרות מערכת לבדיקות ────────────────────────────────────────────────

    /// <summary>
    /// הגדרות מערכת ברירת מחדל לשימוש בבדיקות.
    /// static = ערך מחושב פעם אחת עבור כל הבדיקות (לא נוצר מחדש לכל בדיקה).
    /// readonly = לא ניתן לשינוי לאחר האתחול.
    /// </summary>
    private static readonly SystemSettings DefaultSettings = new()
    {
        BufferTime = 5,               // 5 דקות בין תורים
        MinGapSize = 15,              // חור מינימלי 15 דקות
        CancellationDeadlineHours = 24, // חייבים לבטל לפחות 24 שעות מראש
        MorningMaxDuration = 120,     // בוקר: עד 120 דקות (2 שעות)
        EveningMaxDuration = 40,      // ערב: עד 40 דקות
        EveningStartTime = new TimeSpan(16, 0, 0) // ערב מ-16:00
    };

    /// <summary>
    /// קונסטרקטור - מופעל לפני כל בדיקה.
    /// מגדיר את ה-Mocks ויוצר מופע של AppointmentService.
    /// </summary>
    public AppointmentServiceTests()
    {
        // חיבור Mock של ה-Repositories ל-UnitOfWork Mock
        // כאשר קוד קורא ל-uow.Appointments - יוחזר _appRepoMock
        _uowMock.Setup(u => u.Appointments).Returns(_appRepoMock.Object);

        // כאשר קוד קורא ל-uow.Users - יוחזר _usersMock
        _uowMock.Setup(u => u.Users).Returns(_usersMock.Object);

        // כאשר קוד קורא ל-uow.Settings - יוחזר _settingsMock
        _uowMock.Setup(u => u.Settings).Returns(_settingsMock.Object);

        // כאשר קוד מבקש הגדרות - יוחזרו DefaultSettings (ללא DB אמיתי)
        _settingsMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(DefaultSettings);

        // שליחת אימייל - מדומה כהצלחה (Task.CompletedTask = פעולה שהסתיימה ריקה)
        // It.IsAny<string>() = קבל כל מחרוזת שתישלח (לא אכפת לנו מה נשלח)
        _emailMock.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // יצירת AppointmentService עם כל ה-Mocks
        // new Mock<ILogger<AppointmentService>>().Object = לוגר שלא עושה כלום
        _svc = new AppointmentService(
            _availMock.Object,
            _mapperMock.Object,
            _uowMock.Object,
            _emailMock.Object,
            new Mock<ILogger<AppointmentService>>().Object);
    }

    // ─── בדיקות BookAppointmentAsync ─────────────────────────────────────────

    /// <summary>
    /// בדיקה: קביעת תור בתאריך עבר חייבת להיכשל עם שגיאת PastDate.
    /// </summary>
    [Fact]
    public async Task BookAppointmentAsync_PastDate_ReturnsPastDateError()
    {
        // === Arrange ===
        var app = new Appointment
        {
            StartTime = DateTime.UtcNow.AddHours(-1), // שעה לפני עכשיו = בעבר
            DurationMinutes = 30,
            ClientId = Guid.NewGuid() // מזהה ייחודי אקראי ללקוח
        };

        // === Act ===
        var result = await _svc.BookAppointmentAsync(app);

        // === Assert ===
        Assert.False(result.IsSuccess);                        // חייב להיכשל
        Assert.Equal("Appointment.PastDate", result.Error.Code); // קוד שגיאה מדויק
    }

    /// <summary>
    /// בדיקה: קביעת תור בשעה שאינה כפולת 5 דקות חייבת להיכשל עם שגיאת InvalidInterval.
    /// לדוגמה: 10:03 אינה תקינה - רק 10:00, 10:05, 10:10 וכד'.
    /// </summary>
    [Fact]
    public async Task BookAppointmentAsync_InvalidInterval_ReturnsValidationError()
    {
        // === Arrange ===
        var futureDate = DateTime.UtcNow.AddDays(1); // מחר - בעתיד

        var app = new Appointment
        {
            // 10:03 = לא כפולת 5 → שגיאת תיקוף
            // new DateTime(year, month, day, hour, minute, second, kind) = בנייה מדויקת
            StartTime = new DateTime(futureDate.Year, futureDate.Month, futureDate.Day, 10, 3, 0, DateTimeKind.Utc),
            DurationMinutes = 30,
            ClientId = Guid.NewGuid()
        };

        // === Act ===
        var result = await _svc.BookAppointmentAsync(app);

        // === Assert ===
        Assert.False(result.IsSuccess);
        Assert.Equal("Appointment.InvalidInterval", result.Error.Code);
    }

    /// <summary>
    /// בדיקה: קביעת תור בחריץ תפוס (IsSlotAvailableAsync מחזירה כישלון) - צריכה להיכשל עם NoSlot.
    /// </summary>
    [Fact]
    public async Task BookAppointmentAsync_SlotNotAvailable_ReturnsConflict()
    {
        // === Arrange ===
        var futureDate = DateTime.UtcNow.AddDays(1);
        var app = new Appointment
        {
            StartTime = new DateTime(futureDate.Year, futureDate.Month, futureDate.Day, 10, 0, 0, DateTimeKind.Utc),
            DurationMinutes = 30,
            ClientId = Guid.NewGuid()
        };

        // הגדרת Mocks לפעולות עסקה (Transaction) - נדרשים כי השירות פותח עסקה לפני הבדיקה
        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<System.Data.IsolationLevel>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask); // ה-Rollback יקרא לאחר כישלון

        // IsSlotAvailableAsync מחזירה כישלון = החריץ תפוס
        _availMock.Setup(a => a.IsSlotAvailableAsync(app.StartTime, app.DurationMinutes))
            .ReturnsAsync(Result.Failure(AppointmentErrors.NoSlotFound)); // NoSlotFound = אין חריץ פנוי

        // === Act ===
        var result = await _svc.BookAppointmentAsync(app);

        // === Assert ===
        Assert.False(result.IsSuccess);
        Assert.Equal("Appointment.NoSlot", result.Error.Code); // קוד השגיאה שמחזיר IsSlotAvailableAsync
    }

    /// <summary>
    /// בדיקה: קביעת תור תקינה - תאריך עתידי, כפולת 5, חריץ פנוי - חייבת להצליח ולשמור בבסיס הנתונים.
    /// </summary>
    [Fact]
    public async Task BookAppointmentAsync_ValidSlot_ReturnsSuccess()
    {
        // === Arrange ===
        var futureDate = DateTime.UtcNow.AddDays(1);
        var clientId = Guid.NewGuid();
        var app = new Appointment
        {
            StartTime = new DateTime(futureDate.Year, futureDate.Month, futureDate.Day, 10, 0, 0, DateTimeKind.Utc),
            DurationMinutes = 30,
            ClientId = clientId
        };

        // Mocks לפעולות עסקה (BeginTransaction → SaveChanges → Commit)
        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<System.Data.IsolationLevel>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask); // אישור העסקה

        // IsSlotAvailableAsync מחזירה הצלחה = החריץ פנוי
        _availMock.Setup(a => a.IsSlotAvailableAsync(app.StartTime, app.DurationMinutes))
            .ReturnsAsync(Result.Success());

        // הוספת התור לבסיס הנתונים - לא עושה כלום (Mock)
        _appRepoMock.Setup(r => r.AddAsync(app)).Returns(Task.CompletedTask);

        // החזרת ManagedClient לבדיקת שליחת אימייל
        // ManagedClient = לא שולחים לו אימייל (אין לו אימייל אמיתי)
        _usersMock.Setup(r => r.GetByIdAsync(clientId))
            .ReturnsAsync(new User { Id = clientId, Role = UserRole.ManagedClient });

        // === Act ===
        var result = await _svc.BookAppointmentAsync(app);

        // === Assert ===
        Assert.True(result.IsSuccess); // חייב להצליח

        // Times.Once = מאמת שה-Mock הופעל בדיוק פעם אחת
        // בלעדי זה הבדיקה תעבור גם אם לא נשמר שום דבר!
        _appRepoMock.Verify(r => r.AddAsync(app), Times.Once);
    }

    /// <summary>
    /// בדיקה: קביעת תור עם משך העולה על המקסימום לשעה חייבת להיכשל עם שגיאת TooLong.
    /// 10:00 = בוקר → מקסימום 120 דקות. 150 דקות > 120 → שגיאה.
    /// </summary>
    [Fact]
    public async Task BookAppointmentAsync_TooLongDuration_ReturnsTooLongError()
    {
        // === Arrange ===
        var futureDate = DateTime.UtcNow.AddDays(1);
        var app = new Appointment
        {
            // 10:00 = שעות בוקר → מקסימום 120 דקות (לפי DefaultSettings)
            StartTime = new DateTime(futureDate.Year, futureDate.Month, futureDate.Day, 10, 0, 0, DateTimeKind.Utc),
            DurationMinutes = 150, // 150 > 120 → מוגזם מדי לשעות הבוקר
            ClientId = Guid.NewGuid()
        };

        // Mocks לפעולות עסקה (נדרשות כי הבדיקה קורית *לאחר* פתיחת עסקה)
        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<System.Data.IsolationLevel>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);

        // === Act ===
        var result = await _svc.BookAppointmentAsync(app);

        // === Assert ===
        Assert.False(result.IsSuccess);
        Assert.Equal("Appointment.TooLong", result.Error.Code);
    }

    // ─── בדיקות CancelAppointmentAsync ───────────────────────────────────────

    /// <summary>
    /// בדיקה: ביטול תור שאינו קיים חייב להחזיר שגיאת NotFound.
    /// </summary>
    [Fact]
    public async Task CancelAppointmentAsync_AppointmentNotFound_ReturnsNotFound()
    {
        // === Arrange ===
        // GetByIdAsync מחזיר null = לא נמצא
        _appRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Appointment?)null);

        // === Act ===
        // Guid.NewGuid() = מזהה אקראי לתור שלא קיים
        var result = await _svc.CancelAppointmentAsync(Guid.NewGuid(), Guid.NewGuid(), UserRole.Client);

        // === Assert ===
        Assert.False(result.IsSuccess);
        Assert.Equal("Appointment.NotFound", result.Error.Code);
    }

    /// <summary>
    /// בדיקה: לקוח שמנסה לבטל תור של לקוח אחר צריך לקבל שגיאת Unauthorized.
    /// (ClientId של התור ≠ userId של המבטל)
    /// </summary>
    [Fact]
    public async Task CancelAppointmentAsync_ClientCancelsOtherClient_ReturnsUnauthorized()
    {
        // === Arrange ===
        var appointmentId = Guid.NewGuid();
        var ownerId = Guid.NewGuid(); // בעל התור

        // התור שייך ל-ownerId
        _appRepoMock.Setup(r => r.GetByIdAsync(appointmentId))
            .ReturnsAsync(new Appointment
            {
                Id = appointmentId,
                ClientId = ownerId,          // בעל התור
                StartTime = DateTime.UtcNow.AddDays(2), // תאריך עתידי
                DurationMinutes = 30
            });

        // === Act ===
        // מנסה לבטל עם Guid.NewGuid() = משתמש אחר (לא הבעלים)
        var result = await _svc.CancelAppointmentAsync(appointmentId, Guid.NewGuid(), UserRole.Client);

        // === Assert ===
        Assert.False(result.IsSuccess);
        Assert.Equal("Auth.Unauthorized", result.Error.Code); // לא מורשה
    }

    /// <summary>
    /// בדיקה: לקוח שמנסה לבטל תור בתוך 24 שעות (מתחת לסף הביטול) יקבל שגיאת CannotCancel.
    /// DefaultSettings.CancellationDeadlineHours = 24. תור בעוד 10 שעות = מאוחר מדי.
    /// </summary>
    [Fact]
    public async Task CancelAppointmentAsync_ClientCancelsTooLate_ReturnsCannotCancel()
    {
        // === Arrange ===
        var appointmentId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // התור בעוד 10 שעות - פחות מ-24 שעות מראש
        _appRepoMock.Setup(r => r.GetByIdAsync(appointmentId))
            .ReturnsAsync(new Appointment
            {
                Id = appointmentId,
                ClientId = userId,
                StartTime = DateTime.UtcNow.AddHours(10), // רק 10 שעות → מאוחר מדי
                DurationMinutes = 30
            });

        // === Act ===
        var result = await _svc.CancelAppointmentAsync(appointmentId, userId, UserRole.Client);

        // === Assert ===
        Assert.False(result.IsSuccess);
        Assert.Equal("Appointment.CannotCancel", result.Error.Code); // ביטול מאוחר מדי
    }

    /// <summary>
    /// בדיקה: לקוח שמבטל תור שלו בזמן (מעל 24 שעות לפני) - הביטול צריך להצליח
    /// ולשנות את הסטטוס ל-Cancelled.
    /// </summary>
    [Fact]
    public async Task CancelAppointmentAsync_ClientCancelsInTime_SetsStatusCancelled()
    {
        // === Arrange ===
        var appointmentId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // יצירת אובייקט תור עם סטטוס Scheduled
        var app = new Appointment
        {
            Id = appointmentId,
            ClientId = userId,
            StartTime = DateTime.UtcNow.AddDays(3), // 3 ימים = מעל 24 שעות → בזמן
            DurationMinutes = 30,
            Status = AppointmentStatus.Scheduled // מצב ראשוני: מתוכנן
        };

        // Mocks לשמירה בבסיס הנתונים
        _appRepoMock.Setup(r => r.GetByIdAsync(appointmentId)).ReturnsAsync(app);
        _appRepoMock.Setup(r => r.UpdateAsync(app)).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        // החזרת ManagedClient (לא שולחים אימייל ביטול ל-ManagedClient)
        _usersMock.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(new User { Id = userId, Role = UserRole.ManagedClient });

        // === Act ===
        var result = await _svc.CancelAppointmentAsync(appointmentId, userId, UserRole.Client);

        // === Assert ===
        Assert.True(result.IsSuccess);
        // בדיקת תוצאה ישירה: הסטטוס *של האובייקט* שונה ל-Cancelled
        // זו בדיקה "שחורה" - בודקים את המצב הסופי, לא רק את קוד ההחזר
        Assert.Equal(AppointmentStatus.Cancelled, app.Status);
    }

    /// <summary>
    /// בדיקה: מנהל/ת יכול/ה לבטל תור גם בתוך חלון הביטול (2 שעות לפני).
    /// Admin אינו מוגבל על ידי מדיניות הביטול של הלקוח.
    /// </summary>
    [Fact]
    public async Task CancelAppointmentAsync_AdminCancelsWithinDeadline_ReturnsSuccess()
    {
        // === Arrange ===
        var appointmentId = Guid.NewGuid();
        var ownerId = Guid.NewGuid(); // בעל התור (לקוח רגיל)

        var app = new Appointment
        {
            Id = appointmentId,
            ClientId = ownerId,
            StartTime = DateTime.UtcNow.AddHours(2), // 2 שעות = בתוך חלון הביטול (< 24 שעות)
            DurationMinutes = 30,
            Status = AppointmentStatus.Scheduled
        };

        // Mocks לשמירה
        _appRepoMock.Setup(r => r.GetByIdAsync(appointmentId)).ReturnsAsync(app);
        _appRepoMock.Setup(r => r.UpdateAsync(app)).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);
        _usersMock.Setup(r => r.GetByIdAsync(ownerId))
            .ReturnsAsync(new User { Id = ownerId, Role = UserRole.ManagedClient });

        // === Act ===
        // Admin מבטל: userId מועבר כ-Guid.NewGuid() (לא חשוב - Admin עוקף בדיקת בעלות)
        var result = await _svc.CancelAppointmentAsync(appointmentId, Guid.NewGuid(), UserRole.Admin);

        // === Assert ===
        Assert.True(result.IsSuccess);
        Assert.Equal(AppointmentStatus.Cancelled, app.Status);
    }

    // ─── בדיקות GetUserHistoryAsync ───────────────────────────────────────────

    /// <summary>
    /// בדיקה: בקשת היסטוריית תורים של משתמש עם תורים קיימים מחזירה את הרשימה הנכונה.
    /// </summary>
    [Fact]
    public async Task GetUserHistoryAsync_ReturnsUserAppointments()
    {
        // === Arrange ===
        var userId = Guid.NewGuid();

        // יצירת רשימת תורים עם תור אחד
        var appointments = new List<Appointment>
        {
            new() { ClientId = userId, StartTime = DateTime.UtcNow.AddDays(-1), DurationMinutes = 30 }
        };

        // expectedResponse = ה-DTO שה-Mapper "יחזיר"
        var expectedResponse = new List<AppointmentResponse>();

        // הגדרת Mocks
        _appRepoMock.Setup(r => r.GetByClientIdAsync(userId)).ReturnsAsync(appointments);

        // AutoMapper Mock: כאשר ממירים appointments לרשימה של AppointmentResponse - החזר expectedResponse
        _mapperMock.Setup(m => m.Map<List<AppointmentResponse>>(appointments)).Returns(expectedResponse);

        // === Act ===
        var result = await _svc.GetUserHistoryAsync(userId);

        // === Assert ===
        Assert.True(result.IsSuccess);

        // Assert.Same = בדיקת זהות אובייקטים (אותו reference, לא רק ערכים שווים)
        // כך אנו מאמתים שה-Mapper אכן שימש ולא נוצר אובייקט אחר
        Assert.Same(expectedResponse, result.Value);
    }

    /// <summary>
    /// בדיקה: בקשת היסטוריית תורים של משתמש ללא תורים מחזירה רשימה ריקה (לא שגיאה).
    /// </summary>
    [Fact]
    public async Task GetUserHistoryAsync_EmptyHistory_ReturnsEmptyList()
    {
        // === Arrange ===
        var userId = Guid.NewGuid();
        var empty = new List<Appointment>(); // רשימה ריקה

        // Mocks
        _appRepoMock.Setup(r => r.GetByClientIdAsync(userId)).ReturnsAsync(empty);
        _mapperMock.Setup(m => m.Map<List<AppointmentResponse>>(empty)).Returns(new List<AppointmentResponse>());

        // === Act ===
        var result = await _svc.GetUserHistoryAsync(userId);

        // === Assert ===
        Assert.True(result.IsSuccess); // הצלחה (לא שגיאה) גם כשאין תורים
        Assert.Empty(result.Value);    // הרשימה ריקה
    }
}
