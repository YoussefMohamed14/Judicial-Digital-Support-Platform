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
        public DbSet<LawyerVerificationRequest> LawyerVerificationRequests { get; set; }
        public DbSet<IdentityChangeRequest> IdentityChangeRequests { get; set; }
        public DbSet<SystemNotification> SystemNotifications { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<OfficialCaseRequest> OfficialCaseRequests { get; set; }

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
                .HasOne(x => x.Case)
                .WithMany()
                .HasForeignKey(x => x.CaseId)
                .OnDelete(DeleteBehavior.SetNull);

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

            builder.Entity<LawyerVerificationRequest>()
                .HasOne(x => x.Lawyer)
                .WithMany()
                .HasForeignKey(x => x.LawyerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LawyerVerificationRequest>()
                .HasOne(x => x.ReviewedBy)
                .WithMany()
                .HasForeignKey(x => x.ReviewedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LawyerVerificationRequest>()
                .HasIndex(x => new { x.Status, x.RequestedAt });


            builder.Entity<IdentityChangeRequest>()
                .HasOne(x => x.RequestedBy)
                .WithMany()
                .HasForeignKey(x => x.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<IdentityChangeRequest>()
                .HasOne(x => x.ReviewedBy)
                .WithMany()
                .HasForeignKey(x => x.ReviewedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<IdentityChangeRequest>()
                .HasIndex(x => new { x.Status, x.RequestedAt });

            builder.Entity<SystemNotification>()
                .HasOne(x => x.Recipient)
                .WithMany()
                .HasForeignKey(x => x.RecipientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SystemNotification>()
                .HasIndex(x => new { x.RecipientId, x.IsRead, x.CreatedAt });


            builder.Entity<Payment>()
                .HasOne(x => x.RequestedByLawyer)
                .WithMany()
                .HasForeignKey(x => x.RequestedByLawyerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Payment>()
                .HasIndex(x => new { x.CaseId, x.Status, x.RequestedAt });

            builder.Entity<Payment>()
                .HasIndex(x => new { x.RequestedByLawyerId, x.Status, x.LawyerPayoutStatus });

            builder.Entity<OfficialCaseRequest>()
                .HasOne(x => x.Case)
                .WithMany()
                .HasForeignKey(x => x.CaseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OfficialCaseRequest>()
                .HasOne(x => x.Lawyer)
                .WithMany()
                .HasForeignKey(x => x.LawyerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OfficialCaseRequest>()
                .HasOne(x => x.ReviewedBy)
                .WithMany()
                .HasForeignKey(x => x.ReviewedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OfficialCaseRequest>()
                .HasIndex(x => new { x.Status, x.RequestedAt });

            builder.Entity<OfficialCaseRequest>()
                .HasIndex(x => new { x.CaseId, x.LawyerId, x.Status });

            builder.Entity<ChatMessage>()
                .HasOne(x => x.Sender)
                .WithMany()
                .HasForeignKey(x => x.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ChatMessage>()
                .HasOne(x => x.Receiver)
                .WithMany()
                .HasForeignKey(x => x.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ChatMessage>()
                .HasOne(x => x.RelatedCase)
                .WithMany()
                .HasForeignKey(x => x.RelatedCaseId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ChatMessage>()
                .HasOne(x => x.Payment)
                .WithMany()
                .HasForeignKey(x => x.PaymentId)
                // SQL Server blocks multiple cascade paths here because Payments already cascades from Cases.
                // Keep payment-request chat history stable and prevent payment deletion while a message references it.
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ChatMessage>()
                .HasIndex(x => new { x.SenderId, x.ReceiverId, x.CreatedAt });

            builder.Entity<ChatMessage>()
                .HasIndex(x => new { x.ReceiverId, x.IsRead, x.CreatedAt });

        }
    }
}
