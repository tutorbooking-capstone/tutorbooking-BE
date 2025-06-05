using App.Repositories.UoW;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
