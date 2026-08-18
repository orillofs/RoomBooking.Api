using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoomBooking.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingConcurrencyAndOverlapConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            migrationBuilder.Sql(
                """
                ALTER TABLE "Bookings"
                ADD CONSTRAINT "EX_Bookings_RoomId_DateRange"
                EXCLUDE USING gist (
                    "RoomId" WITH =,
                    (tstzrange("StartDate", "EndDate", '[)')) WITH &&
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Bookings\" DROP CONSTRAINT \"EX_Bookings_RoomId_DateRange\";");
        }
    }
}
