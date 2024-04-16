//2) Уровень доступа к данным. PatientPersonalAssistant.DAL.

using Microsoft.EntityFrameworkCore;
using PatientPersonalAssistant.BAL.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using PatientPersonalAssistant.DAL.Models;

namespace PatientPersonalAssistant.DAL.Models
{
    public partial class PatientPersonalAssistantDbContext : DbContext
    {
        private readonly string connectionString;
        private DbSet<DiagnosisAnswerToTheQuestion> diagnosisAnswerToTheQuestion;

        public PatientPersonalAssistantDbContext(string connectionString) => this.connectionString = connectionString;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(this.connectionString);
            }
        }

        public virtual DbSet<AnswerToTheQuestion> AnswerToTheQuestion { get; set; }
        public virtual DbSet<Branch> Branch { get; set; }
        public virtual DbSet<Diagnosis> Diagnosis { get; set; }
        public virtual DbSet<DiagnosisAnswerToTheQuestion> DiagnosisAnswerToTheQuestion { get; set; }
        public virtual DbSet<Question> Question { get; set; }
        public virtual DbSet<UserSurveyData> UserSurveyData { get; set; }
        public virtual DbSet<DataFromKnowledgeBase> DataFromKnowledgeBase { get; set; } // Added DataFromKnowledgeBase DbSet

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AnswerToTheQuestion>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnType("numeric(10, 0)")
                    .ValueGeneratedOnAdd();

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

                entity.HasMany(d => d.Question)
                    .WithOne(p => p.Branch)
                    .HasForeignKey(d => d.BranchId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Diagnosis>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnType("numeric(10, 0)")
                    .ValueGeneratedOnAdd();

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
                entity.ToTable("DiagnosisAnswerToTheQuestion");

                entity.Property(e => e.Id)
                    .HasColumnType("numeric(10, 0)")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.WeightOfAnswer).HasColumnType("numeric(10, 0)");

                entity.HasOne(d => d.AnswerToTheQuestion)
                    .WithMany(p => p.DiagnosisAnswerToTheQuestion)
                    .HasForeignKey(d => d.AnswerToTheQuestionId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Question>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnType("numeric(10, 0)")
                    .ValueGeneratedOnAdd();

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
                entity.Property(e => e.CodeOfEntry)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.HasOne(d => d.AnswerToTheQuestion)
                    .WithMany(p => p.UserSurveyData)
                    .HasForeignKey(d => d.AnswerToTheQuestionId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<DataFromKnowledgeBase>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnType("numeric(10, 0)")
                    .ValueGeneratedOnAdd();

                entity.HasKey(d => d.PrimaryKeyProperty);
                OnModelCreatingPartial(modelBuilder);
            });

        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }

    internal class DiagnosisAnswerToTheQuestion
    {
    }
}


public partial class AnswerToTheQuestion
{
    public AnswerToTheQuestion()
    {
        DiagnosisAnswerToTheQuestion = new HashSet<DiagnosisAnswerToTheQuestion>();
        UserSurveyData = new HashSet<UserSurveyData>();
    }

    public decimal Id { get; set; }
    public string TextOfAnswer { get; set; }
    public decimal QuestionId { get; set; }
    public virtual Question Question { get; set; }
    public virtual ICollection<DiagnosisAnswerToTheQuestion> DiagnosisAnswerToTheQuestion { get; set; }
    public virtual ICollection<UserSurveyData> UserSurveyData { get; set; }
}

public partial class Branch
{
    public Branch()
    {
        Diagnosis = new HashSet<Diagnosis>();
        Question = new HashSet<Question>();
    }

    public decimal Id { get; set; }
    public string Name { get; set; }
    public decimal TotalWeightOfBranchOfQuestion { get; set; }

    public virtual ICollection<Diagnosis> Diagnosis { get; set; }
    public virtual ICollection<Question> Question { get; set; }
}

public partial class Diagnosis
{
    public Diagnosis()
    {
        DiagnosisAnswerToTheQuestion = new HashSet<DiagnosisAnswerToTheQuestion>();
    }

    public decimal Id { get; set; }

    public string Name { get; set; }
    public string Recommendation { get; set; }
    public decimal BranchId { get; set; }

    public virtual Branch Branch { get; set; }
    public virtual ICollection<DiagnosisAnswerToTheQuestion> DiagnosisAnswerToTheQuestion { get; set; }
}

public partial class DataFromKnowledgeBase
{
    // Add properties for DataFromKnowledgeBase entity if needed
}

public partial class UserSurveyData
{
    public decimal Id { get; set; }
    public decimal AnswerToTheQuestionId { get; set; }
    public string CodeOfEntry { get; set; }
    public string Date { get; set; }

    public virtual AnswerToTheQuestion AnswerToTheQuestion { get; set; }
    public virtual AnswerToTheQuestion AnswerToTheQuestion { get; set; }
}
public virtual AnswerToTheQuestion AnswerToTheQuestion { get; set; }
    }
}

public partial class Question
{
    public Question()
    {
        AnswerToTheQuestion = new HashSet<AnswerToTheQuestion>();
    }

    public decimal Id { get; set; }
    public string QuestionText { get; set; }
    public decimal BranchId { get; set; }

    public virtual Branch Branch { get; set; }
    public virtual ICollection<AnswerToTheQuestion> AnswerToTheQuestion { get; set; }
}

namespace PatientPersonalAssistant.DAL
{
    public interface ITelegramBotRepository
    {
        Task<QuestionWithAnswers> GetQuestionWithAnswersByQuestionIdAsync(decimal Id);
        Task AddToUserSurveyDataAsync(UserSurveyDataBAL userSurvey);
        Task<IEnumerable<QuestionWithAnswers>> GetQuestionsWithAnswersByBranchIdAsync(decimal branchCode);
        Task<IEnumerable<decimal>> GetUserSurveyDataByEntryCodeAsync(string entryCode);
        Task<IEnumerable<DataFromKnowledgeBase>> GetDataFromKnowledgeBaseByBranchIdAsync(decimal branchCode);
        Task<IEnumerable<BranchBAL>> GetAllBranchesAsync();
        Task<decimal> GetBranchWeightByBranchIdAsync(decimal branchId);
        Task<string> GetTextOfAnswerByIdAsync(string textOfAnswerId);
        Task<string> GetBranchNameByIdAsync(string branchId);
    }
}