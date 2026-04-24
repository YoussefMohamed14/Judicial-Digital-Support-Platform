using Microsoft.EntityFrameworkCore;

namespace JDSP.Models {
    public class AppDbContext : DbContext{
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    }
}
