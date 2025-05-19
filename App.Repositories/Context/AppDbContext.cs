using App.Repositories.Models;
using App.Repositories.Models.Chat;
using App.Repositories.Models.User;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace App.Repositories.Context
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // User related DbSets
        public DbSet<Tutor> Tutors { get; set; }
        public DbSet<Staff> Staffs { get; set; }

        // Main entity DbSets
        public DbSet<TutorApplication> TutorApplications { get; set; }
        public DbSet<ApplicationRevision> ApplicationRevisions { get; set; }

        public DbSet<TutorLanguage> TutorLanguages { get; set; }
        public DbSet<Blog> Blogs { get; set; }

        public DbSet<Hashtag> Hashtags { get; set; }
        public DbSet<TutorHashtag> TutorHashtags { get; set; }

        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentFileUpload> DocumentFileUploads { get; set; }
		
		public DbSet<ChatMessage> ChatMessages { get; set; }
		public DbSet<ChatConversation> ChatConversations { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseSnakeCaseNames();

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
            #endregion

            #region TutorApplication Configuration
            // TutorApplication -> Tutor (M:1)
            modelBuilder.Entity<TutorApplication>()
                .HasOne(ta => ta.Tutor)
                .WithMany()
                .HasForeignKey(ta => ta.TutorId)
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
                .WithMany()
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
                .WithMany()
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

			#region Chat Configuration
			modelBuilder.Entity<ChatMessage>(builder =>
			{
				//e.HasOne(m => m.ChatParticipant)
				// .WithMany(m => m.ChatMessages)
				// .HasForeignKey(m => m.ChatParticipantId)
				// .OnDelete(DeleteBehavior.SetNull);

				//e.HasMany(m => m.FileUploads)
				// .WithOne()
				// .HasForeignKey(m => m.Id)
				// .OnDelete(DeleteBehavior.Cascade);

				//e.HasOne(m => m.ChatConversation)
				// .WithMany(m => m.ChatMessages)
				// .HasForeignKey(m => m.ChatConversationId)
				// .OnDelete(DeleteBehavior.SetNull);

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
				//e.HasMany(c => c.ChatMessages)
				// .WithOne()
				// .HasForeignKey(m => m.ChatConversationId)
				// .OnDelete(DeleteBehavior.Cascade);

				//e.HasMany(c => c.ChatParticipants)
				// .WithOne()
				// .HasForeignKey(p => p.ChatConversationId)
				// .OnDelete(DeleteBehavior.Cascade);

				builder.HasKey(c => c.Id);

				builder.HasMany(c => c.AppUsers)
					   .WithMany()  // No explicit navigation property on AppUser for conversations
					   .UsingEntity(j => j.ToTable("UserConversations")); // Configure join table name
			});
			#endregion
		}
	}
}
