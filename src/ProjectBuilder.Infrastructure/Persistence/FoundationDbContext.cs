using Microsoft.EntityFrameworkCore;

namespace ProjectBuilder.Infrastructure.Persistence;

public sealed class FoundationDbContext(DbContextOptions<FoundationDbContext> options) : DbContext(options)
{
    internal DbSet<ProjectRecord> Projects => Set<ProjectRecord>();
    internal DbSet<ProjectChangeSetRecord> ProjectChangeSets => Set<ProjectChangeSetRecord>();
    internal DbSet<ProjectChangeSetRecord> ProjectCreations => Set<ProjectChangeSetRecord>();
    internal DbSet<ProjectChangeOperationRecord> ProjectChangeOperations => Set<ProjectChangeOperationRecord>();
    internal DbSet<ModelElementRecord> ModelElements => Set<ModelElementRecord>();
    internal DbSet<ActorPayloadRecord> ActorPayloads => Set<ActorPayloadRecord>();
    internal DbSet<OutcomePayloadRecord> OutcomePayloads => Set<OutcomePayloadRecord>();
    internal DbSet<CapabilityPayloadRecord> CapabilityPayloads => Set<CapabilityPayloadRecord>();
    internal DbSet<NarrativePayloadRecord> NarrativePayloads => Set<NarrativePayloadRecord>();
    internal DbSet<StateLogicPayloadRecord> StateLogicPayloads => Set<StateLogicPayloadRecord>();
    internal DbSet<PathPayloadRecord> PathPayloads => Set<PathPayloadRecord>();
    internal DbSet<SystemContextPayloadRecord> SystemContextPayloads => Set<SystemContextPayloadRecord>();
    internal DbSet<ModelRelationRecord> ModelRelations => Set<ModelRelationRecord>();
    internal DbSet<PortableProjectSnapshotRecord> PortableProjectSnapshots => Set<PortableProjectSnapshotRecord>();
    internal DbSet<GapDispositionRecord> GapDispositions => Set<GapDispositionRecord>();
    internal DbSet<ClaimRecord> Claims => Set<ClaimRecord>();
    internal DbSet<EvidenceRecord> Evidence => Set<EvidenceRecord>();
    internal DbSet<CanvasViewRecord> CanvasViews => Set<CanvasViewRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var projects = modelBuilder.Entity<ProjectRecord>();
        projects.ToTable("projects");
        projects.HasKey(x => x.Id);
        projects.Property(x => x.Name).HasMaxLength(500).IsRequired();
        projects.Property(x => x.NormalizedName).HasMaxLength(500).IsRequired();
        projects.Property(x => x.Purpose).HasMaxLength(20_000).IsRequired();
        projects.Property(x => x.IntendedOutcome).HasMaxLength(20_000).IsRequired();
        projects.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
        projects.HasIndex(x => new { x.WorkspaceId, x.NormalizedName })
            .IsUnique().HasDatabaseName("ux_projects_workspace_normalized_name");

        var changes = modelBuilder.Entity<ProjectChangeSetRecord>();
        changes.ToTable("project_change_sets");
        changes.HasKey(x => x.Id);
        changes.Property(x => x.ChangeKind).HasMaxLength(100).IsRequired();
        changes.Property(x => x.RequestFingerprint).HasMaxLength(64).IsRequired();
        changes.Property(x => x.Reason).HasMaxLength(2_000).IsRequired();
        changes.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
        changes.Property(x => x.SemanticSummary).HasMaxLength(2_000).IsRequired();
        changes.ToTable(table => table.HasCheckConstraint(
            "ck_project_change_sets_operation_count_positive", "\"OperationCount\" > 0"));
        changes.HasIndex(x => x.ProjectId);
        changes.HasOne(x => x.Project).WithMany(x => x.ChangeSets)
            .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);

        var operations = modelBuilder.Entity<ProjectChangeOperationRecord>();
        operations.ToTable("project_change_operations");
        operations.HasKey(x => new { x.ChangeSetId, x.Sequence });
        operations.Property(x => x.Kind).HasMaxLength(100).IsRequired();
        operations.Property(x => x.SubjectKind).HasMaxLength(100).IsRequired();
        operations.Property(x => x.Summary).HasMaxLength(2_000).IsRequired();
        operations.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        operations.ToTable(table => table.HasCheckConstraint(
            "ck_project_change_operations_sequence_nonnegative", "\"Sequence\" >= 0"));
        operations.HasIndex(x => new { x.ProjectId, x.ChangeSetId });
        operations.HasOne(x => x.ChangeSet).WithMany(x => x.Operations)
            .HasForeignKey(x => x.ChangeSetId).OnDelete(DeleteBehavior.Cascade);

        var elements = modelBuilder.Entity<ModelElementRecord>();
        elements.ToTable("model_elements");
        elements.HasKey(x => x.Id);
        elements.Property(x => x.Kind).HasMaxLength(100).IsRequired();
        elements.Property(x => x.Name).HasMaxLength(500).IsRequired();
        elements.Property(x => x.Description).HasMaxLength(20_000).IsRequired();
        elements.Property(x => x.DefinitionStatus).HasMaxLength(100).IsRequired();
        elements.Property(x => x.KnowledgeStatus).HasMaxLength(100).IsRequired();
        elements.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
        elements.HasIndex(x => new { x.ProjectId, x.Order }).IsUnique();
        elements.HasOne(x => x.Project).WithMany(x => x.Elements)
            .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        elements.HasOne(x => x.Parent).WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentElementId).OnDelete(DeleteBehavior.Restrict);

        var actors = modelBuilder.Entity<ActorPayloadRecord>();
        actors.ToTable("actor_payloads");
        actors.HasKey(x => x.ElementId);
        actors.Property(x => x.ActorKind).HasMaxLength(100).IsRequired();
        actors.Property(x => x.ContextualRole).HasMaxLength(20_000).IsRequired();
        actors.Property(x => x.GoalsJson).HasColumnType("jsonb").IsRequired();
        actors.Property(x => x.ResponsibilitiesJson).HasColumnType("jsonb").IsRequired();
        actors.Property(x => x.AuthorityJson).HasColumnType("jsonb").IsRequired();
        actors.Property(x => x.ConstraintsJson).HasColumnType("jsonb").IsRequired();
        actors.HasOne(x => x.Element).WithOne(x => x.Actor)
            .HasForeignKey<ActorPayloadRecord>(x => x.ElementId).OnDelete(DeleteBehavior.Cascade);

        var outcomes = modelBuilder.Entity<OutcomePayloadRecord>();
        outcomes.ToTable("outcome_payloads");
        outcomes.HasKey(x => x.ElementId);
        outcomes.Property(x => x.Statement).HasMaxLength(20_000).IsRequired();
        outcomes.Property(x => x.SuccessSignalsJson).HasColumnType("jsonb").IsRequired();
        outcomes.HasOne(x => x.Element).WithOne(x => x.Outcome)
            .HasForeignKey<OutcomePayloadRecord>(x => x.ElementId).OnDelete(DeleteBehavior.Cascade);

        var capabilities = modelBuilder.Entity<CapabilityPayloadRecord>();
        capabilities.ToTable("capability_payloads");
        capabilities.HasKey(x => x.ElementId);
        capabilities.Property(x => x.OutcomeIdsJson).HasColumnType("jsonb").IsRequired();
        capabilities.Property(x => x.Priority).HasMaxLength(100).IsRequired();
        capabilities.HasOne(x => x.Element).WithOne(x => x.Capability)
            .HasForeignKey<CapabilityPayloadRecord>(x => x.ElementId).OnDelete(DeleteBehavior.Cascade);

        var narratives = modelBuilder.Entity<NarrativePayloadRecord>();
        narratives.ToTable("narrative_payloads");
        narratives.HasKey(x => x.ElementId);
        narratives.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        narratives.HasOne(x => x.Element).WithOne(x => x.Narrative)
            .HasForeignKey<NarrativePayloadRecord>(x => x.ElementId).OnDelete(DeleteBehavior.Cascade);

        var stateLogic = modelBuilder.Entity<StateLogicPayloadRecord>();
        stateLogic.ToTable("state_logic_payloads");
        stateLogic.HasKey(x => x.ElementId);
        stateLogic.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        stateLogic.HasOne(x => x.Element).WithOne(x => x.StateLogic)
            .HasForeignKey<StateLogicPayloadRecord>(x => x.ElementId).OnDelete(DeleteBehavior.Cascade);

        var paths = modelBuilder.Entity<PathPayloadRecord>();
        paths.ToTable("path_payloads");
        paths.HasKey(x => x.ElementId);
        paths.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        paths.HasOne(x => x.Element).WithOne(x => x.Path)
            .HasForeignKey<PathPayloadRecord>(x => x.ElementId).OnDelete(DeleteBehavior.Cascade);

        var systemContexts = modelBuilder.Entity<SystemContextPayloadRecord>();
        systemContexts.ToTable("system_context_payloads");
        systemContexts.HasKey(x => x.ElementId);
        systemContexts.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        systemContexts.HasOne(x => x.Element).WithOne(x => x.SystemContext)
            .HasForeignKey<SystemContextPayloadRecord>(x => x.ElementId).OnDelete(DeleteBehavior.Cascade);

        var relations = modelBuilder.Entity<ModelRelationRecord>();
        relations.ToTable("model_relations");
        relations.HasKey(x => x.Id);
        relations.Property(x => x.Kind).HasMaxLength(100).IsRequired();
        relations.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
        relations.HasIndex(x => new { x.Kind, x.SourceElementId, x.TargetElementId }).IsUnique();
        relations.HasIndex(x => new { x.Kind, x.TargetElementId })
            .IsUnique()
            .HasFilter("\"Kind\" = 'benefitsFrom'")
            .HasDatabaseName("ux_model_relations_benefits_from_target");
        relations.HasOne(x => x.Project).WithMany(x => x.Relations)
            .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        relations.HasOne(x => x.Source).WithMany().HasForeignKey(x => x.SourceElementId)
            .OnDelete(DeleteBehavior.Restrict);
        relations.HasOne(x => x.Target).WithMany().HasForeignKey(x => x.TargetElementId)
            .OnDelete(DeleteBehavior.Restrict);

        var snapshots = modelBuilder.Entity<PortableProjectSnapshotRecord>();
        snapshots.ToTable("portable_project_snapshots");
        snapshots.HasKey(x => x.ProjectId);
        snapshots.Property(x => x.FormatVersion).HasMaxLength(50).IsRequired();
        snapshots.Property(x => x.ContentHash).HasMaxLength(71).IsRequired();
        snapshots.Property(x => x.CanonicalJson).HasColumnType("text").IsRequired();
        snapshots.HasOne(x => x.Project).WithOne()
            .HasForeignKey<PortableProjectSnapshotRecord>(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        var dispositions = modelBuilder.Entity<GapDispositionRecord>();
        dispositions.ToTable("gap_dispositions");
        dispositions.HasKey(x => x.Id);
        dispositions.Property(x => x.ProfileId).HasMaxLength(100).IsRequired();
        dispositions.Property(x => x.RuleCode).HasMaxLength(100).IsRequired();
        dispositions.Property(x => x.Disposition).HasMaxLength(100).IsRequired();
        dispositions.Property(x => x.Rationale).HasMaxLength(20_000).IsRequired();
        dispositions.Property(x => x.Consequence).HasMaxLength(20_000).IsRequired();
        dispositions.Property(x => x.ReviewOn).HasMaxLength(10);
        dispositions.Property(x => x.TargetMilestone).HasMaxLength(200);
        dispositions.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
        dispositions.HasIndex(x => new { x.ProjectId, x.ProfileId, x.RuleCode, x.ScopeId }).IsUnique();
        dispositions.HasOne(x => x.Project).WithMany(x => x.GapDispositions)
            .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);

        var claims = modelBuilder.Entity<ClaimRecord>();
        claims.ToTable("claims"); claims.HasKey(x => x.Id);
        claims.Property(x => x.Kind).HasMaxLength(100).IsRequired();
        claims.Property(x => x.Statement).HasMaxLength(20_000).IsRequired();
        claims.Property(x => x.Status).HasMaxLength(100).IsRequired();
        claims.Property(x => x.ElementIdsJson).HasColumnType("jsonb").IsRequired();
        claims.Property(x => x.TagsJson).HasColumnType("jsonb").IsRequired();
        claims.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
        claims.HasIndex(x => x.ProjectId);
        claims.HasOne(x => x.Project).WithMany(x => x.Claims).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);

        var evidence = modelBuilder.Entity<EvidenceRecord>();
        evidence.ToTable("evidence"); evidence.HasKey(x => x.Id);
        evidence.Property(x => x.Kind).HasMaxLength(100).IsRequired();
        evidence.Property(x => x.Status).HasMaxLength(100).IsRequired();
        evidence.Property(x => x.Producer).HasMaxLength(500).IsRequired();
        evidence.Property(x => x.Environment).HasMaxLength(500).IsRequired();
        evidence.Property(x => x.Summary).HasMaxLength(20_000).IsRequired();
        evidence.Property(x => x.LimitationsJson).HasColumnType("jsonb").IsRequired();
        evidence.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
        evidence.HasIndex(x => x.ClaimId).IsUnique();
        evidence.HasOne(x => x.Project).WithMany(x => x.Evidence).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        evidence.HasOne(x => x.Claim).WithOne(x => x.Evidence).HasForeignKey<EvidenceRecord>(x => x.ClaimId).OnDelete(DeleteBehavior.Cascade);

        var canvasViews = modelBuilder.Entity<CanvasViewRecord>();
        canvasViews.ToTable("canvas_views");
        canvasViews.HasKey(x => x.Id);
        canvasViews.Property(x => x.Name).HasMaxLength(500).IsRequired();
        canvasViews.Property(x => x.Lens).HasMaxLength(100).IsRequired();
        canvasViews.Property(x => x.ScopeKey).HasMaxLength(500).IsRequired();
        canvasViews.Property(x => x.Visibility).HasMaxLength(50).IsRequired();
        canvasViews.Property(x => x.OwnerKey).HasMaxLength(200).IsRequired();
        canvasViews.Property(x => x.LayoutJson).HasColumnType("jsonb").IsRequired();
        canvasViews.Property(x => x.UpdatedBy).HasMaxLength(200).IsRequired();
        canvasViews.Property(x => x.LayoutVersion).IsConcurrencyToken();
        canvasViews.HasIndex(x => new { x.ProjectId, x.Lens, x.ScopeKey, x.Visibility, x.OwnerKey }).IsUnique();
        canvasViews.HasOne(x => x.Project).WithMany(x => x.CanvasViews)
            .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ProjectRecord
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string IntendedOutcome { get; set; } = string.Empty;
    public long CurrentRevision { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public ICollection<ProjectChangeSetRecord> ChangeSets { get; } = [];
    public ICollection<ModelElementRecord> Elements { get; } = [];
    public ICollection<ModelRelationRecord> Relations { get; } = [];
    public ICollection<GapDispositionRecord> GapDispositions { get; } = [];
    public ICollection<ClaimRecord> Claims { get; } = [];
    public ICollection<EvidenceRecord> Evidence { get; } = [];
    public ICollection<CanvasViewRecord> CanvasViews { get; } = [];
}

internal sealed class CanvasViewRecord
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = "";
    public string Lens { get; set; } = "";
    public string ScopeKey { get; set; } = "";
    public string Visibility { get; set; } = "";
    public string OwnerKey { get; set; } = "";
    public long ModelRevision { get; set; }
    public long LayoutVersion { get; set; }
    public string LayoutJson { get; set; } = "{}";
    public DateTimeOffset UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = "";
    public ProjectRecord Project { get; set; } = null!;
}

internal sealed class ClaimRecord
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Kind { get; set; } = "";
    public string Statement { get; set; } = ""; public string Status { get; set; } = "";
    public string ElementIdsJson { get; set; } = "[]"; public Guid EvidenceId { get; set; }
    public Guid OwnerId { get; set; }
    public string TagsJson { get; set; } = "[]";
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "";
    public ProjectRecord Project { get; set; } = null!; public EvidenceRecord? Evidence { get; set; }
}

internal sealed class EvidenceRecord
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Kind { get; set; } = "";
    public string Status { get; set; } = ""; public Guid ClaimId { get; set; }
    public string Producer { get; set; } = "";
    public DateTimeOffset ProducedAt { get; set; }
    public long ModelRevision { get; set; }
    public string Environment { get; set; } = ""; public string Summary { get; set; } = "";
    public string LimitationsJson { get; set; } = "[]"; public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = ""; public ProjectRecord Project { get; set; } = null!;
    public ClaimRecord Claim { get; set; } = null!;
}

internal sealed class GapDispositionRecord
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string ProfileId { get; set; } = string.Empty;
    public string RuleCode { get; set; } = string.Empty;
    public Guid ScopeId { get; set; }
    public string Disposition { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public string Consequence { get; set; } = string.Empty;
    public Guid AuthorityActorId { get; set; }
    public string? ReviewOn { get; set; }
    public string? TargetMilestone { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public ProjectRecord Project { get; set; } = null!;
}

internal sealed class ProjectChangeSetRecord
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public long? BaseRevision { get; set; }
    public long ResultRevision { get; set; }
    public Guid? ElementId { get; set; }
    public string ChangeKind { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public int OperationCount { get; set; }
    public string SemanticSummary { get; set; } = string.Empty;
    public ProjectRecord Project { get; set; } = null!;
    public ICollection<ProjectChangeOperationRecord> Operations { get; } = [];
}

internal sealed class ProjectChangeOperationRecord
{
    public Guid ChangeSetId { get; set; }
    public Guid ProjectId { get; set; }
    public int Sequence { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string SubjectKind { get; set; } = string.Empty;
    public Guid? ElementId { get; set; }
    public Guid? RelationId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public ProjectChangeSetRecord ChangeSet { get; set; } = null!;
}

internal sealed class ModelElementRecord
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? ParentElementId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DefinitionStatus { get; set; } = string.Empty;
    public string KnowledgeStatus { get; set; } = string.Empty;
    public int Order { get; set; }
    public long Version { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public ProjectRecord Project { get; set; } = null!;
    public ModelElementRecord? Parent { get; set; }
    public ICollection<ModelElementRecord> Children { get; } = [];
    public ActorPayloadRecord? Actor { get; set; }
    public OutcomePayloadRecord? Outcome { get; set; }
    public CapabilityPayloadRecord? Capability { get; set; }
    public NarrativePayloadRecord? Narrative { get; set; }
    public StateLogicPayloadRecord? StateLogic { get; set; }
    public PathPayloadRecord? Path { get; set; }
    public SystemContextPayloadRecord? SystemContext { get; set; }
}

internal sealed class ActorPayloadRecord
{
    public Guid ElementId { get; set; }
    public string ActorKind { get; set; } = string.Empty;
    public string ContextualRole { get; set; } = string.Empty;
    public string GoalsJson { get; set; } = "[]";
    public string ResponsibilitiesJson { get; set; } = "[]";
    public string AuthorityJson { get; set; } = "[]";
    public string ConstraintsJson { get; set; } = "[]";
    public ModelElementRecord Element { get; set; } = null!;
}

internal sealed class OutcomePayloadRecord
{
    public Guid ElementId { get; set; }
    public string Statement { get; set; } = string.Empty;
    public string SuccessSignalsJson { get; set; } = "[]";
    public ModelElementRecord Element { get; set; } = null!;
}

internal sealed class CapabilityPayloadRecord
{
    public Guid ElementId { get; set; }
    public string OutcomeIdsJson { get; set; } = "[]";
    public string Priority { get; set; } = string.Empty;
    public ModelElementRecord Element { get; set; } = null!;
}

internal sealed class NarrativePayloadRecord
{
    public Guid ElementId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public ModelElementRecord Element { get; set; } = null!;
}

internal sealed class StateLogicPayloadRecord
{
    public Guid ElementId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public ModelElementRecord Element { get; set; } = null!;
}

internal sealed class PathPayloadRecord
{
    public Guid ElementId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public ModelElementRecord Element { get; set; } = null!;
}

internal sealed class SystemContextPayloadRecord
{
    public Guid ElementId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public ModelElementRecord Element { get; set; } = null!;
}

internal sealed class ModelRelationRecord
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public Guid SourceElementId { get; set; }
    public Guid TargetElementId { get; set; }
    public long Version { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public ProjectRecord Project { get; set; } = null!;
    public ModelElementRecord Source { get; set; } = null!;
    public ModelElementRecord Target { get; set; } = null!;
}

internal sealed class PortableProjectSnapshotRecord
{
    public Guid ProjectId { get; set; }
    public long ModelRevision { get; set; }
    public string FormatVersion { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string CanonicalJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public ProjectRecord Project { get; set; } = null!;
}
