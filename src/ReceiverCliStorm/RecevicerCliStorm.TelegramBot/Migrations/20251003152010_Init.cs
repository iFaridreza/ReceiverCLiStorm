using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReceiverCliStorm.TelegramBot.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionInfo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApiId = table.Column<string>(type: "TEXT", nullable: false),
                    ApiHash = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UseProxy = table.Column<bool>(type: "INTEGER", nullable: false),
                    UseChangeBio = table.Column<bool>(type: "INTEGER", nullable: false),
                    UseCheckReport = table.Column<bool>(type: "INTEGER", nullable: false),
                    UseLogCLI = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sudo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChatId = table.Column<long>(type: "INTEGER", maxLength: 50, nullable: false),
                    Language = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "En")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sudo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChatId = table.Column<long>(type: "INTEGER", maxLength: 50, nullable: false),
                    IsPermissionToUse = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Language = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "En")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserStep",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChatId = table.Column<long>(type: "INTEGER", maxLength: 50, nullable: false),
                    Step = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ExpierDateTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStep", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Session",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CountryCode = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    Number = table.Column<string>(type: "TEXT", nullable: false),
                    ESessionStatus = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "Exists"),
                    RegisterDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    SessionInfoId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Session", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Session_SessionInfo_SessionInfoId",
                        column: x => x.SessionInfoId,
                        principalTable: "SessionInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Session_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Session_CountryCode_Number",
                table: "Session",
                columns: new[] { "CountryCode", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Session_SessionInfoId",
                table: "Session",
                column: "SessionInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_Session_UserId",
                table: "Session",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Sudo_ChatId",
                table: "Sudo",
                column: "ChatId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_ChatId",
                table: "User",
                column: "ChatId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserStep_ChatId",
                table: "UserStep",
                column: "ChatId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Session");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Sudo");

            migrationBuilder.DropTable(
                name: "UserStep");

            migrationBuilder.DropTable(
                name: "SessionInfo");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
