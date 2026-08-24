using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoVeritas.OffersService.Migrations.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CarOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Variant = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DgtLabel = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PowerCv = table.Column<int>(type: "integer", nullable: false),
                    CashPriceEur = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    FinancedPriceEur = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    ReliabilityScore = table.Column<int>(type: "integer", nullable: true),
                    ReliabilityText = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    BootLiters = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PriceConfidence = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SourceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SourceUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastVerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OfferValidUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SourcePublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarOffers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancingOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Provider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TinPercent = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    TaePercent = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    RepaymentStructure = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TermDescription = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DownPaymentDescription = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FeesDescription = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MonthlyInstallment60Eur = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    TotalInterest60Eur = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    BestFor = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RateConfidence = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SourceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SourceUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastVerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OfferValidUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SourcePublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancingOffers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarOffers_Slug",
                table: "CarOffers",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancingOffers_Slug",
                table: "FinancingOffers",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarOffers");

            migrationBuilder.DropTable(
                name: "FinancingOffers");
        }
    }
}
