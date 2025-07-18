using App.Repositories.UoW;

namespace App.Services.Services
{
	public class MessageService
	{
		#region Dependencies
		private IUnitOfWork _unitOfWork;

		public MessageService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}
		#endregion


	}
}
