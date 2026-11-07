using Library.Models;
using Library.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Library.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookCopy> BookCopies => Set<BookCopy>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<Fine> Fines => Set<Fine>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUser(modelBuilder);
        ConfigureAuthor(modelBuilder);
        ConfigureCategory(modelBuilder);
        ConfigureBook(modelBuilder);
        ConfigureBookCopy(modelBuilder);
        ConfigureLoan(modelBuilder);
        ConfigureFine(modelBuilder);
        ConfigureReservation(modelBuilder);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Role).HasConversion(UpperEnumConverter<UserRole>());
            e.HasIndex(x => x.Email).IsUnique();
        });
    }

    private static void ConfigureAuthor(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Author>(e =>
        {
            e.ToTable("authors");
            e.HasKey(x => x.Id);
        });
    }

    private static void ConfigureCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("categories");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
        });
    }

    private static void ConfigureBook(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(e =>
        {
            e.ToTable("books");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Isbn).IsUnique();

            e.HasMany(x => x.Authors)
                .WithMany(x => x.Books)
                .UsingEntity(j => j.ToTable("book_authors"));

            e.HasMany(x => x.Categories)
                .WithMany(x => x.Books)
                .UsingEntity(j => j.ToTable("book_categories"));
        });
    }

    private static void ConfigureBookCopy(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BookCopy>(e =>
        {
            e.ToTable("book_copies");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion(UpperEnumConverter<BookCopyStatus>());
            e.Property(x => x.Condition).HasConversion(UpperEnumConverter<BookCopyCondition>());
            e.HasIndex(x => x.InventoryNumber).IsUnique();

            e.HasOne(x => x.Book).WithMany(x => x.Copies).HasForeignKey(x => x.BookId);
        });
    }

    private static void ConfigureLoan(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Loan>(e =>
        {
            e.ToTable("loans");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion(UpperEnumConverter<LoanStatus>());

            e.HasOne(x => x.User).WithMany(x => x.Loans).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Book).WithMany(x => x.Loans).HasForeignKey(x => x.BookId);
            e.HasOne(x => x.Copy).WithMany().HasForeignKey(x => x.CopyId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureFine(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Fine>(e =>
        {
            e.ToTable("fines");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion(UpperEnumConverter<FineStatus>());
            e.Property(x => x.Reason).HasConversion(UpperEnumConverter<FineReason>());

            e.HasOne(x => x.User).WithMany(x => x.Fines).HasForeignKey(x => x.UserId);
            // One fine per loan: loan_id is unique.
            e.HasOne(x => x.Loan).WithOne(x => x.Fine).HasForeignKey<Fine>(x => x.LoanId);
        });
    }

    private static void ConfigureReservation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Reservation>(e =>
        {
            e.ToTable("reservations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion(UpperEnumConverter<ReservationStatus>());

            e.HasOne(x => x.User).WithMany(x => x.Reservations).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Book).WithMany(x => x.Reservations).HasForeignKey(x => x.BookId);
            e.HasOne(x => x.Copy).WithMany().HasForeignKey(x => x.CopyId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static ValueConverter<TEnum, string> UpperEnumConverter<TEnum>() where TEnum : struct, Enum =>
        new(v => v.ToString().ToUpperInvariant(), v => Enum.Parse<TEnum>(v, true));
}
