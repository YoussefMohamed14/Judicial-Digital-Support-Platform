using JDSP.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JDSP.Data {
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser> {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Case> Cases { get; set; }
        public DbSet<CaseLawyer> CaseLawyers { get; set; }
        public DbSet<CaseLawyerSubscription> CaseLawyerSubscriptions { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<LawyerProfile> LawyerProfiles { get; set; }
        public DbSet<LawyerFollow> LawyerFollows { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Hearing> Hearings { get; set; }
        public DbSet<LegalServiceRequest> LegalServiceRequests { get; set; }
        public DbSet<PublicRequestProposal> PublicRequestProposals { get; set; }

        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>()
                .HasIndex(u => u.NationalNumber)
                .IsUnique();

            // CreatedBy_Id stores the client who owns the case. Only a Court Employee can create it.
            builder.Entity<Case>()
                .HasOne(c => c.Creator)
                .WithMany()
                .HasForeignKey(c => c.CreatedBy_Id)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CaseLawyer>()
                .HasOne(cl => cl.Case)
                .WithMany()
                .HasForeignKey(cl => cl.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CaseLawyer>()
                .HasOne(cl => cl.Lawyer)
                .WithMany()
                .HasForeignKey(cl => cl.LawyerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CaseLawyerSubscription>()
                .HasOne(cls => cls.Caselawyer)
                .WithMany()
                .HasForeignKey(cls => cls.CaseLawyerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<LawyerProfile>()
                .HasIndex(x => x.UserId)
                .IsUnique();

            builder.Entity<LawyerFollow>()
                .HasIndex(x => new { x.FollowerId, x.LawyerId })
                .IsUnique();

            builder.Entity<LawyerFollow>()
                .HasOne(x => x.Follower)
                .WithMany()
                .HasForeignKey(x => x.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LawyerFollow>()
                .HasOne(x => x.Lawyer)
                .WithMany()
                .HasForeignKey(x => x.LawyerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LegalServiceRequest>()
                .HasOne(x => x.Client)
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LegalServiceRequest>()
                .HasOne(x => x.Lawyer)
                .WithMany()
                .HasForeignKey(x => x.LawyerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LegalServiceRequest>()
                .HasIndex(x => new { x.RequestType, x.Status, x.CreatedAt });

            builder.Entity<PublicRequestProposal>()
                .HasOne(x => x.Request)
                .WithMany(x => x.Proposals)
                .HasForeignKey(x => x.LegalServiceRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PublicRequestProposal>()
                .HasOne(x => x.Lawyer)
                .WithMany()
                .HasForeignKey(x => x.LawyerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PublicRequestProposal>()
                .HasIndex(x => new { x.LegalServiceRequestId, x.LawyerId })
                .IsUnique();
        }
    }
}
