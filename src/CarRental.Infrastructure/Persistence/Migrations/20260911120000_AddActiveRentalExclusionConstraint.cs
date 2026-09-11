using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRental.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddActiveRentalExclusionConstraint : Migration
{
    private const string ActiveRentalPeriodConstraint = "EX_Rentals_ActiveCarDateRange";

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");
        migrationBuilder.Sql($"""
            ALTER TABLE "Rentals"
            ADD CONSTRAINT "{ActiveRentalPeriodConstraint}"
            EXCLUDE USING gist
            (
                "CarId" WITH =,
                daterange("StartDate", "EndDate", '[)') WITH &&
            )
            WHERE ("Status" = 'Active');
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"""
            ALTER TABLE "Rentals" DROP CONSTRAINT IF EXISTS "{ActiveRentalPeriodConstraint}";
            """);

        // btree_gist can predate this migration or be shared by other database objects, so it is intentionally retained.
    }
}
