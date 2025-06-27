using App.Repositories.Models;
using App.Repositories.Models.Chat;
using App.Repositories.Models.Papers;
using App.Repositories.Models.Scheduling;
using App.Repositories.Models.User;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace App.Repositories.Context
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // User related DbSets
        public DbSet<Tutor> Tutors { get; set; }
        public DbSet<Learner> Learners { get; set; }
        public DbSet<Staff> Staffs { get; set; }

        // Main entity DbSets
        public DbSet<TutorApplication> TutorApplications { get; set; }
        public DbSet<ApplicationRevision> ApplicationRevisions { get; set; }
        public DbSet<HardcopySubmit> HardcopySubmits { get; set; }

        public DbSet<TutorLanguage> TutorLanguages { get; set; }
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Lesson> Lessons { get; set; }

        public DbSet<Hashtag> Hashtags { get; set; }
        public DbSet<TutorHashtag> TutorHashtags { get; set; }

        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentFileUpload> DocumentFileUploads { get; set; }

        public DbSet<WeeklyAvailabilityPattern> WeeklyAvailabilityPatterns { get; set; }
        public DbSet<BookingSlot> BookingSlots { get; set; }
        public DbSet<AvailabilitySlot> AvailabilitySlots { get; set; }
        public DbSet<BookedSlot> BookedSlots { get; set; }

        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<ChatConversation> ChatConversations { get; set; }
        public DbSet<ChatConversationReadStatus> chatConversationReadStatuses { get; set; }

        public DbSet<LearnerTimeSlotRequest> LearnerTimeSlotRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseSnakeCaseNames();
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            #region Delete Behavior
            // Cascade Delete được áp dụng khi xóa bản ghi chính sẽ xóa tất cả bản ghi phụ thuộc (vd: xóa Tutor sẽ xóa tất cả TutorLanguage)
            // SetNull áp dụng cho mối quan hệ tùy chọn (vd: xóa Staff không xóa Document nhưng sẽ đặt StaffId về null)
            // Restrict ngăn việc xóa nếu có bản ghi phụ thuộc (vd: không thể xóa Staff nếu đang có ApplicationRevision liên kết)
            #endregion

            #region Main User Configuration
            // AppUser -> Tutor (1:1)
            modelBuilder.Entity<Tutor>()
                .HasKey(s => s.UserId);

            modelBuilder.Entity<Tutor>()
                .HasOne(t => t.User)
                .WithOne()
                .HasForeignKey<Tutor>(t => t.UserId);

            // AppUser -> Staff (1:1)
            modelBuilder.Entity<Staff>()
                .HasKey(s => s.UserId);

            modelBuilder.Entity<Staff>()
                .HasOne(t => t.User)
                .WithOne()
                .HasForeignKey<Staff>(s => s.UserId);

            // AppUser -> Learner (1:1)
            modelBuilder.Entity<Learner>()
                .HasKey(l => l.UserId);

            modelBuilder.Entity<Learner>()
                .HasOne(l => l.User)
                .WithOne()
                .HasForeignKey<Learner>(l => l.UserId);
            #endregion

            #region TutorApplication Configuration
            // TutorApplication -> Tutor (M:1)
            modelBuilder.Entity<TutorApplication>()
                .HasOne(ta => ta.Tutor)
                .WithMany()
                .HasForeignKey(ta => ta.TutorId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TutorApplication>()
                .HasMany(ta => ta.Documents)
                .WithOne(doc => doc.Application)
                .HasForeignKey(ta => ta.Id)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TutorApplication>()
                .HasMany(ta => ta.ApplicationRevisions)
                .WithOne(rev => rev.Application)
                .HasForeignKey(ta => ta.Id)
                .OnDelete(DeleteBehavior.SetNull);
            #endregion

            #region Document Configuration
            // Document -> TutorApplication (M:1)
            modelBuilder.Entity<Document>()
                .HasOne(d => d.Application)
                .WithMany()
                .HasForeignKey(d => d.ApplicationId)
                .OnDelete(DeleteBehavior.SetNull);

            // Document -> Staff (M:1) optional relationship
            modelBuilder.Entity<Document>()
                .HasOne(d => d.Staff)
                .WithMany()
                .HasForeignKey(d => d.StaffId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
            #endregion

            #region ApplicationRevision Configuration
            // ApplicationRevision -> TutorApplication (M:1)
            modelBuilder.Entity<ApplicationRevision>()
                .HasOne(ar => ar.Application)
                .WithMany()
                .HasForeignKey(ar => ar.ApplicationId)
                .OnDelete(DeleteBehavior.SetNull);

            // ApplicationRevision -> Staff (M:1)
            modelBuilder.Entity<ApplicationRevision>()
                .HasOne(ar => ar.Staff)
                .WithMany()
                .HasForeignKey(ar => ar.StaffId)
                .OnDelete(DeleteBehavior.SetNull);
            #endregion

            #region TutorLanguage Configuration
            // TutorLanguage -> Tutor (M:1)
            modelBuilder.Entity<TutorLanguage>()
                .HasOne(tl => tl.Tutor)
                .WithMany(t => t.Languages)
                .HasForeignKey(tl => tl.TutorId)
                .OnDelete(DeleteBehavior.Cascade);
            #endregion

            #region TutorHashtag Configuration
            // TutorHashtag composite key
            modelBuilder.Entity<TutorHashtag>()
                .HasKey(th => new { th.TutorId, th.HashtagId });

            // TutorHashtag -> Tutor (M:1) - CASCADE DELETE
            modelBuilder.Entity<TutorHashtag>()
                .HasOne(th => th.Tutor)
                .WithMany(t => t.Hashtags)
                .HasForeignKey(th => th.TutorId)
                .OnDelete(DeleteBehavior.Cascade);

            // TutorHashtag -> Hashtag (M:1) - CASCADE DELETE
            modelBuilder.Entity<TutorHashtag>()
                .HasOne(th => th.Hashtag)
                .WithMany()
                .HasForeignKey(th => th.HashtagId)
                .OnDelete(DeleteBehavior.Cascade);
            #endregion

            #region DocumentFileUpload Configuration
            // Composite key
            modelBuilder.Entity<DocumentFileUpload>()
                .HasKey(dfu => new { dfu.DocumentId, dfu.FileUploadId });

            // DocumentFileUpload -> Document (M:1) - CASCADE DELETE
            modelBuilder.Entity<DocumentFileUpload>()
                .HasOne(dfu => dfu.Document)
                .WithMany(d => d.DocumentFileUploads)
                .HasForeignKey(dfu => dfu.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            // DocumentFileUpload -> FileUpload (M:1) - RESTRICT DELETE
            modelBuilder.Entity<DocumentFileUpload>()
                .HasOne(dfu => dfu.FileUpload)
                .WithMany()
                .HasForeignKey(dfu => dfu.FileUploadId)
                .OnDelete(DeleteBehavior.Restrict);
            #endregion

            #region HardcopySubmit Configuration
            // HardcopySubmit -> TutorApplication (M:1)
            modelBuilder.Entity<HardcopySubmit>()
                .HasOne(hs => hs.Application)
                .WithMany()
                .HasForeignKey(hs => hs.ApplicationId)
                .OnDelete(DeleteBehavior.SetNull);

            // HardcopySubmit -> Documents (1:N)
            modelBuilder.Entity<HardcopySubmit>()
                .HasMany(hs => hs.Documents)
                .WithOne(d => d.HardcopySubmit)
                .HasForeignKey(d => d.HardcopySubmitId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
            #endregion

            #region Scheduling Configuration
            // WeeklyAvailabilityPattern -> Tutor (M:1)
            modelBuilder.Entity<WeeklyAvailabilityPattern>()
                .HasOne(w => w.Tutor)
                .WithMany(t => t.AvailabilityPatterns)
                .HasForeignKey(w => w.TutorId)
                .OnDelete(DeleteBehavior.Cascade);

            // BookingSlot -> Tutor (M:1)
            modelBuilder.Entity<BookingSlot>()
                .HasOne(b => b.Tutor)
                .WithMany(t => t.BookingSlots)
                .HasForeignKey(b => b.TutorId)
                .OnDelete(DeleteBehavior.Cascade);

            // BookingSlot -> Learner (M:1) optional
            modelBuilder.Entity<BookingSlot>()
                .HasOne(b => b.Learner)
                .WithMany(l => l.BookingSlots)
                .HasForeignKey(b => b.LearnerId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // AvailabilitySlot relationships
            modelBuilder.Entity<AvailabilitySlot>()
                .HasOne(a => a.WeeklyPattern)
                .WithMany(w => w.Slots)
                .HasForeignKey(a => a.WeeklyPatternId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // BookedSlot relationships
            modelBuilder.Entity<BookedSlot>()
                .HasOne(bs => bs.BookingSlot)
                .WithMany(bs => bs.BookedSlots)
                .HasForeignKey(bs => bs.BookingSlotId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BookedSlot>()
                .HasOne(bs => bs.AvailabilitySlot)
                .WithMany()
                .HasForeignKey(bs => bs.AvailabilitySlotId)
                .OnDelete(DeleteBehavior.Cascade);
            #endregion

            #region Chat Configuration
            modelBuilder.Entity<ChatMessage>(builder =>
            {
                builder.HasKey(m => m.Id);

                builder.Property(m => m.AppUserId)
                       .IsRequired();

                builder.Property(m => m.ChatConversationId)
                       .IsRequired();

                builder.Property(m => m.TextMessage)
                       .IsRequired(false);

                builder.HasOne(m => m.AppUser)
                       .WithMany()  // No explicit navigation property on AppUser for messages
                       .HasForeignKey(m => m.AppUserId)
                       .OnDelete(DeleteBehavior.Restrict); // Prevent cascade deletion

                // Relationship with ChatConversation
                builder.HasOne(m => m.ChatConversation)
                       .WithMany(c => c.ChatMessages)
                       .HasForeignKey(m => m.ChatConversationId)
                       .OnDelete(DeleteBehavior.Cascade); // Messages deleted when conversation is deleted
            });

            modelBuilder.Entity<ChatConversation>(builder =>
            {
                builder.HasKey(c => c.Id);

                builder.HasMany(c => c.AppUsers)
                       .WithMany()// No explicit navigation property on AppUser for conversations	
                       .UsingEntity(j => j.ToTable("user_conversations")); // Configure join table name
            });

            modelBuilder.Entity<ChatConversationReadStatus>(builder =>
            {
                builder.HasKey(m => m.Id);

                builder.HasOne(m => m.ChatConversation)
                .WithMany(m => m.ChatConversationReadStatus)
                .HasForeignKey(m => m.ChatConversationId)
                .OnDelete(DeleteBehavior.SetNull);

                builder.HasOne(m => m.LastReadChatMessage)
                .WithMany(m => m.ChatConversationReadStatuses)
                .HasForeignKey(m => m.LastReadChatMessageId)
                .OnDelete(DeleteBehavior.SetNull);

                builder.HasOne(m => m.AppUser)
                .WithMany(m => m.ChatConversationReadStatuses)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            });
            #endregion

            #region Lesson Configuration
            // Lesson -> Tutor (M:1)
            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Tutor)
                .WithMany(t => t.Lessons)
                .HasForeignKey(l => l.TutorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Lesson>()
                .Property(l => l.Price)
                .HasColumnType("decimal(18, 2)");
            #endregion

            #region LearnerTimeSlotRequest Configuration
            modelBuilder.Entity<LearnerTimeSlotRequest>(builder =>
            {
                builder.HasKey(lts => lts.Id);

                // Relationship with Learner
                builder.HasOne(lts => lts.Learner)
                    .WithMany(l => l.TimeSlotRequests)
                    .HasForeignKey(lts => lts.LearnerId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relationship with Tutor
                builder.HasOne(lts => lts.Tutor)
                    .WithMany(t => t.TimeSlotRequests)
                    .HasForeignKey(lts => lts.TutorId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Unique constraint to prevent duplicate requests
                builder.HasIndex(lts => new { lts.LearnerId, lts.TutorId, lts.DayInWeek, lts.SlotIndex })
                    .IsUnique();
            });
            #endregion
        }
    }
}
