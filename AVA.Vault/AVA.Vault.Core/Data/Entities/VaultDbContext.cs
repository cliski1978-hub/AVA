using Microsoft.EntityFrameworkCore;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Data.Entities
{
    public class VaultDbContext : DbContext
    {
        public DbSet<ActivityLogEntry> ActivityLog { get; set; }
        public DbSet<VaultHeader> VaultHeaders { get; set; }
        public DbSet<VaultProject> VaultProjects { get; set; }
        public DbSet<VaultSession> VaultSessions { get; set; }
        public DbSet<VaultFileRef> VaultFileRefs { get; set; }
        public DbSet<VaultGraph> VaultGraphs { get; set; }
        public DbSet<VaultNote> VaultNotes { get; set; }
        public DbSet<VaultTag> VaultTags { get; set; }
        public DbSet<VaultNoteRelation> VaultNoteRelations { get; set; }
        public DbSet<VaultMetadata> VaultMetadata { get; set; }
        public DbSet<VaultWorkflow> VaultWorkflows { get; set; }
        public DbSet<VaultWorkflowNode> VaultWorkflowNodes { get; set; }
        public DbSet<VaultWorkflowLine> VaultWorkflowLines { get; set; }
        public DbSet<VaultWorkflowLineStep> VaultWorkflowLineSteps { get; set; }
        public DbSet<VaultHeaderNote> VaultHeaderNotes { get; set; }
        public DbSet<VaultProjectNote> VaultProjectNotes { get; set; }
        public DbSet<VaultWorkflowNote> VaultWorkflowNotes { get; set; }
        public DbSet<VaultWorkflowNodeNote> VaultWorkflowNodeNotes { get; set; }
        public DbSet<VaultWorkflowLineNote> VaultWorkflowLineNotes { get; set; }
        public DbSet<VaultWorkflowLineStepNote> VaultWorkflowLineStepNotes { get; set; }
        public DbSet<VaultSessionNote> VaultSessionNotes { get; set; }
        public DbSet<VaultFileRefNote> VaultFileRefNotes { get; set; }
        public DbSet<VaultHeaderFileRef> VaultHeaderFileRefs { get; set; }
        public DbSet<VaultProjectFileRef> VaultProjectFileRefs { get; set; }
        public DbSet<VaultWorkflowFileRef> VaultWorkflowFileRefs { get; set; }
        public DbSet<VaultWorkflowNodeFileRef> VaultWorkflowNodeFileRefs { get; set; }
        public DbSet<VaultWorkflowLineFileRef> VaultWorkflowLineFileRefs { get; set; }
        public DbSet<VaultWorkflowLineStepFileRef> VaultWorkflowLineStepFileRefs { get; set; }
        public DbSet<VaultSessionFileRef> VaultSessionFileRefs { get; set; }
        public DbSet<VaultNoteFileRef> VaultNoteFileRefs { get; set; }
        public DbSet<VaultNoteVaultTag> VaultNoteVaultTags { get; set; }
        public DbSet<VaultFileRefRelation> VaultFileRefRelations { get; set; }
        public DbSet<AvaProviderProfile> AvaProviderProfiles { get; set; }
        public DbSet<AvaModelDefinition> AvaModelDefinitions { get; set; }
        public DbSet<AvaSecret> AvaSecrets { get; set; }
        public DbSet<ModuleIdentity> ModuleIdentity { get; set; }

        public VaultDbContext(DbContextOptions<VaultDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // -- VaultHeader -> VaultProjects ---------------------------------
            modelBuilder.Entity<VaultProject>(entity =>
            {
                entity.HasIndex(p => p.VaultID).HasDatabaseName("IX_VaultProjects_VaultID");

                entity
                    .HasOne(p => p.Vault)
                    .WithMany(v => v.Projects)
                    .HasForeignKey(p => p.VaultID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -- VaultSession ------------------------------------------------
            modelBuilder.Entity<VaultSession>(entity =>
            {
                entity.HasIndex(s => s.ProjectID).HasDatabaseName("IX_VaultSessions_ProjectID");
                entity.HasIndex(s => new { s.ProjectID, s.SortOrder }).HasDatabaseName("IX_VaultSessions_ProjectId_SortOrder");

                entity
                    .HasOne(s => s.Project)
                    .WithMany(p => p.Sessions)
                    .HasForeignKey(s => s.ProjectID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity
                    .HasOne(s => s.Vault)
                    .WithMany()
                    .HasForeignKey(s => s.VaultID)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // -- VaultNote ---------------------------------------------------
            modelBuilder.Entity<VaultNote>(entity =>
            {
                entity.HasIndex(n => n.VaultID).HasDatabaseName("IX_VaultNotes_VaultID");
                entity.HasIndex(n => n.SessionID).HasDatabaseName("IX_VaultNotes_SessionID");

                entity
                    .HasOne(n => n.Session)
                    .WithMany()
                    .HasForeignKey(n => n.SessionID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity
                    .HasOne(n => n.Vault)
                    .WithMany()
                    .HasForeignKey(n => n.VaultID)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // -- VaultTag ----------------------------------------------------
            modelBuilder.Entity<VaultTag>(entity =>
            {
                entity.HasIndex(t => t.ProjectID).HasDatabaseName("IX_VaultTags_ProjectID");
                entity.HasIndex(t => new { t.ProjectID, t.Name }).IsUnique().HasDatabaseName("UX_VaultTags_ProjectId_Name");

                entity
                    .HasOne(t => t.Project)
                    .WithMany(p => p.Tags)
                    .HasForeignKey(t => t.ProjectID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -- VaultMetadata -----------------------------------------------
            modelBuilder.Entity<VaultMetadata>(entity =>
            {
                entity.HasIndex(m => m.NoteID).HasDatabaseName("IX_VaultMetadata_NoteID");
                entity.HasIndex(m => new { m.NoteID, m.Key }).HasDatabaseName("IX_VaultMetadata_NoteId_Key");

                entity
                    .HasOne(m => m.Note)
                    .WithMany(n => n.Metadata)
                    .HasForeignKey(m => m.NoteID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -- VaultGraph --------------------------------------------------
            modelBuilder.Entity<VaultGraph>(entity =>
            {
                entity.HasIndex(g => g.ProjectID).HasDatabaseName("IX_VaultGraphs_ProjectID");

                entity
                    .HasOne(g => g.Project)
                    .WithMany(p => p.Graphs)
                    .HasForeignKey(g => g.ProjectID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -- VaultFileRef ------------------------------------------------
            modelBuilder.Entity<VaultFileRef>(entity =>
            {
                entity.HasIndex(f => f.ProjectID).HasDatabaseName("IX_VaultFileRefs_ProjectID");
                entity.HasIndex(f => f.VaultID).HasDatabaseName("IX_VaultFileRefs_VaultID");

                entity
                    .HasOne(f => f.Vault)
                    .WithMany()
                    .HasForeignKey(f => f.VaultID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity
                    .HasOne(f => f.Project)
                    .WithMany(p => p.FileRefs)
                    .HasForeignKey(f => f.ProjectID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -- VaultWorkflow -----------------------------------------------
            modelBuilder.Entity<VaultWorkflow>(entity =>
            {
                entity.HasIndex(w => w.ProjectID).HasDatabaseName("IX_VaultWorkflows_ProjectID");
                entity.HasIndex(w => new { w.ProjectID, w.SortOrder }).HasDatabaseName("IX_VaultWorkflows_ProjectId_SortOrder");

                entity
                    .HasOne(w => w.Project)
                    .WithMany(p => p.Workflows)
                    .HasForeignKey(w => w.ProjectID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -- VaultWorkflowNode -------------------------------------------
            modelBuilder.Entity<VaultWorkflowNode>(entity =>
            {
                entity.HasIndex(n => n.WorkflowID).HasDatabaseName("IX_VaultWorkflowNodes_WorkflowID");
                entity.HasIndex(n => new { n.WorkflowID, n.NodeOrder }).HasDatabaseName("IX_VaultWorkflowNodes_WorkflowId_SortOrder");

                entity
                    .HasOne(n => n.Workflow)
                    .WithMany(w => w.Nodes)
                    .HasForeignKey(n => n.WorkflowID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -- VaultWorkflowLine -------------------------------------------
            modelBuilder.Entity<VaultWorkflowLine>(entity =>
            {
                entity.HasIndex(l => l.WorkflowID).HasDatabaseName("IX_VaultWorkflowLines_WorkflowID");
                entity.HasIndex(l => l.SourceWorkflowNodeID).HasDatabaseName("IX_VaultWorkflowLines_SourceWorkflowNodeID");
                entity.HasIndex(l => l.TargetWorkflowNodeID).HasDatabaseName("IX_VaultWorkflowLines_TargetWorkflowNodeID");
                entity.HasIndex(l => new { l.SourceWorkflowNodeID, l.LineOrder }).HasDatabaseName("IX_VaultWorkflowLines_SourceWorkflowNodeId_SortOrder");

                entity
                    .HasOne(l => l.Workflow)
                    .WithMany(w => w.Lines)
                    .HasForeignKey(l => l.WorkflowID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity
                    .HasOne(l => l.SourceWorkflowNode)
                    .WithMany(n => n.OutgoingLines)
                    .HasForeignKey(l => l.SourceWorkflowNodeID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity
                    .HasOne(l => l.TargetWorkflowNode)
                    .WithMany(n => n.IncomingLines)
                    .HasForeignKey(l => l.TargetWorkflowNodeID)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // -- VaultWorkflowLineStep ---------------------------------------
            modelBuilder.Entity<VaultWorkflowLineStep>(entity =>
            {
                entity.HasIndex(s => s.WorkflowLineID).HasDatabaseName("IX_VaultWorkflowLineSteps_WorkflowLineID");
                entity.HasIndex(s => new { s.WorkflowLineID, s.StepOrder }).IsUnique().HasDatabaseName("UX_VaultWorkflowLineSteps_WorkflowLineId_StepOrder");

                entity
                    .HasOne(s => s.WorkflowLine)
                    .WithMany(l => l.Steps)
                    .HasForeignKey(s => s.WorkflowLineID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -- VaultNoteRelation -------------------------------------------
            modelBuilder.Entity<VaultNoteRelation>(entity =>
            {
                entity.HasIndex(r => r.SourceNoteID).HasDatabaseName("IX_VaultNoteRelations_SourceNoteID");
                entity.HasIndex(r => r.TargetNoteID).HasDatabaseName("IX_VaultNoteRelations_TargetNoteID");
                entity.HasIndex(r => new { r.SourceNoteID, r.SortOrder }).HasDatabaseName("IX_VaultNoteRelations_SourceNoteId_SortOrder");

                entity
                    .HasOne(r => r.OutgoingNote)
                    .WithMany(n => n.OutgoingRelations)
                    .HasForeignKey(r => r.SourceNoteID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity
                    .HasOne(r => r.IncomingNote)
                    .WithMany(n => n.IncomingRelations)
                    .HasForeignKey(r => r.TargetNoteID)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // -- VaultHeaderNote --------------------------------------------
            modelBuilder.Entity<VaultHeaderNote>(entity =>
            {
                entity.HasIndex(e => e.NoteID).HasDatabaseName("IX_VaultHeaderNotes_NoteID");
                entity.HasIndex(e => e.VaultID).HasDatabaseName("IX_VaultHeaderNotes_VaultID");
                entity.HasIndex(e => new { e.VaultID, e.SortOrder }).HasDatabaseName("IX_VaultHeaderNotes_VaultId_SortOrder");
                entity.HasIndex(e => new { e.VaultID, e.NoteID }).IsUnique().HasDatabaseName("UX_VaultHeaderNotes_VaultId_NoteID");

                entity
                    .HasOne(e => e.Note)
                    .WithMany(n => n.HeaderNotes)
                    .HasForeignKey(e => e.NoteID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity
                    .HasOne(e => e.Header)
                    .WithMany(h => h.HeaderNotes)
                    .HasForeignKey(e => e.VaultID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -- VaultProjectNote --------------------------------------------
            modelBuilder.Entity<VaultProjectNote>(entity =>
            {
                entity.HasIndex(e => e.NoteID).HasDatabaseName("IX_VaultProjectNotes_NoteID");
                entity.HasIndex(e => e.ProjectID).HasDatabaseName("IX_VaultProjectNotes_ProjectID");
                entity.HasIndex(e => new { e.ProjectID, e.SortOrder }).HasDatabaseName("IX_VaultProjectNotes_ProjectId_SortOrder");
                entity.HasIndex(e => new { e.ProjectID, e.NoteID }).IsUnique().HasDatabaseName("UX_VaultProjectNotes_ProjectId_NoteID");

                entity
                    .HasOne(e => e.Note)
                    .WithMany(n => n.ProjectNotes)
                    .HasForeignKey(e => e.NoteID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity
                    .HasOne(e => e.Project)
                    .WithMany(p => p.ProjectNotes)
                    .HasForeignKey(e => e.ProjectID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -- VaultWorkflowNote -------------------------------------------
            modelBuilder.Entity<VaultWorkflowNote>(entity =>
            {
                entity.HasIndex(e => e.NoteID).HasDatabaseName("IX_VaultWorkflowNotes_NoteID");
                entity.HasIndex(e => e.WorkflowID).HasDatabaseName("IX_VaultWorkflowNotes_WorkflowID");
                entity.HasIndex(e => new { e.WorkflowID, e.SortOrder }).HasDatabaseName("IX_VaultWorkflowNotes_WorkflowId_SortOrder");
                entity.HasIndex(e => new { e.WorkflowID, e.NoteID }).IsUnique().HasDatabaseName("UX_VaultWorkflowNotes_WorkflowId_NoteID");

                entity
                    .HasOne(e => e.Note)
                    .WithMany(n => n.WorkflowNotes)
                    .HasForeignKey(e => e.NoteID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity
                    .HasOne(e => e.Workflow)
                    .WithMany(w => w.WorkflowNotes)
                    .HasForeignKey(e => e.WorkflowID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -- VaultWorkflowNodeNote ---------------------------------------
            modelBuilder.Entity<VaultWorkflowNodeNote>(entity =>
            {
                entity.HasIndex(e => e.NoteID).HasDatabaseName("IX_VaultWorkflowNodeNotes_NoteID");
                entity.HasIndex(e => e.WorkflowNodeID).HasDatabaseName("IX_VaultWorkflowNodeNotes_WorkflowNodeID");
                entity.HasIndex(e => new { e.WorkflowNodeID, e.NoteOrder }).HasDatabaseName("IX_VaultWorkflowNodeNotes_WorkflowNodeId_SortOrder");
                entity.HasIndex(e => new { e.WorkflowNodeID, e.NoteID }).IsUnique().HasDatabaseName("UX_VaultWorkflowNodeNotes_WorkflowNodeId_NoteID");

                entity
                    .HasOne(e => e.Note)
                    .WithMany(n => n.WorkflowNodeNotes)
                    .HasForeignKey(e => e.NoteID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity
                    .HasOne(e => e.WorkflowNode)
                    .WithMany(n => n.WorkflowNodeNotes)
                    .HasForeignKey(e => e.WorkflowNodeID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -- VaultWorkflowLineNote ---------------------------------------
            modelBuilder.Entity<VaultWorkflowLineNote>(entity =>
            {
                entity.HasIndex(e => e.NoteID).HasDatabaseName("IX_VaultWorkflowLineNotes_NoteID");
                entity.HasIndex(e => e.WorkflowLineID).HasDatabaseName("IX_VaultWorkflowLineNotes_WorkflowLineID");
                entity.HasIndex(e => new { e.WorkflowLineID, e.SortOrder }).HasDatabaseName("IX_VaultWorkflowLineNotes_WorkflowLineId_SortOrder");
                entity.HasIndex(e => new { e.WorkflowLineID, e.NoteID }).IsUnique().HasDatabaseName("UX_VaultWorkflowLineNotes_WorkflowLineId_NoteID");

                entity
                    .HasOne(e => e.Note)
                    .WithMany(n => n.WorkflowLineNotes)
                    .HasForeignKey(e => e.NoteID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity
                    .HasOne(e => e.WorkflowLine)
                    .WithMany(l => l.WorkflowLineNotes)
                    .HasForeignKey(e => e.WorkflowLineID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -- VaultWorkflowLineStepNote -----------------------------------
            modelBuilder.Entity<VaultWorkflowLineStepNote>(entity =>
            {
                entity.HasIndex(e => e.NoteID).HasDatabaseName("IX_VaultWorkflowLineStepNotes_NoteID");
                entity.HasIndex(e => e.WorkflowLineStepID).HasDatabaseName("IX_VaultWorkflowLineStepNotes_WorkflowLineStepID");
                entity.HasIndex(e => new { e.WorkflowLineStepID, e.SortOrder }).HasDatabaseName("IX_VaultWorkflowLineStepNotes_WorkflowLineStepId_SortOrder");
                entity.HasIndex(e => new { e.WorkflowLineStepID, e.NoteID }).IsUnique().HasDatabaseName("UX_VaultWorkflowLineStepNotes_WorkflowLineStepId_NoteID");

                entity
                    .HasOne(e => e.Note)
                    .WithMany(n => n.WorkflowLineStepNotes)
                    .HasForeignKey(e => e.NoteID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity
                    .HasOne(e => e.WorkflowLineStep)
                    .WithMany(s => s.WorkflowLineStepNotes)
                    .HasForeignKey(e => e.WorkflowLineStepID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -- VaultSessionNote --------------------------------------------
            modelBuilder.Entity<VaultSessionNote>(entity =>
            {
                entity.HasIndex(e => e.NoteID).HasDatabaseName("IX_VaultSessionNotes_NoteID");
                entity.HasIndex(e => e.SessionID).HasDatabaseName("IX_VaultSessionNotes_SessionID");
                entity.HasIndex(e => new { e.SessionID, e.SortOrder }).HasDatabaseName("IX_VaultSessionNotes_SessionId_SortOrder");
                entity.HasIndex(e => new { e.SessionID, e.NoteID }).IsUnique().HasDatabaseName("UX_VaultSessionNotes_SessionId_NoteID");

                entity
                    .HasOne(e => e.Note)
                    .WithMany(n => n.SessionNotes)
                    .HasForeignKey(e => e.NoteID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity
                    .HasOne(e => e.Session)
                    .WithMany(s => s.SessionNotes)
                    .HasForeignKey(e => e.SessionID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -- VaultFileRefNote --------------------------------------------
            modelBuilder.Entity<VaultFileRefNote>(entity =>
            {
                entity.HasIndex(e => e.NoteID).HasDatabaseName("IX_VaultFileRefNotes_NoteID");
                entity.HasIndex(e => e.FileRefID).HasDatabaseName("IX_VaultFileRefNotes_FileRefID");
                entity.HasIndex(e => new { e.FileRefID, e.NoteOrder }).HasDatabaseName("IX_VaultFileRefNotes_FileRefId_SortOrder");
                entity.HasIndex(e => new { e.FileRefID, e.NoteID }).IsUnique().HasDatabaseName("UX_VaultFileRefNotes_FileRefId_NoteID");

                entity
                    .HasOne(e => e.Note)
                    .WithMany(n => n.FileRefNotes)
                    .HasForeignKey(e => e.NoteID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity
                    .HasOne(e => e.FileRef)
                    .WithMany(f => f.FileRefNotes)
                    .HasForeignKey(e => e.FileRefID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -- VaultHeaderFileRef ------------------------------------------
            modelBuilder.Entity<VaultHeaderFileRef>(entity =>
            {
                entity.HasIndex(e => e.FileRefID).HasDatabaseName("IX_VaultHeaderFileRefs_FileRefID");
                entity.HasIndex(e => e.VaultID).HasDatabaseName("IX_VaultHeaderFileRefs_VaultID");
                entity.HasIndex(e => new { e.VaultID, e.SortOrder }).HasDatabaseName("IX_VaultHeaderFileRefs_VaultId_SortOrder");
                entity.HasIndex(e => new { e.VaultID, e.FileRefID }).IsUnique().HasDatabaseName("UX_VaultHeaderFileRefs_VaultId_FileRefID");

                entity
                    .HasOne(e => e.FileRef)
                    .WithMany(f => f.HeaderFileRefs)
                    .HasForeignKey(e => e.FileRefID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity
                    .HasOne(e => e.Header)
                    .WithMany(h => h.HeaderFileRefs)
                    .HasForeignKey(e => e.VaultID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -- VaultProjectFileRef ------------------------------------------
            modelBuilder.Entity<VaultProjectFileRef>(entity =>
            {
                entity.HasIndex(e => e.FileRefID).HasDatabaseName("IX_VaultProjectFileRefs_FileRefID");
                entity.HasIndex(e => e.ProjectID).HasDatabaseName("IX_VaultProjectFileRefs_ProjectID");
                entity.HasIndex(e => new { e.ProjectID, e.SortOrder }).HasDatabaseName("IX_VaultProjectFileRefs_ProjectId_SortOrder");
                entity.HasIndex(e => new { e.ProjectID, e.FileRefID }).IsUnique().HasDatabaseName("UX_VaultProjectFileRefs_ProjectId_FileRefID");

                entity
                    .HasOne(e => e.FileRef)
                    .WithMany(f => f.ProjectFileRefs)
                    .HasForeignKey(e => e.FileRefID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity
                    .HasOne(e => e.Project)
                    .WithMany(p => p.ProjectFileRefs)
                    .HasForeignKey(e => e.ProjectID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -- VaultWorkflowFileRef -----------------------------------------
            modelBuilder.Entity<VaultWorkflowFileRef>(entity =>
            {
                entity.HasIndex(e => e.FileRefID).HasDatabaseName("IX_VaultWorkflowFileRefs_FileRefID");
                entity.HasIndex(e => e.WorkflowID).HasDatabaseName("IX_VaultWorkflowFileRefs_WorkflowID");
                entity.HasIndex(e => new { e.WorkflowID, e.SortOrder }).HasDatabaseName("IX_VaultWorkflowFileRefs_WorkflowId_SortOrder");
                entity.HasIndex(e => new { e.WorkflowID, e.FileRefID }).IsUnique().HasDatabaseName("UX_VaultWorkflowFileRefs_WorkflowId_FileRefID");

                entity
                    .HasOne(e => e.FileRef)
                    .WithMany(f => f.WorkflowFileRefs)
                    .HasForeignKey(e => e.FileRefID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity
                    .HasOne(e => e.Workflow)
                    .WithMany(w => w.WorkflowFileRefs)
                    .HasForeignKey(e => e.WorkflowID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -- VaultWorkflowNodeFileRef -------------------------------------
            modelBuilder.Entity<VaultWorkflowNodeFileRef>(entity =>
            {
                entity.HasIndex(e => e.FileRefID).HasDatabaseName("IX_VaultWorkflowNodeFileRefs_FileRefID");
                entity.HasIndex(e => e.WorkflowNodeID).HasDatabaseName("IX_VaultWorkflowNodeFileRefs_WorkflowNodeID");
                entity.HasIndex(e => new { e.WorkflowNodeID, e.SortOrder }).HasDatabaseName("IX_VaultWorkflowNodeFileRefs_WorkflowNodeId_SortOrder");
                entity.HasIndex(e => new { e.WorkflowNodeID, e.FileRefID }).IsUnique().HasDatabaseName("UX_VaultWorkflowNodeFileRefs_WorkflowNodeId_FileRefID");

                entity
                    .HasOne(e => e.FileRef)
                    .WithMany(f => f.WorkflowNodeFileRefs)
                    .HasForeignKey(e => e.FileRefID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity
                    .HasOne(e => e.WorkflowNode)
                    .WithMany(n => n.WorkflowNodeFileRefs)
                    .HasForeignKey(e => e.WorkflowNodeID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -- VaultWorkflowLineFileRef -------------------------------------
            modelBuilder.Entity<VaultWorkflowLineFileRef>(entity =>
            {
                entity.HasIndex(e => e.FileRefID).HasDatabaseName("IX_VaultWorkflowLineFileRefs_FileRefID");
                entity.HasIndex(e => e.WorkflowLineID).HasDatabaseName("IX_VaultWorkflowLineFileRefs_WorkflowLineID");
                entity.HasIndex(e => new { e.WorkflowLineID, e.FileOrder }).HasDatabaseName("IX_VaultWorkflowLineFileRefs_WorkflowLineId_SortOrder");
                entity.HasIndex(e => new { e.WorkflowLineID, e.FileRefID }).IsUnique().HasDatabaseName("UX_VaultWorkflowLineFileRefs_WorkflowLineId_FileRefID");

                entity
                    .HasOne(e => e.FileRef)
                    .WithMany(f => f.WorkflowLineFileRefs)
                    .HasForeignKey(e => e.FileRefID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity
                    .HasOne(e => e.WorkflowLine)
                    .WithMany(l => l.WorkflowLineFileRefs)
                    .HasForeignKey(e => e.WorkflowLineID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -- VaultWorkflowLineStepFileRef ---------------------------------
            modelBuilder.Entity<VaultWorkflowLineStepFileRef>(entity =>
            {
                entity.HasIndex(e => e.FileRefID).HasDatabaseName("IX_VaultWorkflowLineStepFileRefs_FileRefID");
                entity.HasIndex(e => e.WorkflowLineStepID).HasDatabaseName("IX_VaultWorkflowLineStepFileRefs_WorkflowLineStepID");
                entity.HasIndex(e => new { e.WorkflowLineStepID, e.SortOrder }).HasDatabaseName("IX_VaultWorkflowLineStepFileRefs_WorkflowLineStepId_SortOrder");
                entity.HasIndex(e => new { e.WorkflowLineStepID, e.FileRefID }).IsUnique().HasDatabaseName("UX_VaultWorkflowLineStepFileRefs_WorkflowLineStepId_FileRefID");

                entity
                    .HasOne(e => e.FileRef)
                    .WithMany(f => f.WorkflowLineStepFileRefs)
                    .HasForeignKey(e => e.FileRefID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity
                    .HasOne(e => e.WorkflowLineStep)
                    .WithMany(s => s.WorkflowLineStepFileRefs)
                    .HasForeignKey(e => e.WorkflowLineStepID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -- VaultSessionFileRef ------------------------------------------
            modelBuilder.Entity<VaultSessionFileRef>(entity =>
            {
                entity.HasIndex(e => e.FileRefID).HasDatabaseName("IX_VaultSessionFileRefs_FileRefID");
                entity.HasIndex(e => e.SessionID).HasDatabaseName("IX_VaultSessionFileRefs_SessionID");
                entity.HasIndex(e => new { e.SessionID, e.SortOrder }).HasDatabaseName("IX_VaultSessionFileRefs_SessionId_SortOrder");
                entity.HasIndex(e => new { e.SessionID, e.FileRefID }).IsUnique().HasDatabaseName("UX_VaultSessionFileRefs_SessionId_FileRefID");

                entity
                    .HasOne(e => e.FileRef)
                    .WithMany(f => f.SessionFileRefs)
                    .HasForeignKey(e => e.FileRefID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity
                    .HasOne(e => e.Session)
                    .WithMany(s => s.SessionFileRefs)
                    .HasForeignKey(e => e.SessionID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -- VaultNoteFileRef ---------------------------------------------
            modelBuilder.Entity<VaultNoteFileRef>(entity =>
            {
                entity.HasIndex(e => e.FileRefID).HasDatabaseName("IX_VaultNoteFileRefs_FileRefID");
                entity.HasIndex(e => e.NoteID).HasDatabaseName("IX_VaultNoteFileRefs_NoteID");
                entity.HasIndex(e => new { e.NoteID, e.SortOrder }).HasDatabaseName("IX_VaultNoteFileRefs_NoteId_SortOrder");
                entity.HasIndex(e => new { e.NoteID, e.FileRefID }).IsUnique().HasDatabaseName("UX_VaultNoteFileRefs_NoteId_FileRefID");

                entity
                    .HasOne(e => e.FileRef)
                    .WithMany(f => f.NoteFileRefs)
                    .HasForeignKey(e => e.FileRefID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity
                    .HasOne(e => e.Note)
                    .WithMany(n => n.NoteFileRefs)
                    .HasForeignKey(e => e.NoteID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -- VaultFileRefRelation -----------------------------------------
            modelBuilder.Entity<VaultFileRefRelation>(entity =>
            {
                entity.HasIndex(r => r.SourceFileRefID).HasDatabaseName("IX_VaultFileRefRelations_SourceFileRefID");
                entity.HasIndex(r => r.TargetFileRefID).HasDatabaseName("IX_VaultFileRefRelations_TargetFileRefID");
                entity.HasIndex(r => new { r.SourceFileRefID, r.SortOrder }).HasDatabaseName("IX_VaultFileRefRelations_SourceFileRefId_SortOrder");

                entity
                    .HasOne(r => r.SourceFileRef)
                    .WithMany(f => f.OutgoingFileRefRelations)
                    .HasForeignKey(r => r.SourceFileRefID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity
                    .HasOne(r => r.TargetFileRef)
                    .WithMany(f => f.IncomingFileRefRelations)
                    .HasForeignKey(r => r.TargetFileRefID)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // -- AvaProviderProfile ------------------------------------------
            modelBuilder.Entity<AvaProviderProfile>(entity =>
            {
                entity.HasIndex(p => p.ProviderType).HasDatabaseName("IX_AvaProviderProfiles_ProviderType");
                entity.HasIndex(p => p.Name).HasDatabaseName("IX_AvaProviderProfiles_Name");
                entity.HasIndex(p => p.SortOrder).HasDatabaseName("IX_AvaProviderProfiles_SortOrder");
                entity.HasIndex(p => p.IsActive).HasDatabaseName("IX_AvaProviderProfiles_IsActive");
                entity.HasIndex(p => p.IsDefault).HasDatabaseName("IX_AvaProviderProfiles_IsDefault");
                entity.HasIndex(p => p.TransportType).HasDatabaseName("IX_AvaProviderProfiles_TransportType");
            });

            // -- AvaSecret ---------------------------------------------------
            modelBuilder.Entity<AvaSecret>(entity =>
            {
                entity.HasIndex(s => s.SecretRef).IsUnique().HasDatabaseName("UX_AvaSecrets_SecretRef");
                entity.HasIndex(s => s.SecretType).HasDatabaseName("IX_AvaSecrets_SecretType");
                entity.HasIndex(s => s.IsActive).HasDatabaseName("IX_AvaSecrets_IsActive");
            });

            // -- AvaModelDefinition -------------------------------------------
            modelBuilder.Entity<AvaModelDefinition>(entity =>
            {
                entity.HasIndex(m => m.ProviderProfileId).HasDatabaseName("IX_AvaModelDefinitions_ProviderProfileId");
                entity.HasIndex(m => new { m.ProviderProfileId, m.SortOrder }).HasDatabaseName("IX_AvaModelDefinitions_ProviderProfileId_SortOrder");
                entity.HasIndex(m => new { m.ProviderProfileId, m.ModelId }).IsUnique().HasDatabaseName("UX_AvaModelDefinitions_ProviderProfileId_ModelId");
                entity.HasIndex(m => m.IsActive).HasDatabaseName("IX_AvaModelDefinitions_IsActive");
                entity.HasIndex(m => m.IsDiscovered).HasDatabaseName("IX_AvaModelDefinitions_IsDiscovered");
                entity.HasIndex(m => m.IsDefault).HasDatabaseName("IX_AvaModelDefinitions_IsDefault");

                entity
                    .HasOne(m => m.ProviderProfile)
                    .WithMany(p => p.ModelDefinitions)
                    .HasForeignKey(m => m.ProviderProfileId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -- VaultNoteVaultTag ---------------------------------------------
            modelBuilder.Entity<VaultNoteVaultTag>(entity =>
            {
                entity.HasIndex(e => e.NoteID).HasDatabaseName("IX_VaultNoteVaultTags_NoteID");
                entity.HasIndex(e => e.TagID).HasDatabaseName("IX_VaultNoteVaultTags_TagID");
                entity.HasIndex(e => new { e.NoteID, e.TagID }).IsUnique().HasDatabaseName("UX_VaultNoteVaultTags_NoteID_TagID");

                entity
                    .HasOne(e => e.Note)
                    .WithMany(n => n.VaultNoteVaultTags)
                    .HasForeignKey(e => e.NoteID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity
                    .HasOne(e => e.Tag)
                    .WithMany(t => t.VaultNoteVaultTags)
                    .HasForeignKey(e => e.TagID)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
