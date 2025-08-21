using App.Repositories.Models;
using App.Repositories.Models.Chat;
using App.Repositories.Models.Legal;
using App.Repositories.Models.Notifications;
using App.Repositories.Models.Papers;
using App.Repositories.Models.Payment;
using App.Repositories.Models.Rating;
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
        public DbSet<Manager> Managers { get; set; }

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
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<AvailabilitySlot> AvailabilitySlots { get; set; }
        public DbSet<BookedSlot> BookedSlots { get; set; }
        public DbSet<BookingSlotRating> BookingSlotRatings { get; set; }
        public DbSet<TutorBookingOffer> TutorBookingOffers { get; set; }
        public DbSet<OfferedSlot> OfferedSlots { get; set; }

        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<ChatConversation> ChatConversations { get; set; }
        public DbSet<ChatConversationReadStatus> chatConversationReadStatuses { get; set; }

        public DbSet<LearnerTimeSlotRequest> LearnerTimeSlotRequests { get; set; }

        // Payment related DbSets
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<HeldFund> HeldFunds { get; set; }
        public DbSet<DepositRequest> DepositRequests { get; set; }
        public DbSet<WithdrawalRequest> WithdrawalRequests { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<FeeConfig> FeeConfigs { get; set; }

        // Legal Documents
        public DbSet<LegalDocument> LegalDocuments { get; set; }
        public DbSet<LegalDocumentVersion> LegalDocumentVersions { get; set; }
        public DbSet<LegalDocumentAcceptance> LegalDocumentAcceptances { get; set; }
        public DbSet<LessonSnapshot> LessonSnapshots { get; set; }

        public DbSet<NotificationEntity> NotificationEntities { get; set; }
        public DbSet<TutorIntroductionVideo> TutorIntroductionVideos { get; set; }

        public DbSet<BookingDispute> BookingDisputes { get; set; }
        public DbSet<RescheduleRequest> RescheduleRequests { get; set; }
        public DbSet<BookingConfig> BookingConfigs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseSnakeCaseNames();
            //AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

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

            // Tutor -> TutorApplication (1:1)
            modelBuilder.Entity<Tutor>()
                .HasOne(t => t.Application)
                .WithOne()
                .HasForeignKey<TutorApplication>(ta => ta.TutorId);

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

            // AppUser -> Manager (1:1)
            modelBuilder.Entity<Manager>()
                .HasKey(m => m.UserId);

            modelBuilder.Entity<Manager>()
                .HasOne(m => m.User)
                .WithOne()
                .HasForeignKey<Manager>(m => m.UserId);
            #endregion

            #region TutorApplication Configuration
            modelBuilder.Entity<TutorApplication>()
                .HasIndex(ta => ta.TutorId)
                .IsUnique();

            modelBuilder.Entity<TutorApplication>()
                .HasMany(ta => ta.Documents)
                .WithOne(doc => doc.Application)
                .HasForeignKey(doc => doc.ApplicationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TutorApplication>()
                .HasMany(ta => ta.ApplicationRevisions)
                .WithOne(rev => rev.Application)
                .HasForeignKey(rev => rev.ApplicationId)
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

            // Booking -> Tutor (M:1)
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Tutor)
                .WithMany(t => t.Bookings)
                .HasForeignKey(b => b.TutorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Booking -> Learner (M:1) optional
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Learner)
                .WithMany(l => l.Bookings)
                .HasForeignKey(b => b.LearnerId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Booking>()
                .HasOne(bs => bs.BookingSlotRating)
                .WithOne(br => br.Booking)
                .OnDelete(DeleteBehavior.SetNull);

            // AvailabilitySlot relationships
            modelBuilder.Entity<AvailabilitySlot>()
                .HasOne(a => a.WeeklyPattern)
                .WithMany(w => w.Slots)
                .HasForeignKey(a => a.WeeklyPatternId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // BookingSlot relationships
            modelBuilder.Entity<BookedSlot>()
                .HasOne(bs => bs.Booking)
                .WithMany(bs => bs.BookedSlots)
                .HasForeignKey(bs => bs.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // modelBuilder.Entity<BookedSlot>()
            //     .HasOne(bs => bs.AvailabilitySlot)
            //     .WithMany()
            //     .HasForeignKey(bs => bs.AvailabilitySlotId)
            //     .OnDelete(DeleteBehavior.Cascade);
            #endregion

            #region TutorBookingOffer Configuration
            modelBuilder.Entity<TutorBookingOffer>(builder =>
            {
                builder.HasKey(o => o.Id);

                builder.HasOne(o => o.Tutor)
                    .WithMany()
                    .HasForeignKey(o => o.TutorId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasOne(o => o.Learner)
                    .WithMany()
                    .HasForeignKey(o => o.LearnerId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasOne(o => o.Lesson)
                    .WithMany()
                    .HasForeignKey(o => o.LessonId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);

                builder.HasMany(o => o.OfferedSlots)
                    .WithOne(s => s.TutorBookingOffer)
                    .HasForeignKey(s => s.TutorBookingOfferId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OfferedSlot>(builder =>
            {
                builder.HasKey(s => s.Id);
            });
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

                // Relationship with Lesson
                builder.HasOne(lts => lts.Lesson)
                    .WithMany()
                    .HasForeignKey(lts => lts.LessonId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);

                // Unique constraint to prevent duplicate requests
                builder.HasIndex(lts => new { lts.LearnerId, lts.TutorId })
                    .IsUnique();
            });
            #endregion

            #region Rating Configuration
            // 1 BookingSlotRating => 1 Booking
            modelBuilder.Entity<BookingSlotRating>()
                .HasOne(br => br.Booking)
                .WithOne(bs => bs.BookingSlotRating)
                .OnDelete(DeleteBehavior.SetNull);

            // 1 BookingSlotRating => 1 Tutor
            modelBuilder.Entity<BookingSlotRating>()
                .HasOne(br => br.Tutor)
                .WithMany(t => t.BookingSlotRatings)
                .HasForeignKey(br => br.TutorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BookingSlotRating>()
                .HasOne(br => br.Learner)
                .WithMany(l => l.BookingSlotRatings)
                .HasForeignKey(br => br.LearnerId)
                .OnDelete(DeleteBehavior.SetNull);
            #endregion

            #region LegalDocument Configuration
            // Cấu hình được cải tiến để đảm bảo toàn vẹn dữ liệu cho các tài liệu pháp lý.

            // LegalDocument -> LegalDocumentVersion (1-M)
            // Khi một tài liệu gốc bị xóa, tất cả các phiên bản của nó cũng sẽ bị xóa theo.
            modelBuilder.Entity<LegalDocumentVersion>()
                .HasOne(version => version.LegalDocument)
                .WithMany(doc => doc.Versions)
                .HasForeignKey(version => version.LegalDocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            // LegalDocumentVersion -> LegalDocumentAcceptance (1-M)
            // Ngăn chặn việc xóa một phiên bản tài liệu nếu đã có người dùng chấp thuận nó.
            // Điều này quan trọng cho việc kiểm toán và lưu trữ lịch sử.
            modelBuilder.Entity<LegalDocumentAcceptance>()
                .HasOne(acceptance => acceptance.LegalDocumentVersion)
                .WithMany(version => version.LegalDocumentAcceptances)
                .HasForeignKey(acceptance => acceptance.LegalDocumentVersionId)
                .OnDelete(DeleteBehavior.Restrict);

            // AppUser -> LegalDocumentAcceptance (1-M)
            // Khi một người dùng bị xóa, các bản ghi chấp thuận của họ cũng sẽ bị xóa.
            modelBuilder.Entity<LegalDocumentAcceptance>()
                .HasOne(acceptance => acceptance.AppUser)
                .WithMany(user => user.LegalDocumentAcceptances)
                .HasForeignKey(acceptance => acceptance.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // LegalDocument -> LegalDocumentAcceptance (1-M)
            // Ngăn chặn việc xóa tài liệu gốc nếu đã có bất kỳ sự chấp thuận nào liên quan.
            modelBuilder.Entity<LegalDocumentAcceptance>()
                .HasOne(acceptance => acceptance.LegalDocument)
                .WithMany(doc => doc.LegalDocumentAcceptances)
                .HasForeignKey(acceptance => acceptance.LegalDocumentId)
                .OnDelete(DeleteBehavior.Restrict);

            #endregion

            #region Wallet Configuration
            // Wallet -> AppUser (1:1)
            modelBuilder.Entity<Wallet>()
                .HasOne(w => w.User)
                .WithOne()
                .HasForeignKey<Wallet>(w => w.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // Ràng buộc unique cho mỗi người dùng chỉ có một ví
            modelBuilder.Entity<Wallet>()
                .HasIndex(w => w.UserId)
                .IsUnique();
                
            // Mối quan hệ Transaction
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.SourceWallet)
                .WithMany(w => w.SourceTransactions)
                .HasForeignKey(t => t.SourceWalletId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.TargetWallet)
                .WithMany(w => w.TargetTransactions)
                .HasForeignKey(t => t.TargetWalletId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // HeldFund -> BookedSlot (M:1)
            modelBuilder.Entity<HeldFund>()
                .HasOne(h => h.BookedSlot)
                .WithMany()
                .HasForeignKey(h => h.BookedSlotId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<HeldFund>()
                .HasOne(h => h.WithdrawalRequest)
                .WithMany()
                .HasForeignKey(h => h.WithdrawalRequestId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<HeldFund>()
                .HasOne(h => h.BookedSlot)
                .WithMany()
                .HasForeignKey(h => h.BookedSlotId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // DepositRequest -> AppUser (M:1)
            modelBuilder.Entity<DepositRequest>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index for NumericOrderCode for faster lookups from PayOS callbacks
            modelBuilder.Entity<DepositRequest>()
                .HasIndex(d => d.NumericOrderCode);

            // WithdrawalRequest -> AppUser (M:1)
            modelBuilder.Entity<WithdrawalRequest>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // WithdrawalRequest -> BankAccount (M:1)
            modelBuilder.Entity<WithdrawalRequest>()
                .HasOne(w => w.BankAccount)
                .WithMany()
                .HasForeignKey(w => w.BankAccountId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // BankAccount -> AppUser (M:1)
            modelBuilder.Entity<BankAccount>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình kiểu dữ liệu decimal
            modelBuilder.Entity<Wallet>()
                .Property(w => w.Balance)
                .HasColumnType("decimal(18, 2)");
                
            modelBuilder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasColumnType("decimal(18, 2)");
                
            modelBuilder.Entity<HeldFund>()
                .Property(h => h.Amount)
                .HasColumnType("decimal(18, 2)");
                
            modelBuilder.Entity<DepositRequest>()
                .Property(d => d.Amount)
                .HasColumnType("decimal(18, 2)");
                
            modelBuilder.Entity<WithdrawalRequest>()
                .Property(w => w.GrossAmount)
                .HasColumnType("decimal(18, 2)");
                
            modelBuilder.Entity<WithdrawalRequest>()
                .Property(w => w.NetAmount)
                .HasColumnType("decimal(18, 2)");
                
            modelBuilder.Entity<FeeConfig>()
                .Property(f => f.Value)
                .HasColumnType("decimal(18, 4)");
                
            // Giá trị mặc định cho PaymentGateway
            modelBuilder.Entity<DepositRequest>()
                .Property(d => d.PaymentGateway)
                .HasDefaultValue("PayOS");
            #endregion

            #region NotificationEntities Configuration
            modelBuilder.Entity<NotificationEntity>()
                .HasKey(d => d.Id);

            modelBuilder.Entity<NotificationEntity>()
                .HasMany(e => e.AppUsers)
                .WithMany(e => e.NotificationEntities)
                .UsingEntity<AppUserNotification>(e =>
                {
                    e.HasOne(an => an.AppUser)
                    .WithMany(a => a.AppUserNotifications)
                    .HasForeignKey(an => an.AppUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                    e.HasOne(an => an.NotificationEntity)
                    .WithMany(n => n.AppUserNotifications)
                    .HasForeignKey(an => an.NotificationEntityId)
                    .OnDelete(DeleteBehavior.SetNull);
                });
            #endregion
            #region Configuration For Booking
            modelBuilder.Entity<LessonSnapshot>()
                .HasKey(ls => ls.Id);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.LessonSnapshot)
                .WithMany()
                .HasForeignKey(b => b.LessonSnapshotId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<BookedSlot>()
                .HasOne(bs => bs.HeldFund)
                .WithOne(hf => hf.BookedSlot)
                .HasForeignKey<BookedSlot>(bs => bs.HeldFundId)
                .OnDelete(DeleteBehavior.SetNull);
            #endregion

            #region BookingDispute Configuration

            // BookingDispute -> Learner (M:1)
            modelBuilder.Entity<BookingDispute>()
                .HasOne(d => d.Learner)
                .WithMany()
                .HasForeignKey(d => d.LearnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // BookingDispute -> Tutor (M:1)
            modelBuilder.Entity<BookingDispute>()
                .HasOne(d => d.Tutor)
                .WithMany()
                .HasForeignKey(d => d.TutorId)
                .OnDelete(DeleteBehavior.Restrict);

            // BookingDispute -> Staff (M:1) optional
            modelBuilder.Entity<BookingDispute>()
                .HasOne(d => d.Staff)
                .WithMany()
                .HasForeignKey(d => d.StaffId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // BookingDispute -> BookedSlot (M:1)
            modelBuilder.Entity<BookingDispute>()
                .HasOne(d => d.BookedSlot)
                .WithMany() // Nếu BookedSlot không có navigation property về BookingDisputes
                .HasForeignKey(d => d.BookedSlotId)
                .IsRequired(false) // Đảm bảo khóa ngoại là tùy chọn
                .OnDelete(DeleteBehavior.SetNull); // Đặt thành NULL khi BookedSlot bị xóa

            // BookedSlot -> Dispute (M:1) (cấu hình này đã có ở AppDbContext.cs từ trước)
            modelBuilder.Entity<BookedSlot>()
                .HasOne(bs => bs.Dispute)
                .WithMany() // Nếu BookingDispute không có navigation property về BookedSlots (nhưng thực tế là có)
                .HasForeignKey(bs => bs.DisputeId)
                .IsRequired(false) // Đảm bảo khóa ngoại là tùy chọn
                .OnDelete(DeleteBehavior.SetNull); // Đặt thành NULL khi BookingDispute bị xóa

            #endregion

            #region RescheduleRequest Configuration
            // RescheduleRequest -> BookedSlot (M:1)
            modelBuilder.Entity<RescheduleRequest>()
                .HasOne(r => r.BookedSlot)
                .WithMany(b => b.RescheduleRequests)
                .HasForeignKey(r => r.BookedSlotId)
                .OnDelete(DeleteBehavior.Cascade);

            // RescheduleRequest -> OfferedSlot (1:M)
            modelBuilder.Entity<RescheduleRequest>()
                .HasMany(r => r.OfferedSlots)
                .WithOne(o => o.RescheduleRequest)
                .HasForeignKey(o => o.RescheduleRequestId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // RescheduleRequest -> AcceptedSlot (1:1) - optional
            modelBuilder.Entity<RescheduleRequest>()
                .HasOne(r => r.AcceptedSlot)
                .WithOne()
                .HasForeignKey<RescheduleRequest>(r => r.AcceptedSlotId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Cấu hình kiểu dữ liệu DateTime
            modelBuilder.Entity<RescheduleRequest>()
                .Property(r => r.CreatedAt)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<RescheduleRequest>()
                .Property(r => r.ExpiresAt)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<RescheduleRequest>()
                .Property(r => r.RespondedAt)
                .HasColumnType("timestamp without time zone");
            #endregion


            #region TutorIntroductionVideo Configuration
            // TutorIntroductionVideo -> Tutor (M:1)
            modelBuilder.Entity<TutorIntroductionVideo>()
                .HasOne(tiv => tiv.Tutor)
                .WithMany(t => t.IntroductionVideos)
                .HasForeignKey(tiv => tiv.TutorUserId)
                .OnDelete(DeleteBehavior.Cascade);
            #endregion

            #region BookingConfig Configuration
            // BookingConfig -> Tutor (1:1)
            modelBuilder.Entity<BookingConfig>()
                .HasOne(bc => bc.Tutor)
                .WithOne(t => t.BookingConfig)
                .HasForeignKey<BookingConfig>(bc => bc.TutorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Đảm bảo TutorId là unique
            modelBuilder.Entity<BookingConfig>()
                .HasIndex(bc => bc.TutorId)
                .IsUnique();
            #endregion

            // Manager configuration
            modelBuilder.Entity<Manager>()
                .Property(m => m.EncryptedCitizenId)
                .IsRequired();

            // Staff configuration
            modelBuilder.Entity<Staff>()
                .Property(s => s.EncryptedCitizenId)
                .IsRequired();
        }
    }
}
