using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using RoomBooking.Api.Auth;
using RoomBooking.Api.Data;
using RoomBooking.Api.Repositories.Booking;
using RoomBooking.Api.Services.Booking;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Dev-only authentication: maps a bearer token to a seeded user (see Auth/DevAuthHandler).
builder.Services.AddAuthentication("DevAuth")
    .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>("DevAuth", _ => { });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUserService>();

// Enable the RFC 7807 problem-details contract for validation errors and unhandled exceptions.
builder.Services.AddProblemDetails();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IBookingService, BookingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
