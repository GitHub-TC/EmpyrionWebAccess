﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmpyrionModWebHost.Migrations
{
    /// <inheritdoc />
    public partial class PlayerFilesize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Filesize",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Filesize",
                table: "Players");
        }
    }
}
