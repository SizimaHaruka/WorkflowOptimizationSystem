using dxpmt.Domain;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<ImprovementCase> Cases => Set<ImprovementCase>();
    public DbSet<CaseForm> CaseForms => Set<CaseForm>();
    public DbSet<ActionItem> ActionItems => Set<ActionItem>();
    public DbSet<DecisionRecord> DecisionRecords => Set<DecisionRecord>();
    public DbSet<GateReview> GateReviews => Set<GateReview>();
    public DbSet<CaseStatusHistory> CaseStatusHistories => Set<CaseStatusHistory>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<Problem> Problems => Set<Problem>();
    public DbSet<ImprovementOption> ImprovementOptions => Set<ImprovementOption>();
    public DbSet<TraceLink> TraceLinks => Set<TraceLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ImprovementCase>(entity =>
        {
            entity.ToTable("Cases");
            entity.HasIndex(x => x.CaseNumber).IsUnique();
            entity.Property(x => x.CaseNumber).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.RequestingDepartment).HasMaxLength(100).IsRequired();
            entity.Property(x => x.RequesterName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.OwnerName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Priority).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.ScopeSummary).HasMaxLength(2000);
        });

        modelBuilder.Entity<CaseForm>(entity =>
        {
            entity.HasIndex(x => new { x.CaseId, x.FormType, x.Version }).IsUnique();
            entity.Property(x => x.FormType).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.ContentJson).IsRequired();
            entity.HasOne(x => x.Case).WithMany(x => x.Forms).HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ActionItem>(entity =>
        {
            entity.HasIndex(x => new { x.CaseId, x.Sequence }).IsUnique();
            entity.Property(x => x.Category).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Content).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.ContactOrResponseTarget).HasMaxLength(200);
            entity.Property(x => x.OwnerName).HasMaxLength(100);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Response).HasMaxLength(2000);
            entity.HasOne(x => x.Case).WithMany(x => x.ActionItems).HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DecisionRecord>(entity =>
        {
            entity.HasIndex(x => new { x.CaseId, x.Sequence }).IsUnique();
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.DeciderName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.CreatedBy).HasMaxLength(100);
            entity.HasOne(x => x.Case).WithMany(x => x.DecisionRecords).HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GateReview>(entity =>
        {
            entity.HasIndex(x => new { x.CaseId, x.Gate, x.CreatedAt });
            entity.Property(x => x.Gate).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Decision).HasMaxLength(20).IsRequired();
            entity.Property(x => x.ReviewerName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Comment).HasMaxLength(2000);
            entity.Property(x => x.ChecklistJson).IsRequired();
            entity.HasOne(x => x.Case).WithMany(x => x.GateReviews).HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CaseStatusHistory>(entity =>
        {
            entity.Property(x => x.PreviousStatus).HasMaxLength(30).IsRequired();
            entity.Property(x => x.NewStatus).HasMaxLength(30).IsRequired();
            entity.Property(x => x.ChangedBy).HasMaxLength(100);
            entity.Property(x => x.Comment).HasMaxLength(2000);
            entity.HasOne(x => x.Case).WithMany(x => x.StatusHistory).HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkItem>(entity =>
        {
            entity.HasIndex(x => new { x.CaseId, x.Sequence }).IsUnique();
            entity.Property(x => x.WorkType).HasMaxLength(10).IsRequired().HasDefaultValue(WorkItemTypes.AsIs);
            entity.Property(x => x.BusinessProcessName).HasMaxLength(200);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.DepartmentAndRole).HasMaxLength(200);
            entity.Property(x => x.Performer).HasMaxLength(100);
            entity.Property(x => x.Location).HasMaxLength(200);
            entity.Property(x => x.ContentJson).IsRequired();
            entity.HasOne(x => x.Case).WithMany(x => x.WorkItems).HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Problem>(entity =>
        {
            entity.HasIndex(x => new { x.CaseId, x.Sequence }).IsUnique();
            entity.Property(x => x.Category).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Phenomenon).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Severity).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.HasOne(x => x.Case).WithMany(x => x.Problems).HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.WorkItem).WithMany(x => x.Problems).HasForeignKey(x => x.WorkItemId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<TraceLink>(entity =>
        {
            entity.HasIndex(x => new { x.CaseId, x.SourceType, x.SourceId, x.TargetType, x.TargetId }).IsUnique();
            entity.Property(x => x.SourceType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.TargetType).HasMaxLength(30).IsRequired();
            entity.HasOne(x => x.Case).WithMany(x => x.TraceLinks).HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ImprovementOption>(entity =>
        {
            entity.HasIndex(x => new { x.CaseId, x.Sequence }).IsUnique();
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Recommendation).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.HasOne(x => x.Case).WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Problem).WithMany().HasForeignKey(x => x.ProblemId).OnDelete(DeleteBehavior.NoAction);
        });
    }
}
