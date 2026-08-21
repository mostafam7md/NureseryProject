using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NurseryLink.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RestructureAccountCtiModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Accounts_CreatedByAccountId",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_AdminPrivileges_Accounts_AdminAccountId",
                table: "AdminPrivileges");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Accounts_AdminAccountId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Accounts_TeacherAccountId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Accounts_ParentAccountId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_ParentStudents_Accounts_ParentAccountId",
                table: "ParentStudents");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Classes_ClassId",
                table: "Students");

            migrationBuilder.DropTable(
                name: "MealEntries");

            migrationBuilder.DropTable(
                name: "TemperatureReadings");

            migrationBuilder.DropTable(
                name: "ToiletVisits");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_IsRead",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_ParentAccountId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Classes_TeacherAccountId",
                table: "Classes");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_CreatedByAccountId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ParentAccountId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "TeacherAccountId",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "CreatedByAccountId",
                table: "Accounts");

            migrationBuilder.RenameColumn(
                name: "ClassId",
                table: "Students",
                newName: "CurrentClassId");

            migrationBuilder.RenameIndex(
                name: "IX_Students_ClassId",
                table: "Students",
                newName: "IX_Students_CurrentClassId");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "Accounts",
                newName: "AccountType");

            migrationBuilder.RenameIndex(
                name: "IX_Accounts_Role",
                table: "Accounts",
                newName: "IX_Accounts_AccountType");

            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoggedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LogType = table.Column<int>(type: "int", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoggedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_Accounts_LoggedByAccountId",
                        column: x => x.LoggedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Admins_Accounts_Id",
                        column: x => x.Id,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Admins_Admins_CreatedByAdminId",
                        column: x => x.CreatedByAdminId,
                        principalTable: "Admins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Parents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parents_Accounts_Id",
                        column: x => x.Id,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Teachers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teachers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teachers_Accounts_Id",
                        column: x => x.Id,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParentNotifications",
                columns: table => new
                {
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentNotifications", x => new { x.NotificationId, x.ParentAccountId });
                    table.ForeignKey(
                        name: "FK_ParentNotifications_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParentNotifications_Parents_ParentAccountId",
                        column: x => x.ParentAccountId,
                        principalTable: "Parents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClassTeachers",
                columns: table => new
                {
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassTeachers", x => new { x.ClassId, x.TeacherAccountId });
                    table.ForeignKey(
                        name: "FK_ClassTeachers_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassTeachers_Teachers_TeacherAccountId",
                        column: x => x.TeacherAccountId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Type",
                table: "Notifications",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_ClassId",
                table: "ActivityLogs",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_LoggedByAccountId",
                table: "ActivityLogs",
                column: "LoggedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_StudentId_LoggedAtUtc",
                table: "ActivityLogs",
                columns: new[] { "StudentId", "LoggedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Admins_CreatedByAdminId",
                table: "Admins",
                column: "CreatedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassTeachers_TeacherAccountId",
                table: "ClassTeachers",
                column: "TeacherAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentNotifications_IsRead",
                table: "ParentNotifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_ParentNotifications_ParentAccountId",
                table: "ParentNotifications",
                column: "ParentAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdminPrivileges_Admins_AdminAccountId",
                table: "AdminPrivileges",
                column: "AdminAccountId",
                principalTable: "Admins",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Admins_AdminAccountId",
                table: "AuditLogs",
                column: "AdminAccountId",
                principalTable: "Admins",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ParentStudents_Parents_ParentAccountId",
                table: "ParentStudents",
                column: "ParentAccountId",
                principalTable: "Parents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Classes_CurrentClassId",
                table: "Students",
                column: "CurrentClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdminPrivileges_Admins_AdminAccountId",
                table: "AdminPrivileges");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Admins_AdminAccountId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ParentStudents_Parents_ParentAccountId",
                table: "ParentStudents");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Classes_CurrentClassId",
                table: "Students");

            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropTable(
                name: "ClassTeachers");

            migrationBuilder.DropTable(
                name: "ParentNotifications");

            migrationBuilder.DropTable(
                name: "Teachers");

            migrationBuilder.DropTable(
                name: "Parents");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_Type",
                table: "Notifications");

            migrationBuilder.RenameColumn(
                name: "CurrentClassId",
                table: "Students",
                newName: "ClassId");

            migrationBuilder.RenameIndex(
                name: "IX_Students_CurrentClassId",
                table: "Students",
                newName: "IX_Students_ClassId");

            migrationBuilder.RenameColumn(
                name: "AccountType",
                table: "Accounts",
                newName: "Role");

            migrationBuilder.RenameIndex(
                name: "IX_Accounts_AccountType",
                table: "Accounts",
                newName: "IX_Accounts_Role");

            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "Notifications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentAccountId",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TeacherAccountId",
                table: "Classes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAccountId",
                table: "Accounts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MealEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EatingStatus = table.Column<int>(type: "int", nullable: false),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LoggedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MealType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealEntries_Accounts_TeacherAccountId",
                        column: x => x.TeacherAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MealEntries_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MealEntries_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemperatureReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LoggedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ValueCelsius = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemperatureReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemperatureReadings_Accounts_TeacherAccountId",
                        column: x => x.TeacherAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemperatureReadings_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemperatureReadings_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ToiletVisits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LoggedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VisitType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToiletVisits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToiletVisits_Accounts_TeacherAccountId",
                        column: x => x.TeacherAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ToiletVisits_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ToiletVisits_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IsRead",
                table: "Notifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ParentAccountId",
                table: "Notifications",
                column: "ParentAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_TeacherAccountId",
                table: "Classes",
                column: "TeacherAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_CreatedByAccountId",
                table: "Accounts",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MealEntries_ClassId",
                table: "MealEntries",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_MealEntries_StudentId_MealType_LocalDate",
                table: "MealEntries",
                columns: new[] { "StudentId", "MealType", "LocalDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MealEntries_TeacherAccountId",
                table: "MealEntries",
                column: "TeacherAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_TemperatureReadings_ClassId",
                table: "TemperatureReadings",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_TemperatureReadings_StudentId",
                table: "TemperatureReadings",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_TemperatureReadings_TeacherAccountId",
                table: "TemperatureReadings",
                column: "TeacherAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ToiletVisits_ClassId",
                table: "ToiletVisits",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_ToiletVisits_StudentId",
                table: "ToiletVisits",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_ToiletVisits_TeacherAccountId",
                table: "ToiletVisits",
                column: "TeacherAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Accounts_CreatedByAccountId",
                table: "Accounts",
                column: "CreatedByAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AdminPrivileges_Accounts_AdminAccountId",
                table: "AdminPrivileges",
                column: "AdminAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Accounts_AdminAccountId",
                table: "AuditLogs",
                column: "AdminAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Accounts_TeacherAccountId",
                table: "Classes",
                column: "TeacherAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Accounts_ParentAccountId",
                table: "Notifications",
                column: "ParentAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ParentStudents_Accounts_ParentAccountId",
                table: "ParentStudents",
                column: "ParentAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Classes_ClassId",
                table: "Students",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
