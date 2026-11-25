// Spec: Documentation/dev-prism-ai-specs/05_DATA_MODEL.md (relational boundaries).
namespace DevPrism.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

/// <summary>Owns the schema for the durable local read model.</summary>
public sealed class DevPrismDbContext(DbContextOptions<DevPrismDbContext> options) : DbContext(options)
{
    /// <summary>Returns stored workspaces; callers must explicitly constrain operator scope.</summary>
    public DbSet<ProjectRecord> Projects => Set<ProjectRecord>();

    /// <summary>Returns source configuration rows; callers must explicitly constrain project scope.</summary>
    public DbSet<ConnectionRecord> Connections => Set<ConnectionRecord>();

    /// <summary>Returns recorded import attempts; callers constrain project scope.</summary>
    public DbSet<SyncRunRecord> SyncRuns => Set<SyncRunRecord>();

    /// <summary>Defines stable IDs, provider scope keys, and optimistic concurrency.</summary>
    /// <param name="modelBuilder">The EF Core schema builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var project = modelBuilder.Entity<ProjectRecord>();
        project.ToTable("projects");
        project.HasKey(row => row.Id);
        project.Property(row => row.Id).HasColumnName("id");
        project.Property(row => row.OwnerId).HasColumnName("owner_id");
        project.Property(row => row.Slug).HasColumnName("slug").HasMaxLength(80);
        project.Property(row => row.Name).HasColumnName("name").HasMaxLength(160);
        project.Property(row => row.CreatedAt).HasColumnName("created_at");
        project.Property(row => row.Version).HasColumnName("version").IsConcurrencyToken();
        project.HasIndex(row => new { row.OwnerId, row.Slug }).IsUnique();

        var connection = modelBuilder.Entity<ConnectionRecord>();
        connection.ToTable("connections");
        connection.HasKey(row => row.Id);
        connection.HasAlternateKey(row => new { row.ProjectId, row.Id });
        connection.Property(row => row.Id).HasColumnName("id");
        connection.Property(row => row.ProjectId).HasColumnName("project_id");
        connection.Property(row => row.Provider).HasColumnName("provider").HasMaxLength(32);
        connection.Property(row => row.ScopeKey).HasColumnName("scope_key").HasMaxLength(400);
        connection.Property(row => row.SecretReference).HasColumnName("secret_ref").HasMaxLength(180);
        connection.Property(row => row.Enabled).HasColumnName("enabled");
        connection.Property(row => row.Version).HasColumnName("version").IsConcurrencyToken();
        connection.HasIndex(row => new { row.ProjectId, row.Provider, row.ScopeKey }).IsUnique();
        connection.HasOne<ProjectRecord>().WithMany().HasForeignKey(row => row.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        ConfigureSyncRuns(modelBuilder);
    }

    /// <summary>Enforces scoped references and the one-active-run laptop limit in PostgreSQL.</summary>
    private static void ConfigureSyncRuns(ModelBuilder modelBuilder)
    {
        var run = modelBuilder.Entity<SyncRunRecord>();
        run.ToTable("sync_runs", table =>
        {
            table.HasCheckConstraint("ck_sync_runs_slot", "active_slot = 1");
            table.HasCheckConstraint("ck_sync_runs_state", "state IN ('Queued','Running','Succeeded','Partial','Failed','Interrupted','Cancelled')");
            table.HasCheckConstraint("ck_sync_runs_operation", "operation IN ('Probe','Import')");
            table.HasCheckConstraint("ck_sync_runs_version", "connection_version > 0");
        });
        run.HasKey(row => row.Id);
        run.Property(row => row.Id).HasColumnName("id");
        run.Property(row => row.ProjectId).HasColumnName("project_id");
        run.Property(row => row.ConnectionId).HasColumnName("connection_id");
        run.Property(row => row.ConnectionVersion).HasColumnName("connection_version");
        run.Property(row => row.Operation).HasColumnName("operation").HasConversion<string>().HasMaxLength(16);
        run.Property(row => row.State).HasColumnName("state").HasConversion<string>().HasMaxLength(16).IsConcurrencyToken();
        run.Property(row => row.RequestedAt).HasColumnName("requested_at");
        run.Property(row => row.StartedAt).HasColumnName("started_at");
        run.Property(row => row.FinishedAt).HasColumnName("finished_at");
        run.Property(row => row.ReasonCode).HasColumnName("reason_code").HasMaxLength(64);
        run.HasOne<ConnectionRecord>().WithMany()
            .HasForeignKey(row => new { row.ProjectId, row.ConnectionId })
            .HasPrincipalKey(row => new { row.ProjectId, row.Id })
            .OnDelete(DeleteBehavior.Restrict);
        run.HasIndex(row => new { row.ProjectId, row.RequestedAt, row.Id });

        // All active attempts share a single slot, even across connections and projects.
        run.Property<int>("ActiveSlot").HasColumnName("active_slot").HasDefaultValue(1);
        run.HasIndex("ActiveSlot").HasDatabaseName("ux_sync_runs_single_active").IsUnique()
            .HasFilter("state IN ('Queued','Running')");
    }
}
