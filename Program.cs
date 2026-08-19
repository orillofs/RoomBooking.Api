using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RoomBooking.Api.Authentication;
using RoomBooking.Api.Data;
using RoomBooking.Api.Models.Entities;
using RoomBooking.Api.Repositories.Booking;
using RoomBooking.Api.Services.Auth;
using RoomBooking.Api.Services.Booking;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Real authentication: JWT bearer tokens issued by /signin and /signup. The
// signing key comes from the "Jwt" configuration section (dev value in
// appsettings.json; override via user secrets or environment before going live).
var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>() ?? new JwtSettings();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

builder.Services.AddAuthentication("JwtBearer")
    .AddJwtBearer("JwtBearer", options =>
        options.TokenValidationParameters = TokenValidator.CreateParameters(jwtSettings))
    // Dev-only fallback: maps a bearer token to a seeded user (Authentication/DevAuthHandler).
    .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>("DevAuth", _ => { });

// The default policy accepts either scheme, so existing DevAuth tokens keep
// working during development while JWT becomes the real one.
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes("JwtBearer", "DevAuth")
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUserService>();

builder.Services.AddProblemDetails();

// Dev-only CORS: allow the Vite dev server (5173) to call the API during
// development. Tighten to specific origins before production.
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevClient", policy =>
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Token issuance and authentication
builder.Services.AddSingleton<TokenGenerator>();
builder.Services.AddScoped<TokenProvider>();
builder.Services.AddSingleton<TokenValidator>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IBookingService, BookingService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("DevClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();