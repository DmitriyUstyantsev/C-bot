//2)	Уровень доступа к данным. PatientPersonalAssistant.DAL.


using Microsoft.EntityFrameworkCore;
using PatientPersonalAssistant.DAL.Models;

namespace PatientPersonalAssistant.DAL
{
    public partial class PatientPersonalAssistantDbContext : DbContext
    {
        private readonly string connectionString;

        public PatientPersonalAssistantDbContext() { }

        public PatientPersonalAssistantDbContext(string connectionString) => this.connectionString = connectionString;

        public PatientPersonalAssistantDbContext(DbContextOptions<PatientPersonalAssistantDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<AnswerToTheQuestion> AnswerToTheQuestion { get; set; }
        public virtual DbSet<Branch> Branch { get; set; }
        public virtual DbSet<Diagnosis> Diagnosis { get; set; }
        public virtual DbSet<DiagnosisAnswerToTheQuestion> DiagnosisAnswerToTheQuestion { get; set; }
        public virtual DbSet<Question> Question { get; set; }
        public virtual DbSet<UserSurveyData> UserSurveyData { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(this.connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AnswerToTheQuestion>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnType("numeric(10, 0)")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.QuestionId).HasColumnType("numeric(10, 0)");

                entity.Property(e => e.TextOfAnswer)
                    .IsRequired()
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.HasOne(d => d.Question)
                    .WithMany(p => p.AnswerToTheQuestion)
                    .HasForeignKey(d => d.QuestionId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Branch>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnType("numeric(10, 0)")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Name)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Diagnosis>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnType("numeric(10, 0)")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.BranchId).HasColumnType("numeric(10, 0)");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.Recommendation)
                    .IsRequired()
                    .HasMaxLength(1000)
                    .IsUnicode(false);

                entity.HasOne(d => d.Branch)
                    .WithMany(p => p.Diagnosis)
                    .HasForeignKey(d => d.BranchId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<DiagnosisAnswerToTheQuestion>(entity =>
            {
                entity.ToTable("Diagnosis_AnswerToTheQuestion");

                entity.Property(e => e.Id)
                    .HasColumnType("numeric(10, 0)")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.AnswerToTheQuestionId).HasColumnType("numeric(10, 0)");

                entity.Property(e => e.DiagnosisId).HasColumnType("numeric(10, 0)");

                entity.Property(e => e.WeightOfAnswer).HasColumnType("numeric(10, 0)");

                entity.HasOne(d => d.AnswerToTheQuestion)
                    .WithMany(p => p.DiagnosisAnswerToTheQuestion)
                    .HasForeignKey(d => d.AnswerToTheQuestionId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Diagnosis)
                    .WithMany(p => p.DiagnosisAnswerToTheQuestion)
                    .HasForeignKey(d => d.DiagnosisId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Question>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnType("numeric(10, 0)")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.BranchId).HasColumnType("numeric(10, 0)");

                entity.Property(e => e.QuestionText)
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.HasOne(d => d.Branch)
                    .WithMany(p => p.Question)
                    .HasForeignKey(d => d.BranchId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<UserSurveyData>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnType("numeric(10, 0)")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.AnswerToTheQuestionId).HasColumnType("numeric(10, 0)");

                entity.Property(e => e.CodeOfEntry)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Date)
                    .IsRequired()
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.HasOne(d => d.AnswerToTheQuestion)
                    .WithMany(p => p.UserSurveyData)
                    .HasForeignKey(d => d.AnswerToTheQuestionId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}