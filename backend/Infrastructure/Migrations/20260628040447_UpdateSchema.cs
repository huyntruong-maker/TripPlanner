using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { new Guid("0f34236c-5a49-44ee-8e61-2992b0308ab9"), new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5") });

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("0f34236c-5a49-44ee-8e61-2992b0308ab9"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("de31cffb-9af8-41d0-b7d8-8fb1780f6560"));

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: new Guid("bb9f6603-1c9f-4933-9f66-031c9fb933a5"));
        }
    }
}
