using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanHome.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "service_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    scheduled_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    estimated_duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    copied_from_request_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_requests_service_categories_service_category_id",
                        column: x => x.service_category_id,
                        principalTable: "service_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_service_requests_copied_from_request_id",
                        column: x => x.copied_from_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_users_customer_user_id",
                        column: x => x.customer_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_quotes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    description = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_quotes", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_quotes_service_requests_service_request_id",
                        column: x => x.service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_quotes_worker_profiles_worker_profile_id",
                        column: x => x.worker_profile_id,
                        principalTable: "worker_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_request_addresses",
                columns: table => new
                {
                    service_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    exterior_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    interior_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    neighborhood = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    postal_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_request_addresses", x => x.service_request_id);
                    table.ForeignKey(
                        name: "FK_service_request_addresses_service_requests_service_request_~",
                        column: x => x.service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_bookings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_quote_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agreed_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    scheduled_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    estimated_duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_bookings", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_bookings_service_quotes_service_quote_id",
                        column: x => x.service_quote_id,
                        principalTable: "service_quotes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_bookings_service_requests_service_request_id",
                        column: x => x.service_request_id,
                        principalTable: "service_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_bookings_users_customer_user_id",
                        column: x => x.customer_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_bookings_worker_profiles_worker_profile_id",
                        column: x => x.worker_profile_id,
                        principalTable: "worker_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "booking_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    new_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_status_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_booking_status_history_service_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "service_bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_booking_status_history_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_booking_status_history_actor_user_id",
                table: "booking_status_history",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_status_history_booking_id_created_at",
                table: "booking_status_history",
                columns: new[] { "booking_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_service_bookings_customer_user_id_created_at",
                table: "service_bookings",
                columns: new[] { "customer_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_service_bookings_service_quote_id",
                table: "service_bookings",
                column: "service_quote_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_bookings_service_request_id",
                table: "service_bookings",
                column: "service_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_bookings_worker_profile_id_scheduled_start",
                table: "service_bookings",
                columns: new[] { "worker_profile_id", "scheduled_start" });

            migrationBuilder.CreateIndex(
                name: "IX_service_quotes_service_request_id",
                table: "service_quotes",
                column: "service_request_id",
                unique: true,
                filter: "status = 'accepted'");

            migrationBuilder.CreateIndex(
                name: "IX_service_quotes_service_request_id_worker_profile_id",
                table: "service_quotes",
                columns: new[] { "service_request_id", "worker_profile_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_quotes_worker_profile_id_status",
                table: "service_quotes",
                columns: new[] { "worker_profile_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_copied_from_request_id",
                table: "service_requests",
                column: "copied_from_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_customer_user_id_created_at",
                table: "service_requests",
                columns: new[] { "customer_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_service_category_id",
                table: "service_requests",
                column: "service_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_status_city_scheduled_start",
                table: "service_requests",
                columns: new[] { "status", "city", "scheduled_start" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_status_history");

            migrationBuilder.DropTable(
                name: "service_request_addresses");

            migrationBuilder.DropTable(
                name: "service_bookings");

            migrationBuilder.DropTable(
                name: "service_quotes");

            migrationBuilder.DropTable(
                name: "service_requests");
        }
    }
}
