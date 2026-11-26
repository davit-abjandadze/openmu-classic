using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MUnique.OpenMU.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddCastleSiegeRewardLevelAndCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "ItemLevel",
                schema: "data",
                table: "CastleSiegePendingReward",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<int>(
                name: "RewardItemCount",
                schema: "config",
                table: "CastleSiegeConfiguration",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte>(
                name: "RewardItemLevel",
                schema: "config",
                table: "CastleSiegeConfiguration",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemLevel",
                schema: "data",
                table: "CastleSiegePendingReward");

            migrationBuilder.DropColumn(
                name: "RewardItemCount",
                schema: "config",
                table: "CastleSiegeConfiguration");

            migrationBuilder.DropColumn(
                name: "RewardItemLevel",
                schema: "config",
                table: "CastleSiegeConfiguration");
        }
    }
}
