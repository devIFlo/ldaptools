using LdapTools.ViewModels;

namespace LdapTools.Services.Interfaces
{
    public interface ILdapExplorerService
    {
        Task<List<string>> GetAllOUs();
        Task<List<LdapUserViewModel>> GetUsersAsync(string ou, string args, bool recursive);
        Task<List<OrganizationalUnitViewModel>> GetOuTreeAsync(string? parentDn = null);
    }
}
