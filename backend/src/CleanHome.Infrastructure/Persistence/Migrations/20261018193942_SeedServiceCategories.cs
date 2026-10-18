using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanHome.Infrastructure.Persistence.Migrations
{
    public partial class SeedServiceCategories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "service_categories",
                columns: new[]
                {
                    "id",
                    "name",
                    "slug",
                    "description",
                    "is_active",
                    "created_at",
                    "updated_at"
                },
                values: new object[,]
                {
                    {
                        new Guid("a1111111-1111-4111-8111-111111111111"),
                        "Limpieza general",
                        "limpieza-general",
                        "Limpieza habitual de viviendas, incluyendo pisos, superficies, cocina y baños.",
                        true,
                        new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc),
                        new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc)
                    },
                    {
                        new Guid("a2222222-2222-4222-8222-222222222222"),
                        "Limpieza profunda",
                        "limpieza-profunda",
                        "Limpieza detallada de viviendas, incluyendo áreas que requieren mayor tiempo y atención.",
                        true,
                        new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc),
                        new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc)
                    },
                    {
                        new Guid("a3333333-3333-4333-8333-333333333333"),
                        "Limpieza de oficinas pequeñas",
                        "limpieza-oficinas",
                        "Limpieza ordinaria de oficinas pequeñas y espacios de trabajo, excluyendo actividades industriales o de alto riesgo.",
                        true,
                        new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc),
                        new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc)
                    }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "service_categories",
                keyColumn: "id",
                keyValue: new Guid("a1111111-1111-4111-8111-111111111111"));

            migrationBuilder.DeleteData(
                table: "service_categories",
                keyColumn: "id",
                keyValue: new Guid("a2222222-2222-4222-8222-222222222222"));

            migrationBuilder.DeleteData(
                table: "service_categories",
                keyColumn: "id",
                keyValue: new Guid("a3333333-3333-4333-8333-333333333333"));
        }
    }
}
