using App.Core.Base;
using App.Core.Constants;
using App.DTOs.LegalDocumentDTOs;
using App.DTOs.LegalDocumentDTOs.VersionDTOs;
using App.Repositories.Models.Legal;
using App.Repositories.UoW;
using App.Services.Interfaces;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using StackExchange.Profiling.Internal;
using System.Runtime.InteropServices;

namespace App.Services.Services
{
    public class LegalDocumentService : ILegalDocumentService
    {
        private readonly IUnitOfWork _unitOfWork;

        public LegalDocumentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<LegalDocumentResponse> CreateAsync(LegalDocumentCreateRequest request)
        {
            var entity = request.ToEntity();
            _unitOfWork.GetRepository<LegalDocument>().Insert(entity);
            await _unitOfWork.SaveAsync();
            return entity.ToResponse();
        }

        public async Task<List<LegalDocumentResponse>> GetAllAsync([Optional] string? category, int page = 1, int size = 10)
        {
            var predicate = PredicateBuilder.New<LegalDocument>(true);
            if (!category.IsNullOrWhiteSpace()) predicate.And(LegalDocument.IsCategoryExpression(category));

            var entities = await _unitOfWork.GetRepository<LegalDocument>().ExistEntities()
                .Include(e => e.Versions).ThenInclude(v => v.LegalDocumentAcceptances)
                .Where(predicate)
                .OrderByDescending(e => e.LastUpdatedTime)      
                .Skip((page - 1) * size)
                .Take(size)
                .Select(e => e.ToResponseWithLatestActiveVersionOnly())
                .ToListAsync();
            return entities;
        }

        public async Task<LegalDocumentResponse?> GetByIdAsync(string id)
        {
            var entity = await _unitOfWork.GetRepository<LegalDocument>().ExistEntities()
                .Include(e => e.Versions).ThenInclude(v => v.LegalDocumentAcceptances)
                .Where(e => e.Id.Equals(id))
                .Select(e => e.ToResponse())
                .FirstOrDefaultAsync();
            if (entity == null)
                throw new ErrorException(404, ErrorCode.NotFound, "NOT_FOUND");
            return entity;
        }

        public async Task<ICollection<string>> GetAllCategoriesAsync()
        {
            var categories = await _unitOfWork.GetRepository<LegalDocument>().ExistEntities()
                .Select(e => e.Category)
                .Distinct()
                .ToListAsync();
            return categories;
        }

        public async Task<LegalDocumentResponse> UpdateAsync(LegalDocumentUpdateRequest request)
        {
            var entity = await _unitOfWork.GetRepository<LegalDocument>().GetByIdAsync(request.Id);
            if (entity == null)
                throw new ErrorException(404, ErrorCode.NotFound, "NOT_FOUND");
            entity.UpdateFromRequest(request);
            _unitOfWork.GetRepository<LegalDocument>().Update(entity);
            await _unitOfWork.SaveAsync();
            return entity.ToResponse();
        }

        public async Task DeleteAsync(string id)
        {
            await _unitOfWork.ExecuteWithConnectionReuseAsync(async () =>
            {
                var entity = await _unitOfWork.GetRepository<LegalDocument>().ExistEntities().FirstOrDefaultAsync(x => x.Id.Equals(id));
                if (entity == null)
                    throw new ErrorException(404, ErrorCode.NotFound, "NOT_FOUND");

                var entityHasAcceptance = await _unitOfWork.GetRepository<LegalDocumentAcceptance>().ExistEntities()
                    .AnyAsync(x => x.LegalDocumentId == entity.Id);
                if (entityHasAcceptance)
                    throw new ErrorException(400, ErrorCode.BadRequest, "LEGAL_DOCUMENT_HAS_ACCEPTANCES");

                _unitOfWork.GetRepository<LegalDocument>().Delete(entity);
                await _unitOfWork.SaveAsync();
                return true;
            });
        }

        public async Task<LegalDocumentVersionResponse> CreateVersionAsync(LegalDocumentVersionCreateRequest request)
        {
            var legalDocument = await _unitOfWork.GetRepository<LegalDocument>().GetByIdAsync(request.LegalDocumentId);
            if (legalDocument == null)
                throw new ErrorException(404, ErrorCode.NotFound, "LEGAL_DOCUMENT_NOT_FOUND");

            var entity = request.ToEntity();
            _unitOfWork.GetRepository<LegalDocumentVersion>().Insert(entity);
            await _unitOfWork.SaveAsync();

            if (entity.Status == LegalDocumentStatus.Active)
                await InactiveOtherLegalDocumentVersionsAsync(legalDocument.Id, entity.Id);
            return entity.ToResponse();
        }

        public async Task<LegalDocumentVersionResponse?> GetVersionByIdAsync(string id)
        {
            var version = await _unitOfWork.GetRepository<LegalDocumentVersion>().GetByIdAsync(id);
            if (version == null)
                throw new ErrorException(404, ErrorCode.NotFound, "LEGAL_DOCUMENT_VERSION_NOT_FOUND");
            return version.ToResponse();
        }

        public async Task<LegalDocumentVersionResponse?> UpdateVersionAsync(LegalDocumentVersionUpdateRequest request)
        {
            var response = await _unitOfWork.ExecuteWithConnectionReuseAsync( async () =>
            {
                var entity = await _unitOfWork.GetRepository<LegalDocumentVersion>().ExistEntities()
                    .FirstOrDefaultAsync(x => x.Id.Equals(request.Id));
                if (entity == null)
                    throw new ErrorException(404, ErrorCode.NotFound, "LEGAL_DOCUMENT_VERSION_NOT_FOUND");

                bool entityHasAcceptance = await _unitOfWork.GetRepository<LegalDocumentAcceptance>().ExistEntities()
                    .AnyAsync(x => x.LegalDocumentVersionId == entity.Id);
                if (entityHasAcceptance)
                    throw new ErrorException(400, ErrorCode.BadRequest, "LEGAL_DOCUMENT_VERSION_HAS_ACCEPTANCES");

                entity.UpdateFromRequest(request);
                _unitOfWork.GetRepository<LegalDocumentVersion>().Update(entity);
                await _unitOfWork.SaveAsync();
                if (request.Status == LegalDocumentStatus.Active)
                    await InactiveOtherLegalDocumentVersionsAsync(entity.LegalDocumentId, entity.Id);
                return entity.ToResponse();
            });
            return response;
        }

        public async Task DeleteVersionAsync(string id)
        {
            await _unitOfWork.ExecuteWithConnectionReuseAsync(async () =>
            {
                var entity = await _unitOfWork.GetRepository<LegalDocumentVersion>().ExistEntities()
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null)
                    throw new ErrorException(404, ErrorCode.NotFound, "LEGAL_DOCUMENT_VERSION_NOT_FOUND");

                bool entityHasAcceptance = await _unitOfWork.GetRepository<LegalDocumentAcceptance>().ExistEntities()
                    .AnyAsync(x => x.LegalDocumentVersionId == entity.Id);
                if (entityHasAcceptance)
                    throw new ErrorException(400, ErrorCode.BadRequest, "LEGAL_DOCUMENT_VERSION_HAS_ACCEPTANCES");

                _unitOfWork.GetRepository<LegalDocumentVersion>().Delete(entity);
                await _unitOfWork.SaveAsync();
                return true;
            });
        }

        #region Private Methods
        private async Task InactiveOtherLegalDocumentVersionsAsync(string legalDocumentId, string activeVersionId)
        {
            var legalDocuments = await _unitOfWork.GetRepository<LegalDocumentVersion>()
                .ExistEntities()
                .Where(x => x.LegalDocumentId == legalDocumentId && x.Id != activeVersionId && x.Status != LegalDocumentStatus.Draft)
                .ToListAsync();
            if (legalDocuments.Count == 0) return;
            foreach (var legalDocument in legalDocuments)
            {
                legalDocument.Status = LegalDocumentStatus.Inactive;
                _unitOfWork.GetRepository<LegalDocumentVersion>().Update(legalDocument);
            }
            await _unitOfWork.SaveAsync();
        }
        #endregion
    }
}
