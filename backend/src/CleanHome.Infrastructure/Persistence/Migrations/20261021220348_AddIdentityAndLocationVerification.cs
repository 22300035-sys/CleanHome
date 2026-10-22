using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanHome.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityAndLocationVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_locations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    exterior_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    interior_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    neighborhood = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    postal_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    verification_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_locations", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_locations_users_customer_user_id",
                        column: x => x.customer_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "private_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    storage_key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    byte_length = table.Column<long>(type: "bigint", nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    security_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    moderation_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    review_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_private_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_private_documents_customer_locations_customer_location_id",
                        column: x => x.customer_location_id,
                        principalTable: "customer_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_private_documents_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_private_documents_users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "document_access_audits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    support_incident_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accessed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_access_audits", x => x.id);
                    table.ForeignKey(
                        name: "FK_document_access_audits_private_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "private_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_document_access_audits_support_incidents_support_incident_id",
                        column: x => x.support_incident_id,
                        principalTable: "support_incidents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_document_access_audits_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_locations_customer_user_id",
                table: "customer_locations",
                column: "customer_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_locations_customer_user_id_verification_status",
                table: "customer_locations",
                columns: new[] { "customer_user_id", "verification_status" });

            migrationBuilder.CreateIndex(
                name: "IX_document_access_audits_actor_user_id_accessed_at",
                table: "document_access_audits",
                columns: new[] { "actor_user_id", "accessed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_document_access_audits_document_id_accessed_at",
                table: "document_access_audits",
                columns: new[] { "document_id", "accessed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_document_access_audits_support_incident_id",
                table: "document_access_audits",
                column: "support_incident_id");

            migrationBuilder.CreateIndex(
                name: "IX_private_documents_customer_location_id",
                table: "private_documents",
                column: "customer_location_id");

            migrationBuilder.CreateIndex(
                name: "IX_private_documents_owner_user_id_document_type",
                table: "private_documents",
                columns: new[] { "owner_user_id", "document_type" });

            migrationBuilder.CreateIndex(
                name: "IX_private_documents_reviewed_by_user_id",
                table: "private_documents",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_private_documents_storage_key",
                table: "private_documents",
                column: "storage_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_access_audits");

            migrationBuilder.DropTable(
                name: "private_documents");

            migrationBuilder.DropTable(
                name: "customer_locations");
        }
    }
}
