using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoVeritas.OffersService.Migrations.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CarOfferHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CarOfferId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChangedByEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarOfferHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarOfferHistories_CarOffers_CarOfferId",
                        column: x => x.CarOfferId,
                        principalTable: "CarOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinancingOfferHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FinancingOfferId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChangedByEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancingOfferHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancingOfferHistories_FinancingOffers_FinancingOfferId",
                        column: x => x.FinancingOfferId,
                        principalTable: "FinancingOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarOfferHistories_CarOfferId_RecordedAt",
                table: "CarOfferHistories",
                columns: new[] { "CarOfferId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancingOfferHistories_FinancingOfferId_RecordedAt",
                table: "FinancingOfferHistories",
                columns: new[] { "FinancingOfferId", "RecordedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarOfferHistories");

            migrationBuilder.DropTable(
                name: "FinancingOfferHistories");
        }
    }
}
