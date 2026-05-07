using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vjezba.Model.Migrations
{
    /// <inheritdoc />
    public partial class ModelAnnotationsUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryPackage_Deliveries_DeliveryId",
                table: "DeliveryPackage");

            migrationBuilder.DropForeignKey(
                name: "FK_PackageWarehouse_Warehouses_WarehouseId",
                table: "PackageWarehouse");

            migrationBuilder.RenameColumn(
                name: "WarehouseId",
                table: "PackageWarehouse",
                newName: "WarehousesId");

            migrationBuilder.RenameIndex(
                name: "IX_PackageWarehouse_WarehouseId",
                table: "PackageWarehouse",
                newName: "IX_PackageWarehouse_WarehousesId");

            migrationBuilder.RenameColumn(
                name: "DeliveryId",
                table: "DeliveryPackage",
                newName: "DeliveriesId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryPackage_Deliveries_DeliveriesId",
                table: "DeliveryPackage",
                column: "DeliveriesId",
                principalTable: "Deliveries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PackageWarehouse_Warehouses_WarehousesId",
                table: "PackageWarehouse",
                column: "WarehousesId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryPackage_Deliveries_DeliveriesId",
                table: "DeliveryPackage");

            migrationBuilder.DropForeignKey(
                name: "FK_PackageWarehouse_Warehouses_WarehousesId",
                table: "PackageWarehouse");

            migrationBuilder.RenameColumn(
                name: "WarehousesId",
                table: "PackageWarehouse",
                newName: "WarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_PackageWarehouse_WarehousesId",
                table: "PackageWarehouse",
                newName: "IX_PackageWarehouse_WarehouseId");

            migrationBuilder.RenameColumn(
                name: "DeliveriesId",
                table: "DeliveryPackage",
                newName: "DeliveryId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryPackage_Deliveries_DeliveryId",
                table: "DeliveryPackage",
                column: "DeliveryId",
                principalTable: "Deliveries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PackageWarehouse_Warehouses_WarehouseId",
                table: "PackageWarehouse",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
