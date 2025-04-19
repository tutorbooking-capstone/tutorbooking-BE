using App.Core.Base;

namespace App.Repositories.Models
{
    public class AppUser : BaseUser
    {
        public string FullName { get; set; } = "SystemCreated";

        public int? EmailCode { get; set; }
        public DateTime? CodeGeneratedTime { get; set; }

    }
}
