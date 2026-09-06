// ReSharper disable All - Justification: Example File

using Microsoft.EntityFrameworkCore.Migrations;

namespace Example.EntityFrameworkCore.MigrationPlacement.Application.Migrations;

// ARCH015: a Migration subclass belongs in Persistence/Migrations.
public sealed class PreviewPizzaOrdersMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) { }

    protected override void Down(MigrationBuilder migrationBuilder) { }
}
