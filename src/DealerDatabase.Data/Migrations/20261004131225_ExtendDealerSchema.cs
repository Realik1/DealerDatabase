using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DealerDatabase.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExtendDealerSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Dealers",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine2",
                table: "Dealers",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompaniesHouseNumber",
                table: "Dealers",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Dealers",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DissolutionDate",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FcaFirmRefNumber",
                table: "Dealers",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasConflicts",
                table: "Dealers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "IcoRegistrationNumber",
                table: "Dealers",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IncorporationDate",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedDate",
                table: "Dealers",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "LegalName",
                table: "Dealers",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Dealers",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Dealers",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Postcode",
                table: "Dealers",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SafMemberStatus",
                table: "Dealers",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VatNumber",
                table: "Dealers",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebsiteDomain",
                table: "Dealers",
                type: "TEXT",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DealerSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DealerId = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SourceId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    SourceName_Value = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    SourceAddress = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SourcePostcode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    SourcePhoneNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    MatchConfidence = table.Column<int>(type: "INTEGER", nullable: false),
                    MatchReason = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ImportedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceData = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DealerSources_Dealers_DealerId",
                        column: x => x.DealerId,
                        principalTable: "Dealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dealers_CompaniesHouseNumber",
                table: "Dealers",
                column: "CompaniesHouseNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Dealers_FcaFirmRefNumber",
                table: "Dealers",
                column: "FcaFirmRefNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Dealers_Name",
                table: "Dealers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Dealers_VatNumber",
                table: "Dealers",
                column: "VatNumber");

            migrationBuilder.CreateIndex(
                name: "IX_DealerSources_DealerId",
                table: "DealerSources",
                column: "DealerId");

            migrationBuilder.CreateIndex(
                name: "IX_DealerSources_SourceName_SourceId",
                table: "DealerSources",
                columns: new[] { "SourceName", "SourceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DealerSources");

            migrationBuilder.DropIndex(
                name: "IX_Dealers_CompaniesHouseNumber",
                table: "Dealers");

            migrationBuilder.DropIndex(
                name: "IX_Dealers_FcaFirmRefNumber",
                table: "Dealers");

            migrationBuilder.DropIndex(
                name: "IX_Dealers_Name",
                table: "Dealers");

            migrationBuilder.DropIndex(
                name: "IX_Dealers_VatNumber",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "AddressLine2",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "CompaniesHouseNumber",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "DissolutionDate",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "FcaFirmRefNumber",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "HasConflicts",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "IcoRegistrationNumber",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "IncorporationDate",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "LastModifiedDate",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "LegalName",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "Postcode",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "SafMemberStatus",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "VatNumber",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "WebsiteDomain",
                table: "Dealers");
        }
    }
}
