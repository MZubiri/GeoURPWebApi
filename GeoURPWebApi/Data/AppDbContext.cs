using GeoURPWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace GeoURPWebApi.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<BoardMember> BoardMembers => Set<BoardMember>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<ResearchCategory> ResearchCategories => Set<ResearchCategory>();
    public DbSet<Research> Researches => Set<Research>();
    public DbSet<ExamCategory> ExamCategories => Set<ExamCategory>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<BookCategory> BookCategories => Set<BookCategory>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().ToTable("Users").Ignore(x => x.Roles);
        modelBuilder.Entity<Role>().ToTable("Roles");

        modelBuilder.Entity<UserRole>()
            .ToTable("UserRoles")
            .HasKey(x => new { x.UserId, x.RoleId });

        modelBuilder.Entity<UserRole>()
            .HasOne(x => x.User)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.UserId);

        modelBuilder.Entity<UserRole>()
            .HasOne(x => x.Role)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.RoleId);

        modelBuilder.Entity<BoardMember>().ToTable("BoardMembers");
        modelBuilder.Entity<Event>().ToTable("Events");
        modelBuilder.Entity<ResearchCategory>().ToTable("ResearchCategories");
        modelBuilder.Entity<Research>().ToTable("Researches");
        modelBuilder.Entity<ExamCategory>().ToTable("ExamCategories");
        modelBuilder.Entity<Exam>().ToTable("Exams");
        modelBuilder.Entity<BookCategory>().ToTable("BookCategories");
        modelBuilder.Entity<Book>().ToTable("Books");
        modelBuilder.Entity<ContactMessage>().ToTable("ContactMessages");
    }
}
