using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AptekaParsing.Migrations
{
    public partial class changes4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductInStores_Stores_StoreId",
                table: "ProductInStores");

            migrationBuilder.DropIndex(
                name: "IX_ProductInStores_StoreId",
                table: "ProductInStores");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ProductInStores_StoreId",
                table: "ProductInStores",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductInStores_Stores_StoreId",
                table: "ProductInStores",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "StoreId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
