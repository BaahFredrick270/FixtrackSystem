using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixtrack.Migrations
{
    /// <inheritdoc />
    public partial class NotificationsAndLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationLogs_RepairJobs_RepairJobId",
                table: "NotificationLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NotificationLogs",
                table: "NotificationLogs");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "NotificationLogs");

            migrationBuilder.DropColumn(
                name: "ReceptionistId",
                table: "NotificationLogs");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "NotificationLogs");

            migrationBuilder.RenameTable(
                name: "NotificationLogs",
                newName: "NotificationLog");

            migrationBuilder.RenameColumn(
                name: "ContactedAt",
                table: "NotificationLog",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_NotificationLogs_RepairJobId",
                table: "NotificationLog",
                newName: "IX_NotificationLog_RepairJobId");

            migrationBuilder.AddColumn<string>(
                name: "Message",
                table: "NotificationLog",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WasSuccessful",
                table: "NotificationLog",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_NotificationLog",
                table: "NotificationLog",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    RepairJobId = table.Column<int>(type: "int", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_RepairJobs_RepairJobId",
                        column: x => x.RepairJobId,
                        principalTable: "RepairJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RepairJobId",
                table: "Notifications",
                column: "RepairJobId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationLog_RepairJobs_RepairJobId",
                table: "NotificationLog",
                column: "RepairJobId",
                principalTable: "RepairJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationLog_RepairJobs_RepairJobId",
                table: "NotificationLog");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NotificationLog",
                table: "NotificationLog");

            migrationBuilder.DropColumn(
                name: "Message",
                table: "NotificationLog");

            migrationBuilder.DropColumn(
                name: "WasSuccessful",
                table: "NotificationLog");

            migrationBuilder.RenameTable(
                name: "NotificationLog",
                newName: "NotificationLogs");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "NotificationLogs",
                newName: "ContactedAt");

            migrationBuilder.RenameIndex(
                name: "IX_NotificationLog_RepairJobId",
                table: "NotificationLogs",
                newName: "IX_NotificationLogs_RepairJobId");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "NotificationLogs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceptionistId",
                table: "NotificationLogs",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "NotificationLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_NotificationLogs",
                table: "NotificationLogs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationLogs_RepairJobs_RepairJobId",
                table: "NotificationLogs",
                column: "RepairJobId",
                principalTable: "RepairJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
