using App.Repositories.Models;
using App.Repositories.Models.Papers;
using App.Repositories.Models.User;
using App.Repositories.UoW;

namespace App.Repositories.States
{
public class StateOrchestrator
    {
        #region DI Constructor
        private readonly IUnitOfWork _unitOfWork;
        private readonly TutorStateManager _tutorStateManager;
        private readonly TutorApplicationStateManager _tutorApplicationStateManager;
        private readonly ApplicationRevisionStateManager _revisionStateManager;

        public StateOrchestrator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _tutorStateManager = new TutorStateManager(unitOfWork);
            _tutorApplicationStateManager = new TutorApplicationStateManager(unitOfWork);
            _revisionStateManager = new ApplicationRevisionStateManager(unitOfWork);
        }
        #endregion

        public async Task ApproveAsync(string revisionId, string applicationId, string tutorId)
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _revisionStateManager.UpdateRevisionActionAsync(revisionId, RevisionAction.Approve);

                await _tutorApplicationStateManager.UpdateApplicationStatusAsync(applicationId, ApplicationStatus.Verified);

                //await _tutorStateManager.UpdateVerificationStatusAsync(tutorId, VerificationStatus.VerifiedHardcopy);

                await _unitOfWork.SaveAsync();
            });
        }
    }
}
