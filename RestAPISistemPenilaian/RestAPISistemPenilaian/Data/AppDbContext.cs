using Microsoft.EntityFrameworkCore;
using RestAPISistemPenilaian.Models.Entities;

namespace RestAPISistemPenilaian.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<MenuRole> MenuRoles { get; set; }
        public DbSet<MasterKriteria> MasterKriterias { get; set; }
        public DbSet<MasterBobotAHP> MasterBobotAHPs { get; set; }
        public DbSet<MasterBobotAHPKriteria> MasterBobotAHPKriterias { get; set; }
        public DbSet<MasterKategori> MasterKategoris { get; set; }
        public DbSet<MasterKategoriDetail> MasterKategoriDetails { get; set; }
        public DbSet<Periode> Periodes { get; set; }
        public DbSet<Penilaian360> Penilaian360s { get; set; }
        public DbSet<HasilPenilaianSistem> HasilPenilaianSistems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // MasterKriteria unique constraint
            modelBuilder.Entity<MasterKriteria>()
                .HasIndex(k => k.KodeKriteria)
                .IsUnique();

            // User unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.KodeKaryawan)
                .IsUnique();

            // Periode unique constraint
            modelBuilder.Entity<Periode>()
                .HasIndex(p => p.KodePeriode)
                .IsUnique();

            // MasterBobotAHP unique constraint
            modelBuilder.Entity<MasterBobotAHP>()
                .HasIndex(m => m.KodeAturan)
                .IsUnique();

            // MasterKategori unique constraint
            modelBuilder.Entity<MasterKategori>()
                .HasIndex(k => k.KodeKategori)
                .IsUnique();

            // Penilaian360 unique constraint
            modelBuilder.Entity<Penilaian360>()
                .HasIndex(p => p.KodePenilaian)
                .IsUnique();

            // HasilPenilaianSistem unique constraint
            modelBuilder.Entity<HasilPenilaianSistem>()
                .HasIndex(h => h.KodeHasil)
                .IsUnique();
        }
    }
}
