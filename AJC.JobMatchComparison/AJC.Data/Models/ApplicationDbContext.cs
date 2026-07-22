using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AJC.Data.Models;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<JobBoardProvider> JobBoardProviders { get; set; }

    public virtual DbSet<JobBoardProviderResponse> JobBoardProviderResponses { get; set; }

    public virtual DbSet<JobRetrievalWorkflowExecution> JobRetrievalWorkflowExecutions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobBoardProvider>(entity =>
        {
            entity.ToTable("JobBoardProvider");

            entity.HasIndex(e => e.IsEnabled, "IX_JobBoardProvider_IsEnabled").HasFilter("([IsEnabled]=(1))");

            entity.HasIndex(e => e.JobBoardName, "UQ_JobBoardProvider_JobBoardName").IsUnique();

            entity.Property(e => e.CreatedDate)
                .HasPrecision(3)
                .HasDefaultValueSql("(CONVERT([datetime2](3),((sysutcdatetime() AT TIME ZONE 'UTC') AT TIME ZONE 'Central Standard Time')))");
            entity.Property(e => e.CredentialReference).HasMaxLength(512);
            entity.Property(e => e.ExpectedResponseType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FeedUrl).HasMaxLength(2048);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.JobBoardName).HasMaxLength(200);
            entity.Property(e => e.ModifiedDate).HasPrecision(3);
        });

        modelBuilder.Entity<JobBoardProviderResponse>(entity =>
        {
            entity.ToTable("JobBoardProviderResponse");

            entity.HasIndex(e => new { e.JobBoardProviderId, e.RequestStartedDate }, "IX_JobBoardProviderResponse_JobBoardProviderId_RequestStartedDate").IsDescending(false, true);

            entity.HasIndex(e => new { e.WorkflowExecutionId, e.JobBoardProviderResponseId }, "IX_JobBoardProviderResponse_WorkflowExecutionId");

            entity.Property(e => e.CreatedDate)
                .HasPrecision(3)
                .HasDefaultValueSql("(CONVERT([datetime2](3),((sysutcdatetime() AT TIME ZONE 'UTC') AT TIME ZONE 'Central Standard Time')))");
            entity.Property(e => e.FailureMessage).HasMaxLength(4000);
            entity.Property(e => e.FailureType).HasMaxLength(200);
            entity.Property(e => e.JobBoardName).HasMaxLength(200);
            entity.Property(e => e.RequestCompletedDate).HasPrecision(3);
            entity.Property(e => e.RequestStartedDate).HasPrecision(3);
            entity.Property(e => e.RequestUrl).HasMaxLength(2048);
            entity.Property(e => e.ResponseContentType).HasMaxLength(255);

            entity.HasOne(d => d.JobBoardProvider).WithMany(p => p.JobBoardProviderResponses)
                .HasForeignKey(d => d.JobBoardProviderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobBoardProviderResponse_JobBoardProvider");

            entity.HasOne(d => d.WorkflowExecution).WithMany(p => p.JobBoardProviderResponses)
                .HasForeignKey(d => d.WorkflowExecutionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobBoardProviderResponse_WorkflowExecution");
        });

        modelBuilder.Entity<JobRetrievalWorkflowExecution>(entity =>
        {
            entity.HasKey(e => e.WorkflowExecutionId);

            entity.ToTable("JobRetrievalWorkflowExecution");

            entity.Property(e => e.WorkflowExecutionId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CompletedDate).HasPrecision(3);
            entity.Property(e => e.ExecutionStatus)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("Started");
            entity.Property(e => e.FailureMessage).HasMaxLength(4000);
            entity.Property(e => e.StartedDate)
                .HasPrecision(3)
                .HasDefaultValueSql("(CONVERT([datetime2](3),((sysutcdatetime() AT TIME ZONE 'UTC') AT TIME ZONE 'Central Standard Time')))");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
