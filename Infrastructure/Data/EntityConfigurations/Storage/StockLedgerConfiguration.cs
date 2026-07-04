using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations.Storage;

public class StockLedgerConfiguration : IEntityTypeConfiguration<StockLedger>
{
    public void Configure(EntityTypeBuilder<StockLedger> builder)
    {
        builder.ToTable(
            "stock_ledger",
            table => table.HasCheckConstraint("ck_stock_ledger_change_quantity", "change_quantity > 0"));
        builder.ConfigureBaseEntity();

        builder.Property(x => x.StockBatchId).HasColumnName("stock_batch_id");
        builder.Property(x => x.WareId).HasColumnName("ware_id");
        builder.Property(x => x.WareNameSnapshot).HasColumnName("ware_name_snapshot").HasMaxLength(150).IsRequired();
        builder.Property(x => x.GoodsId).HasColumnName("goods_id");
        builder.Property(x => x.GoodsNameSnapshot).HasColumnName("goods_name_snapshot").HasMaxLength(150).IsRequired();
        builder.Property(x => x.GoodsCodeSnapshot).HasColumnName("goods_code_snapshot").HasMaxLength(50).IsRequired();
        builder.Property(x => x.BatchNoSnapshot).HasColumnName("batch_no_snapshot").HasMaxLength(100).IsRequired();
        builder.Property(x => x.BaseUnitNameSnapshot).HasColumnName("base_unit_name_snapshot").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Direction).HasColumnName("direction").HasColumnType("integer");
        builder.Property(x => x.SourceType).HasColumnName("source_type").HasColumnType("integer");
        builder.Property(x => x.SourceOrderId).HasColumnName("source_order_id");
        builder.Property(x => x.SourceDetailId).HasColumnName("source_detail_id");
        builder.Property(x => x.ChangeQuantity).HasColumnName("change_quantity").HasColumnType("numeric(18,6)");
        builder.Property(x => x.BalanceQuantity).HasColumnName("balance_quantity").HasColumnType("numeric(18,6)");
        builder.Property(x => x.UnitCost).HasColumnName("unit_cost").HasColumnType("numeric(18,4)");
        builder.Property(x => x.TotalCost).HasColumnName("total_cost").HasColumnType("numeric(18,4)");
        builder.Property(x => x.OccurredTime).HasColumnName("occurred_time").HasColumnType("timestamp with time zone");
        builder.Property(x => x.ReversedFromLedgerId).HasColumnName("reversed_from_ledger_id");
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => new { x.StockBatchId, x.OccurredTime }).HasDatabaseName("idx_stock_ledger_batch_time");
        builder.HasIndex(x => new { x.SourceOrderId, x.SourceDetailId }).HasDatabaseName("idx_stock_ledger_source");
        builder.HasIndex(x => x.ReversedFromLedgerId)
            .IsUnique()
            .HasDatabaseName("idx_stock_ledger_reversed_from");

        builder.HasOne(x => x.StockBatch)
            .WithMany(x => x.Ledgers)
            .HasForeignKey(x => x.StockBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReversedFromLedger)
            .WithMany()
            .HasForeignKey(x => x.ReversedFromLedgerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
