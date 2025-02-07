namespace LdapTools.Services.Interfaces
{
    public interface ISeedUserRoleInitial
    {
        Task SeedRolesAsync();
        Task SeedUsersAsync();
    }
}