using App.Repositories.Models.Legal;
using App.Repositories.UoW;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using App.DTOs.LegalDocumentDTOs;
using App.Services.Interfaces.User;
using App.Services.Interfaces;

namespace App.Services.Services
{
    public class LegalDocumentAcceptanceService : ILegalDocumentAcceptanceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserService _userService;

        public LegalDocumentAcceptanceService(IUnitOfWork unitOfWork, IUserService userService)
        {
            _unitOfWork = unitOfWork;
            _userService = userService;
        }

        public async Task AcceptAllDocumentsForCurrentUserAsync()
        {
            string userId = _userService.GetCurrentUserId();
            await _unitOfWork.ExecuteWithConnectionReuseAsync(async () =>
            {
                var documentIds = await _unitOfWork.GetRepository<LegalDocument>().ExistEntities()
                    .Where(e => e.Versions.Any(v => v.Status == LegalDocumentStatus.Active) && !e.LegalDocumentAcceptances.Any(a => a.UserId.Equals(userId)))
                    .Select(e => new LegalDocumentLatestVersion()
                    {
                        Id = e.Id,
                        VersionId = e.Versions.FirstOrDefault(v => v.Status == LegalDocumentStatus.Active).Id
                    })
                    .ToListAsync();
                if (!documentIds.Any())
                    return false;

                var entities = documentIds.Select(e => new LegalDocumentAcceptance() {
                    UserId = userId,
                    LegalDocumentId = e.Id,
                    LegalDocumentVersionId = e.VersionId
                }).ToList();

                _unitOfWork.GetRepository<LegalDocumentAcceptance>().InsertRange(entities);
                await _unitOfWork.SaveAsync();

                return true;
            });
        }

        public async Task<ICollection<LegalDocumentResponse>> GetNotAcceptedLegalDocumentsOfCurrentUserAsync()
        {
            string userId = _userService.GetCurrentUserId();
            var documents = await _unitOfWork.GetRepository<LegalDocument>().ExistEntities()
                    .Where(e => e.Versions.Any(v => v.Status == LegalDocumentStatus.Active) && !e.LegalDocumentAcceptances.Any(a => a.UserId.Equals(userId)))
                    .Select(e => e.ToResponse())
                    .ToArrayAsync();
            return documents;
        }


        public async Task<ICollection<LegalDocumentResponse>> GetLegalDocumentsWithActiveVersionAsync()
        {
            var legalDocuments = await _unitOfWork.GetRepository<LegalDocument>().ExistEntities()
                .Include(e => e.Versions)
                .Where(e => e.Versions.Any(v => v.Status == LegalDocumentStatus.Active))
                .Select(e => e.ToResponseWithLatestActiveVersionOnly())
                .ToListAsync();
            return legalDocuments;
        }

    }

    public record LegalDocumentLatestVersion
    {
        public string Id { get; init; }
        public string VersionId { get; init; }
    }
}
