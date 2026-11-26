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

    /// <summary>Returns locally authored features within a caller-supplied project scope.</summary>
    public DbSet<FeatureRecord> Features => Set<FeatureRecord>();

    /// <summary>Returns local decision history scoped by project and feature.</summary>
    public DbSet<DecisionVersionRecord> Decisions => Set<DecisionVersionRecord>();

    public DbSet<McpProfileRecord> McpProfiles => Set<McpProfileRecord>();

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

        var profile = modelBuilder.Entity<McpProfileRecord>();
        profile.ToTable("mcp_profiles", table => table.HasCheckConstraint("ck_mcp_profiles_provider",
            "provider IN ('AzureDevOpsBoards','Figma')"));
        profile.HasKey(row => row.Id);
        profile.Property(row => row.Id).HasColumnName("id");
        profile.Property(row => row.ProjectId).HasColumnName("project_id");
        profile.Property(row => row.Provider).HasColumnName("provider").HasMaxLength(32);
        profile.Property(row => row.Endpoint).HasColumnName("endpoint").HasMaxLength(255);
        profile.Property(row => row.Enabled).HasColumnName("enabled");
        profile.Property(row => row.Version).HasColumnName("version").IsConcurrencyToken();
        profile.HasIndex(row => row.Provider).HasFilter("project_id IS NULL").IsUnique();
        profile.HasIndex(row => new { row.ProjectId, row.Provider }).HasFilter("project_id IS NOT NULL").IsUnique();
        profile.HasOne<ProjectRecord>().WithMany().HasForeignKey(row => row.ProjectId).OnDelete(DeleteBehavior.Cascade);

        ConfigureSyncRuns(modelBuilder);
        ConfigureFeatures(modelBuilder);
        ConfigureDecisions(modelBuilder);
    }

    /// <summary>Constrains decision versions and keeps the approved version unique per project key.</summary>
    private static void ConfigureDecisions(ModelBuilder modelBuilder)
    {
        var row = modelBuilder.Entity<DecisionVersionRecord>();
        row.ToTable("decision_versions", table =>
        {
            table.HasCheckConstraint("ck_decision_revision", "revision > 0");
            table.HasCheckConstraint("ck_decision_approval", "(state = 'Draft' AND approved_by IS NULL AND approved_at IS NULL) OR (state IN ('Approved','Superseded') AND approved_by IS NOT NULL AND approved_at IS NOT NULL)");
        });
        row.HasKey(value => value.Id);
        row.Property(value => value.Id).HasColumnName("id");
        row.Property(value => value.ProjectId).HasColumnName("project_id");
        row.Property(value => value.FeatureId).HasColumnName("feature_id");
        row.Property(value => value.Key).HasColumnName("key").HasMaxLength(80);
        row.Property(value => value.Revision).HasColumnName("revision");
        row.Property(value => value.Statement).HasColumnName("statement").HasMaxLength(2000);
        row.Property(value => value.Rationale).HasColumnName("rationale").HasMaxLength(2000);
        row.Property(value => value.AuthoredBy).HasColumnName("authored_by");
        row.Property(value => value.CreatedAt).HasColumnName("created_at");
        row.Property(value => value.State).HasColumnName("state").HasConversion<string>().HasMaxLength(16).IsConcurrencyToken();
        row.Property(value => value.ApprovedBy).HasColumnName("approved_by");
        row.Property(value => value.ApprovedAt).HasColumnName("approved_at");
        row.HasOne<FeatureRecord>().WithMany().HasForeignKey(value => new { value.ProjectId, value.FeatureId })
            .HasPrincipalKey(value => new { value.ProjectId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        row.HasIndex(value => new { value.ProjectId, value.Key, value.Revision }).IsUnique();
        row.HasIndex(value => new { value.ProjectId, value.Key, value.State }).IsUnique()
            .HasFilter("state IN ('Draft','Approved')");
    }

    /// <summary>Keeps feature identity and keys scoped to their project.</summary>
    private static void ConfigureFeatures(ModelBuilder modelBuilder)
    {
        var feature = modelBuilder.Entity<FeatureRecord>();
        feature.ToTable("features");
        feature.HasKey(row => row.Id);
        feature.HasAlternateKey(row => new { row.ProjectId, row.Id });
        feature.Property(row => row.Id).HasColumnName("id");
        feature.Property(row => row.ProjectId).HasColumnName("project_id");
        feature.Property(row => row.Key).HasColumnName("key").HasMaxLength(80);
        feature.Property(row => row.Name).HasColumnName("name").HasMaxLength(160);
        feature.Property(row => row.Description).HasColumnName("description").HasMaxLength(2000);
        feature.Property(row => row.CreatedAt).HasColumnName("created_at");
        feature.Property(row => row.Version).HasColumnName("version").IsConcurrencyToken();
        feature.HasIndex(row => new { row.ProjectId, row.Key }).IsUnique().HasDatabaseName("ux_features_project_key");
        feature.HasOne<ProjectRecord>().WithMany().HasForeignKey(row => row.ProjectId).OnDelete(DeleteBehavior.Restrict);
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
