// =====================================
// קובץ: EmailTemplates.cs
// שכבה: Application → Helpers
// תפקיד: תבניות HTML מעוצבות לאימיילים (RTL, עברית).
// =====================================

using System.Globalization;
using System.Text;

namespace AppointmentManager.Application.Helpers;

public static class EmailTemplates
{
    public record AppointmentReportRow(DateTime StartTime, DateTime EndTime, string ClientName);

    // ── המרת תאריך לעברי — מבוסס על HebrewCalendarService.ts ────────────────

    private static readonly HebrewCalendar _hc = new();

    // זהה לגישה ב-toHebrewLetters() ב-hebrew-calendar.service.ts
    private static readonly string[] _ones = ["", "א", "ב", "ג", "ד", "ה", "ו", "ז", "ח", "ט"];
    private static readonly string[] _tens = ["", "י", "כ", "ל"];

    private static string ToHebrewDay(int n)
    {
        if (n == 15) return "ט\"ו";
        if (n == 16) return "ט\"ז";
        if (n % 10 == 0) return _tens[n / 10] + "׳";
        if (n < 10)      return _ones[n] + "׳";
        return _tens[n / 10] + "\"" + _ones[n % 10];
    }

    private static string HebrewDate(DateTime date)
    {
        int year  = _hc.GetYear(date);
        int month = _hc.GetMonth(date);
        int day   = _hc.GetDayOfMonth(date);
        bool leap = _hc.IsLeapYear(year);

        string monthName = (month, leap) switch
        {
            (1,  _)     => "תשרי",
            (2,  _)     => "חשוון",
            (3,  _)     => "כסלו",
            (4,  _)     => "טבת",
            (5,  _)     => "שבט",
            (6,  false) => "אדר",
            (6,  true)  => "אדר א׳",
            (7,  false) => "ניסן",
            (7,  true)  => "אדר ב׳",
            (8,  false) => "אייר",
            (8,  true)  => "ניסן",
            (9,  false) => "סיוון",
            (9,  true)  => "אייר",
            (10, false) => "תמוז",
            (10, true)  => "סיוון",
            (11, false) => "אב",
            (11, true)  => "תמוז",
            (12, false) => "אלול",
            (12, true)  => "אב",
            (13, true)  => "אלול",
            _           => month.ToString()
        };

        return $"{ToHebrewDay(day)} ב{monthName}";
    }

    // תאריך עברי ראשי + לועזי קטן בצד
    private static string FormatDate(DateTime date) =>
        $"{HebrewDate(date)} <span style=\"font-size:12px;color:#9ca3af;font-weight:400;\">({date:dd/MM/yyyy})</span>";

    // ── תבנית עטיפה אחידה (header כחול כמו דף הבית) ───────────────────────────

    private static string Wrap(string title, string bodyContent, string businessName) => $"""
        <!DOCTYPE html>
        <html dir="rtl" lang="he">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width,initial-scale=1.0">
          <link href="https://fonts.googleapis.com/css2?family=Heebo:wght@400;500;600;700&display=swap" rel="stylesheet">
        </head>
        <body style="margin:0;padding:0;background-color:#f8fafc;font-family:'Heebo','Segoe UI',Tahoma,Arial,sans-serif;direction:rtl;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#f8fafc;">
            <tr><td align="center" style="padding:32px 16px;">
              <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);border:1px solid #e2e8f0;">

                <!-- Header – תמיד כחול ראשי -->
                <tr>
                  <td style="background:linear-gradient(135deg,#1e3a5f 0%,#2d5282 100%);padding:36px 40px;text-align:center;">
                    <h1 style="color:#ffffff;margin:0 0 4px;font-size:24px;font-weight:700;letter-spacing:0.3px;">SmartClinic</h1>
                    <p style="color:rgba(255,255,255,0.70);margin:0 0 20px;font-size:13px;">מערכת ניהול תורים חכמה</p>
                    <p style="color:#ffffff;font-size:18px;font-weight:600;margin:0;">{title}</p>
                  </td>
                </tr>

                <!-- Body -->
                <tr>
                  <td style="padding:36px 40px;">
                    {bodyContent}
                  </td>
                </tr>

                <!-- Footer -->
                <tr>
                  <td style="background:#f8fafc;padding:20px 40px;text-align:center;border-top:1px solid #e2e8f0;">
                    <p style="color:#6b7280;font-size:13px;margin:0 0 3px;">{businessName}</p>
                    <p style="color:#9ca3af;font-size:12px;margin:0;">הודעה זו נשלחה אוטומטית — אנא אל תשיב אליה</p>
                  </td>
                </tr>

              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;

    // ── שורת מידע בטבלת פרטים ──────────────────────────────────────────────

    private static string InfoRow(string label, string value) => $"""
        <tr>
          <td style="padding:11px 0;border-bottom:1px solid #e2e8f0;">
            <span style="color:#6b7280;font-size:12px;">{label}</span>
            <span style="display:block;color:#111827;font-size:16px;font-weight:600;margin-top:2px;">{value}</span>
          </td>
        </tr>
        """;

    // ── כרטיס פרטי תור אחיד ────────────────────────────────────────────────

    private static string InfoCard(string accentColor, string tableRows) => $"""
        <div style="background:#f8fafc;border:1px solid #e2e8f0;border-right:3px solid {accentColor};border-radius:10px;padding:20px 24px;margin-bottom:24px;">
          <table width="100%" cellpadding="0" cellspacing="0">
            {tableRows}
          </table>
        </div>
        """;

    // ── אישור קביעת תור ────────────────────────────────────────────────────

    public static string BookingConfirmation(string fullName, DateTime startTime, int durationMinutes, string businessName)
    {
        string content = $"""
            <p style="color:#111827;font-size:17px;margin:0 0 8px;">שלום <strong>{fullName}</strong>,</p>
            <p style="color:#374151;font-size:15px;margin:0 0 24px;">תורך <strong>נקבע בהצלחה</strong> — אנו מצפים לראותך!</p>

            {InfoCard("#10b981",
                InfoRow("תאריך", FormatDate(startTime)) +
                InfoRow("שעה", startTime.ToString("HH:mm")) +
                InfoRow("משך הטיפול", $"{durationMinutes} דקות")
            )}

            <p style="color:#6b7280;font-size:14px;margin:0;">לביטול או שינוי תור, אנא היכנס/י למערכת.</p>
            """;

        return Wrap("אישור קביעת תור", content, businessName);
    }

    // ── ביטול תור ──────────────────────────────────────────────────────────

    public static string BookingCancellation(string fullName, DateTime startTime, string businessName)
    {
        string content = $"""
            <p style="color:#111827;font-size:17px;margin:0 0 8px;">שלום <strong>{fullName}</strong>,</p>
            <p style="color:#374151;font-size:15px;margin:0 0 24px;">תורך <strong>בוטל</strong> — נשמח לראותך בפעם אחרת.</p>

            {InfoCard("#ef4444",
                InfoRow("תאריך", FormatDate(startTime)) +
                InfoRow("שעה", startTime.ToString("HH:mm"))
            )}

            <p style="color:#6b7280;font-size:14px;margin:0;">לקביעת תור חדש, אנא היכנס/י למערכת.</p>
            """;

        return Wrap("ביטול תור", content, businessName);
    }

    // ── תזכורת לתור ────────────────────────────────────────────────────────

    public static string AppointmentReminder(string fullName, DateTime startTime, string businessName)
    {
        string content = $"""
            <p style="color:#111827;font-size:17px;margin:0 0 8px;">שלום <strong>{fullName}</strong>,</p>
            <p style="color:#374151;font-size:15px;margin:0 0 24px;">תזכורת: יש לך <strong>תור מחר</strong> — אנו מצפים לראותך!</p>

            {InfoCard("#2d5282",
                InfoRow("תאריך", FormatDate(startTime)) +
                InfoRow("שעה", startTime.ToString("HH:mm"))
            )}

            <p style="color:#6b7280;font-size:14px;margin:0;">לביטול, אנא היכנס/י למערכת בהקדם האפשרי.</p>
            """;

        return Wrap("תזכורת לתור מחר", content, businessName);
    }

    // ── העברת תור (מציעה) ──────────────────────────────────────────────────────

    public static string SwapTransferredAway(string fullName, DateTime startTime, string businessName)
    {
        string content = $"""
            <p style="color:#111827;font-size:17px;margin:0 0 8px;">שלום <strong>{fullName}</strong>,</p>
            <p style="color:#374151;font-size:15px;margin:0 0 24px;">תורך <strong>הועבר בהצלחה</strong> ללקוחה אחרת — אינך צריכה להגיע.</p>

            {InfoCard("#f59e0b",
                InfoRow("תאריך", FormatDate(startTime)) +
                InfoRow("שעה", startTime.ToString("HH:mm"))
            )}

            <p style="color:#6b7280;font-size:14px;margin:0;">לקביעת תור חדש, אנא היכנסי למערכת.</p>
            """;

        return Wrap("העברת תור", content, businessName);
    }

    // ── דו"ח יומי למנהל ────────────────────────────────────────────────────

    public static string AdminDailyReport(DateTime tomorrow, List<AppointmentReportRow> rows, string businessName)
    {
        var tableRows = new StringBuilder();
        double totalMinutes = 0;

        foreach (var row in rows)
        {
            double duration = (row.EndTime - row.StartTime).TotalMinutes;
            totalMinutes += duration;
            tableRows.Append($"""
                <tr>
                  <td style="padding:11px 14px;border-bottom:1px solid #e2e8f0;font-weight:600;color:#111827;white-space:nowrap;">{row.StartTime:HH:mm}</td>
                  <td style="padding:11px 14px;border-bottom:1px solid #e2e8f0;color:#374151;">{row.ClientName}</td>
                  <td style="padding:11px 14px;border-bottom:1px solid #e2e8f0;color:#6b7280;text-align:left;white-space:nowrap;">{duration} דק'</td>
                </tr>
                """);
        }

        string emptyRow = rows.Count == 0
            ? """<tr><td colspan="3" style="padding:20px;text-align:center;color:#6b7280;">אין תורים מתוכננים למחר</td></tr>"""
            : string.Empty;

        var hours = Math.Floor(totalMinutes / 60);
        var minutes = totalMinutes % 60;
        string summaryRow = rows.Count > 0
            ? $"""
              <tr style="background:#eef2ff;">
                <td colspan="2" style="padding:11px 14px;font-weight:700;color:#1e3a5f;">סה"כ זמן טיפולים</td>
                <td style="padding:11px 14px;font-weight:700;color:#1e3a5f;text-align:left;">{hours}:{minutes:00} ש'</td>
              </tr>
              """
            : string.Empty;

        string content = $"""
            <p style="color:#111827;font-size:17px;margin:0 0 8px;">שלום,</p>
            <p style="color:#374151;font-size:15px;margin:0 0 24px;">להלן <strong>סיכום {rows.Count} התורים</strong> המתוכננים למחר, {FormatDate(tomorrow)}:</p>

            <div style="border:1px solid #e2e8f0;border-radius:10px;overflow:hidden;margin-bottom:16px;">
              <table width="100%" cellpadding="0" cellspacing="0">
                <thead>
                  <tr style="background:#f8fafc;">
                    <th style="padding:12px 14px;text-align:right;color:#6b7280;font-size:12px;font-weight:600;letter-spacing:0.5px;border-bottom:1px solid #e2e8f0;">שעה</th>
                    <th style="padding:12px 14px;text-align:right;color:#6b7280;font-size:12px;font-weight:600;border-bottom:1px solid #e2e8f0;">לקוח/ה</th>
                    <th style="padding:12px 14px;text-align:left;color:#6b7280;font-size:12px;font-weight:600;border-bottom:1px solid #e2e8f0;">משך</th>
                  </tr>
                </thead>
                <tbody>
                  {tableRows}{emptyRow}
                </tbody>
                {(rows.Count > 0 ? $"<tfoot>{summaryRow}</tfoot>" : "")}
              </table>
            </div>
            """;

        return Wrap($"דו\"ח תורים למחר — {HebrewDate(tomorrow)}", content, businessName);
    }
}
