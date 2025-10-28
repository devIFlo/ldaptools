using System.Collections.Generic;

namespace LdapTools.ViewModels
{
    public class PagedLdapUsersViewModel
    {
        public List<LdapUserViewModel> Users { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }

        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
