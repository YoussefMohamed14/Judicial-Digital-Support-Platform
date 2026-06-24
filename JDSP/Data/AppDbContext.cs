using JDSP.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JDSP.Data {
        public class ApplicationDbContext : IdentityDbContext<ApplicationUser> {
            public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Case> Cases { get; set; }
        protected override void OnModelCreating(ModelBuilder builder) {
                base.OnModelCreating(builder);

                builder.Entity<ApplicationUser>()
                    .HasIndex(u => u.NationalNumber)
                    .IsUnique();
                builder.Entity<Case>()
                    .HasOne(c => c.Creator)
                    .WithMany()
                    .HasForeignKey(c => c.CreatedBy_Id)
                    .OnDelete(DeleteBehavior.Restrict); // Prevents cascade delete to avoid deleting users when a case is deleted مهمة دى
        }
        } 
}
