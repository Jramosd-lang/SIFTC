using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIFTC.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    IdUsuario = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_del_sistema = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    contrasena_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrimerNombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PrimerApellido = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    rol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.IdUsuario);
                });

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_usuario_del_sistema",
                table: "usuarios",
                column: "usuario_del_sistema",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "usuarios");
        }
    }
}
