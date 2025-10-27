using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LdapTools.ViewModels
{
    public class OrganizationalUnitViewModel
    {
        public string Name { get; set; }
        public string DistinguishedName { get; set; }
        public List<OrganizationalUnitViewModel> Children { get; set; } = new();
        public List<LdapUserViewModel> Users { get; set; } = new();
    }
}
