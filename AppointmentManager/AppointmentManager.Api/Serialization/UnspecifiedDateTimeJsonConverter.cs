// =====================================
// קובץ: UnspecifiedDateTimeJsonConverter.cs
// שכבה: API → Serialization
// תפקיד: כותב/קורא DateTime ב-JSON בלי סיומת "Z" (UTC) או Offset.
//         המערכת מתייחסת לכל ה-DateTime-ים כ"שעון קיר" גולמי (למשל 23:00 = 23:00 בישראל),
//         ולא כזמן UTC אמיתי - ראה AsUtc() ב-DateTimeHelpers.
//         Npgsql מחזיר תמיד Kind=Utc על עמודות "timestamp with time zone", ולכן ברירת
//         המחדל של System.Text.Json הוסיפה "Z" לתגובות ה-API. ה-Frontend קורא ISO strings
//         עם "Z" כזמן UTC אמיתי וממיר אותם לשעון המקומי של הדפדפן (למשל getHours()) - מה
//         שהזיז כל שעה מוצגת/נשלחת ב-3 שעות (הפרש ה-UTC של ישראל בקיץ).
//         הממיר הזה מבטל את סיומת ה-Z ומחזיר את ההתנהגות שהייתה לפני המעבר מ-SQL Server
//         (שם Kind לא נאכף ולכן לא נוספה סיומת).
// =====================================

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppointmentManager.Api.Serialization;

public class UnspecifiedDateTimeJsonConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss.fffffff";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = DateTime.Parse(reader.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault);
        // מבטיח Kind=Unspecified גם אם המחרוזת הנכנסת כוללת "Z"/Offset בטעות
        return DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // כתיבה בלי "Z"/Offset - ה-Kind בפועל (Utc/Unspecified) לא משפיע על הפלט
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }
}
