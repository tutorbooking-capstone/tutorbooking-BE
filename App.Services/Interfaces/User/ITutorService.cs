﻿using App.DTOs.AppUserDTOs.TutorDTOs;
using App.DTOs.HashtagDTOs;
using App.Repositories.Models.User;

namespace App.Services.Interfaces.User
{
    public interface ITutorService
    {
        // Tutor Registration
        Task<TutorResponse> RegisterAsTutorAsync(TutorRegistrationRequest request);
        Task<TutorResponse> SeedRegisterAsTutorAsync(string userId, TutorRegistrationRequest request);

        // Profile Updates 
        Task UpdateLanguagesAsync(List<TutorLanguageDTO> languages);
        Task UpdateTutorHashtagsAsync(UpdateTutorHashtagListRequest request);

        // Retrieval
        Task<TutorResponse> GetByIdAsync(string tutorId);
        Task<bool> GetVerificationStatusAsync(string tutorId);
        Task<List<TutorHashtagDTO>> GetTutorHashtagsAsync();
        Task<List<TutorLanguageDTO>> GetTutorLanguagesAsync();
        Task<List<TutorCardDTO>> GetTutorCardListAsync();
		Task<List<TutorCardDTO>> GetTutorCardsPagingAsync(int page, int size);

		// Status Management 
		//Task UpdateVerificationStatusAsync(string id, VerificationStatus status, string? updatedBy = null);
    }
}