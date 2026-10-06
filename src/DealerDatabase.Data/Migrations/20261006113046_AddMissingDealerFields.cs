using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DealerDatabase.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingDealerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DirectorInfo",
                table: "DealerSources",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinanceCalculatorDetails",
                table: "DealerSources",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastValidationDate",
                table: "DealerSources",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceEmailAddress",
                table: "DealerSources",
                type: "TEXT",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SourceRating",
                table: "DealerSources",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockFigure",
                table: "DealerSources",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleTypeInfo",
                table: "DealerSources",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailAddress",
                table: "Dealers",
                type: "TEXT",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FcaStatus",
                table: "Dealers",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IcoExpiryDate",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SafExpiryDate",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VatValidationStatus",
                table: "Dealers",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DealerSources_ImportedDate",
                table: "DealerSources",
                column: "ImportedDate");

            migrationBuilder.CreateIndex(
                name: "IX_DealerSources_SourceName",
                table: "DealerSources",
                column: "SourceName");

            migrationBuilder.CreateIndex(
                name: "IX_Dealers_EmailAddress",
                table: "Dealers",
                column: "EmailAddress");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DealerSources_ImportedDate",
                table: "DealerSources");

            migrationBuilder.DropIndex(
                name: "IX_DealerSources_SourceName",
                table: "DealerSources");

            migrationBuilder.DropIndex(
                name: "IX_Dealers_EmailAddress",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "DirectorInfo",
                table: "DealerSources");

            migrationBuilder.DropColumn(
                name: "FinanceCalculatorDetails",
                table: "DealerSources");

            migrationBuilder.DropColumn(
                name: "LastValidationDate",
                table: "DealerSources");

            migrationBuilder.DropColumn(
                name: "SourceEmailAddress",
                table: "DealerSources");

            migrationBuilder.DropColumn(
                name: "SourceRating",
                table: "DealerSources");

            migrationBuilder.DropColumn(
                name: "StockFigure",
                table: "DealerSources");

            migrationBuilder.DropColumn(
                name: "VehicleTypeInfo",
                table: "DealerSources");

            migrationBuilder.DropColumn(
                name: "EmailAddress",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "FcaStatus",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "IcoExpiryDate",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "SafExpiryDate",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "VatValidationStatus",
                table: "Dealers");
        }
    }
}
