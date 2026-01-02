using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExamRequestsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExamRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfessionalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NomeExame = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CodigoExame = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Categoria = table.Column<int>(type: "INTEGER", nullable: false),
                    Prioridade = table.Column<int>(type: "INTEGER", nullable: false),
                    DataEmissao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DataLimite = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IndicacaoClinica = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    HipoteseDiagnostica = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Cid = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Observacoes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    InstrucoesPreparo = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    DigitalSignature = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: true),
                    CertificateThumbprint = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CertificateSubject = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DocumentHash = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SignedPdfBase64 = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamRequests_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamRequests_Users_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamRequests_Users_ProfessionalId",
                        column: x => x.ProfessionalId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamRequests_AppointmentId",
                table: "ExamRequests",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamRequests_DocumentHash",
                table: "ExamRequests",
                column: "DocumentHash");

            migrationBuilder.CreateIndex(
                name: "IX_ExamRequests_PatientId",
                table: "ExamRequests",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamRequests_ProfessionalId",
                table: "ExamRequests",
                column: "ProfessionalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamRequests");
        }
    }
}
