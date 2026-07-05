using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRbacScaffold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("RoleId", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("RoleClaimId", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "CreatedAt", "CreatedBy", "DisplayName", "IsDeleted", "Level", "Name", "NormalizedName", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("0f34236c-5a49-44ee-8e61-2992b0308ab9"), "6AAFFB84-E49A-468D-9153-2DA282AC0CDA", new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5"), "Super Admin", false, 1024, "SuperAdmin", "SuperAdmin", new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5") },
                    { new Guid("de31cffb-9af8-41d0-b7d8-8fb1780f6560"), "6AAFFB84-E49A-468D-9153-2DA282AC0CDA", new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5"), "System Admin", false, 512, "SystemAdmin", "SystemAdmin", new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5") }
                });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "CreatedAt", "CreatedBy", "Email", "EmailConfirmed", "FirstName", "IsDeleted", "LastName", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "ResetPasswordExpiration", "ResetPasswordToken", "SecurityStamp", "TwoFactorEnabled", "UpdatedAt", "UpdatedBy", "UserName" },
                values: new object[] { new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5"), 0, "616f1653-48e9-4a6f-81b3-1bdd52e565b5", new DateTimeOffset(new DateTime(2023, 10, 9, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000000"), null, false, "Admin", false, null, true, null, null, "ADMIN", "AQAAAAEAACcQAAAAEELKNErj+EBVy3yZwAI32HSAQILEj5UAOooOEHTMPYU/yp0E28xNH1BjU/SEBw8kuA==", null, false, null, null, "ZY5BGSWBARTE74T6ZLO7WKKMMILBEB2E", false, new DateTimeOffset(new DateTime(2023, 10, 9, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000000"), "admin" });

            migrationBuilder.InsertData(
                table: "AspNetRoleClaims",
                columns: new[] { "Id", "ClaimType", "ClaimValue", "CreatedAt", "CreatedBy", "IsDeleted", "RoleId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, "RolePolicy", "ManageAssignRoles", new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5"), false, new Guid("0f34236c-5a49-44ee-8e61-2992b0308ab9"), new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5") },
                    { 2, "RolePolicy", "ViewUsers", new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5"), false, new Guid("0f34236c-5a49-44ee-8e61-2992b0308ab9"), new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5") },
                    { 3, "RolePolicy", "CreateUser", new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5"), false, new Guid("0f34236c-5a49-44ee-8e61-2992b0308ab9"), new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5") },
                    { 4, "RolePolicy", "UpdateUser", new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5"), false, new Guid("0f34236c-5a49-44ee-8e61-2992b0308ab9"), new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5") },
                    { 5, "RolePolicy", "DeactivateUser", new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5"), false, new Guid("0f34236c-5a49-44ee-8e61-2992b0308ab9"), new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5") },
                    { 6, "RolePolicy", "ResetPassUser", new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5"), false, new Guid("0f34236c-5a49-44ee-8e61-2992b0308ab9"), new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5") },
                    { 7, "RolePolicy", "ViewUsers", new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5"), false, new Guid("de31cffb-9af8-41d0-b7d8-8fb1780f6560"), new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5") },
                    { 8, "RolePolicy", "CreateUser", new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5"), false, new Guid("de31cffb-9af8-41d0-b7d8-8fb1780f6560"), new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5") },
                    { 9, "RolePolicy", "UpdateUser", new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5"), false, new Guid("de31cffb-9af8-41d0-b7d8-8fb1780f6560"), new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5") },
                    { 10, "RolePolicy", "DeactivateUser", new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5"), false, new Guid("de31cffb-9af8-41d0-b7d8-8fb1780f6560"), new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5") },
                    { 11, "RolePolicy", "ResetPassUser", new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5"), false, new Guid("de31cffb-9af8-41d0-b7d8-8fb1780f6560"), new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5") }
                });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId", "CreatedAt", "CreatedBy", "IsDeleted", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("0f34236c-5a49-44ee-8e61-2992b0308ab9"), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5"), new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5"), false, new DateTimeOffset(new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(8363), new TimeSpan(0, 0, 0, 0, 0)), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5") });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");
        }
    }
}
