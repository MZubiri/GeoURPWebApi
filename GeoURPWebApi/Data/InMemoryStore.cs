using GeoURPWebApi.Models;

namespace GeoURPWebApi.Data;

public sealed class InMemoryStore
{
    public List<Role> Roles { get; } =
    [
        new Role { Id = 1, Name = "Admin", Description = "Administrador general" },
        new Role { Id = 2, Name = "Editor", Description = "Editor de contenido" }
    ];

    public List<User> Users { get; } =
    [
        new User { Id = 1, Name = "Administrador", Email = "admin@geourp.local", Password = "Admin123*", Roles = ["Admin"] },
        new User { Id = 2, Name = "Editor", Email = "editor@geourp.local", Password = "Editor123*", Roles = ["Editor"] }
    ];

    public List<BoardMember> BoardMembers { get; } =
    [
        new BoardMember { Id = 1, FullName = "Dra. Carmen Ruiz", Position = "Presidenta", PhotoUrl = "https://images.unsplash.com/photo-1438761681033-6461ffad8d80", Bio = "Especialista en gestión universitaria.", SortOrder = 1, IsActive = true },
        new BoardMember { Id = 2, FullName = "Mg. Luis Herrera", Position = "Secretario", PhotoUrl = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e", Bio = "Coordinador académico y de calidad.", SortOrder = 2, IsActive = true }
    ];

    public List<Event> Events { get; } =
    [
        new Event { Id = 1, Title = "Seminario de investigación", Description = "Presentación de avances de tesis", StartAt = DateTime.UtcNow.AddDays(5), EndAt = DateTime.UtcNow.AddDays(5).AddHours(2), Location = "Auditorio Principal", IsPublic = true }
    ];

    public List<ResearchCategory> ResearchCategories { get; } = [new ResearchCategory { Id = 1, Name = "Metodología", IsActive = true }];
    public List<ExamCategory> ExamCategories { get; } = [new ExamCategory { Id = 1, Name = "Parcial", IsActive = true }];
    public List<BookCategory> BookCategories { get; } = [new BookCategory { Id = 1, Name = "Geografía", IsActive = true }];

    public List<Research> Researches { get; } =
    [
        new Research { Id = 1, Title = "Impacto urbano", Summary = "Estudio de crecimiento urbano", FileUrl = "https://example.com/research.pdf", CategoryId = 1, PublishedAt = DateTime.UtcNow, IsActive = true }
    ];

    public List<Exam> Exams { get; } =
    [
        new Exam { Id = 1, Title = "Examen parcial 1", Description = "Evaluación de cartografía", Date = DateTime.UtcNow.AddDays(10), FileUrl = "https://example.com/exam.pdf", CategoryId = 1, IsActive = true }
    ];

    public List<Book> Books { get; } =
    [
        new Book { Id = 1, Title = "Introducción a SIG", Author = "Juan Pérez", Editorial = "GeoPress", Year = 2024, FileUrl = "https://example.com/book.pdf", CategoryId = 1, IsActive = true }
    ];

    public List<ContactMessage> ContactMessages { get; } = [];
}