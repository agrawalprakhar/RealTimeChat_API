using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealTimeChat.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddstatusMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AlterColumn<string>(
           name: "StatusMessage",
           table: "User",
           nullable: true, // Set to true to make the column nullable
           oldNullable: false); // Set to false if the column was previously non-nullable


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
         
        }
    }
}
