// =====================================
// קובץ: AuthServiceTests.cs
// שכבה: Tests → Services
// תפקיד: בדיקות יחידה (Unit Tests) לשירות האימות (AuthService).
//         "בדיקת יחידה" = בדיקת פונקציה אחת בבידוד מוחלט.
//         Moq = ספריה ליצירת "מוקים" (Mocks) - חיקויים של תלויות.
//         במקום בסיס נתונים אמיתי, משתמשים ב-_uowMock שמחזיר ערכים מוגדרים מראש.
//         [Fact] = מסמן מתודה כבדיקה שה-test runner (xUnit) יריץ.
// =====================================

using AppointmentManager.Application.DTOs.Auth;
using AppointmentManager.Application.Services;
using AppointmentManager.Domain;
using AppointmentManager.Domain.Entities;
using AppointmentManager.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;
using BC = BCrypt.Net.BCrypt; // לצורך יצירת Hash בדיקות

namespace AppointmentManager.Tests.Services;

/// <summary>
/// מחלקת בדיקות ל-AuthService.
/// כל בדיקה מאמתת תרחיש אחד ספציפי.
/// </summary>
public class AuthServiceTests
{
    // _uowMock = Mock של IUnitOfWork - מחקה בסיס נתונים ללא SQL אמיתי
    private readonly Mock<IUnitOfWork> _uowMock = new();

    // _usersMock = Mock של IUserRepository
    private readonly Mock<IUserRepository> _usersMock = new();

    // _svc = השירות הנבדק (System Under Test)
    private readonly AuthService _svc;

    /// <summary>
    /// קונסטרקטור - מופעל לפני כל בדיקה.
    /// מגדיר את ה-Mocks ויוצר מופע של AuthService.
    /// </summary>
    public AuthServiceTests()
    {
        // הגדרה: כאשר קוראים ל-uow.Users - החזר את _usersMock
        _uowMock.Setup(u => u.Users).Returns(_usersMock.Object);

        // Mock של IConfiguration להגדרות JWT
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Jwt:Key"]).Returns("TestOnly_SuperSecretKey_AtLeast32Chars!"); // מפתח לבדיקות
        configMock.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        configMock.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

        // יצירת השירות הנבדק עם ה-Mocks
        _svc = new AuthService(_uowMock.Object, configMock.Object);
    }

    // ─── בדיקות RegisterAsync ─────────────────────────────────────────────────

    /// <summary>
    /// בדיקה: הרשמה עם אימייל חדש צריכה להצליח ולהחזיר Token.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_NewEmail_ReturnsSuccessWithToken()
    {
        // === Arrange (הכנת תנאי הבדיקה) ===
        // כשמחפשים את האימייל החדש - לא נמצא (null) = אימייל חופשי
        _usersMock.Setup(r => r.GetByEmailAsync("new@test.com")).ReturnsAsync((User?)null);
        // הוספת משתמש - לא עושה כלום (Task.CompletedTask)
        _usersMock.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        // שמירה - לא עושה כלום
        _uowMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        // === Act (ביצוע הפעולה) ===
        var result = await _svc.RegisterAsync(new RegisterRequest("Test User", "new@test.com", "0501234567", "password123"));

        // === Assert (אימות התוצאה) ===
        Assert.True(result.IsSuccess);               // חייב להצליח
        Assert.NotEmpty(result.Value.Token);          // Token לא יכול להיות ריק
        Assert.Equal("Test User", result.Value.FullName); // שם נשמר נכון
        Assert.Equal("Client", result.Value.Role);    // תפקיד ברירת מחדל = Client
    }

    /// <summary>
    /// בדיקה: הרשמה עם אימייל קיים צריכה להחזיר שגיאת Conflict.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_ExistingEmail_ReturnsConflict()
    {
        // כשמחפשים את האימייל - נמצא משתמש קיים
        var existingUser = new User { Email = "exists@test.com" };
        _usersMock.Setup(r => r.GetByEmailAsync("exists@test.com")).ReturnsAsync(existingUser);

        var result = await _svc.RegisterAsync(new RegisterRequest("Test", "exists@test.com", "0501234567", "password123"));

        Assert.False(result.IsSuccess); // חייב להיכשל
        Assert.Equal("Auth.UserAlreadyExists", result.Error.Code); // קוד שגיאה נכון
    }

    /// <summary>
    /// בדיקה: הרשמה עצמאית חייבת לשמור תפקיד Client (לא Admin).
    /// </summary>
    [Fact]
    public async Task RegisterAsync_SavesClientRole()
    {
        User? savedUser = null; // ישמור את המשתמש שנוסף

        _usersMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        // It.IsAny<User>() = כל User | Callback = קוד שרץ כשה-Mock מופעל
        _usersMock.Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => savedUser = u) // שמירת המשתמש שנשלח
            .Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _svc.RegisterAsync(new RegisterRequest("Admin Try", "admin@test.com", "0521234567", "password123"));

        // Assert: המשתמש שנשמר חייב להיות Client (לא Admin)
        Assert.NotNull(savedUser);
        Assert.Equal(UserRole.Client, savedUser!.Role); // "!" = מבטיח שאינו null
    }

    // ─── בדיקות LoginAsync ────────────────────────────────────────────────────

    /// <summary>
    /// בדיקה: התחברות עם פרטים נכונים מחזירה Token.
    /// </summary>
    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        // יצירת Hash אמיתי (BCrypt) כמו שהמערכת עושה בהרשמה
        var hash = BC.HashPassword("password123");
        var user = new User
        {
            FullName = "Test User",
            Email = "test@test.com",
            PasswordHash = hash, // Hash אמיתי
            Role = UserRole.Client
        };
        _usersMock.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);

        var result = await _svc.LoginAsync(new LoginRequest("test@test.com", "password123"));

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.Token); // Token חייב להיות ממולא
    }

    /// <summary>
    /// בדיקה: התחברות עם אימייל שאינו קיים מחזירה שגיאת InvalidCredentials.
    /// (לא מגלים שהאימייל לא קיים - מטעמי אבטחה)
    /// </summary>
    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsInvalidCredentials()
    {
        // כל חיפוש אימייל מחזיר null (לא נמצא)
        _usersMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var result = await _svc.LoginAsync(new LoginRequest("noone@test.com", "password123"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Auth.InvalidCredentials", result.Error.Code); // אותה שגיאה כמו סיסמה שגויה
    }

    /// <summary>
    /// בדיקה: התחברות עם סיסמה שגויה מחזירה שגיאת InvalidCredentials.
    /// </summary>
    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsInvalidCredentials()
    {
        // Hash של הסיסמה הנכונה
        var hash = BC.HashPassword("correct_password");
        var user = new User
        {
            FullName = "Test User",
            Email = "test@test.com",
            PasswordHash = hash,
            Role = UserRole.Client
        };
        _usersMock.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);

        // מתחבר עם סיסמה שגויה
        var result = await _svc.LoginAsync(new LoginRequest("test@test.com", "wrong_password"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Auth.InvalidCredentials", result.Error.Code);
    }
}
