using Microsoft.EntityFrameworkCore;
using RoomBooking.Api.Data;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<RoomBooking.Api.Repositories.Booking.IBookingRepository, RoomBooking.Api.Repositories.Booking.BookingRepository>();
builder.Services.AddScoped<RoomBooking.Api.Services.Booking.IBookingService, RoomBooking.Api.Services.Booking.BookingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
