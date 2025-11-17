using ExaminationSystem.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace ExaminationSystem.Configurations
{
    public class ExamConfiguration : IEntityTypeConfiguration<ExaminationSystem.Models.Exam>
    {
        public void Configure(EntityTypeBuilder<ExaminationSystem.Models.Exam> builder)
        {
            builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(50);

            builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(500);
        }
    }
}
