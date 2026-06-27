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
        protected override void OnModelCreating(ModelBuilder builder) {
                base.OnModelCreating(builder);

                builder.Entity<ApplicationUser>()
                    .HasIndex(u => u.NationalNumber)
                    .IsUnique();
                
                //Case -> ApplicationUser (Creator)
                builder.Entity<Case>()
                    .HasOne(c => c.Creator)
                    .WithMany()
                    .HasForeignKey(c => c.CreatedBy_Id)
                    .OnDelete(DeleteBehavior.Restrict); // Prevents cascade delete to avoid deleting users when a case is deleted مهمة دى

                // CaseLawyer -> Case
                builder.Entity<CaseLawyer>()
                    .HasOne(cl => cl.Case)
                    .WithMany()
                    .HasForeignKey(cl => cl.CaseId)
                    .OnDelete(DeleteBehavior.Cascade);
                // CaseLawyer -> Lawyer 
                builder.Entity<CaseLawyer>()
                    .HasOne(cl => cl.Lawyer)
                    .WithMany()
                    .HasForeignKey(cl => cl.LawyerId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // CaseLawyerSubscription -> CaseLawyer
                builder.Entity<CaseLawyerSubscription>()
                    .HasOne(cls => cls.Caselawyer)
                    .WithMany()
                    .HasForeignKey(cls => cls.CaseLawyerId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                builder.Entity<Document>()
                    .HasOne(d => d.Case)
                    .WithMany()
                    .HasForeignKey(d => d.CaseId)
                    .OnDelete(DeleteBehavior.Cascade);
                builder.Entity<Document>()
                    .HasOne(d => d.UploadedBy)
                    .WithMany()
                    .HasForeignKey(d => d.UploadedById)
                    .OnDelete(DeleteBehavior.Restrict);
            }
        } 
}
