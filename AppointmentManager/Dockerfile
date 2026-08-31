# בונה ומריץ את AppointmentManager.Api בקונטיינר יחיד (שימוש: Render Web Service).
# ה-build context הוא תיקיית AppointmentManager (מכילה את ה-.sln וכל תתי-הפרויקטים).

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY AppointmentManager.sln .
COPY AppointmentManager.Domain/AppointmentManager.Domain.csproj AppointmentManager.Domain/
COPY AppointmentManager.Application/AppointmentManager.Application.csproj AppointmentManager.Application/
COPY AppointmentManager.Infrastructure/AppointmentManager.Infrastructure.csproj AppointmentManager.Infrastructure/
COPY AppointmentManager.Api/AppointmentManager.Api.csproj AppointmentManager.Api/
COPY AppointmentManager.Tests/AppointmentManager.Tests.csproj AppointmentManager.Tests/
RUN dotnet restore AppointmentManager.sln

COPY . .
RUN dotnet publish AppointmentManager.Api -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render מזריק את משתנה הסביבה PORT בזמן ריצה (לא בזמן build) - קושרים את Kestrel אליו כאן.
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet AppointmentManager.Api.dll"]
