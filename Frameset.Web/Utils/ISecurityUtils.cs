namespace Frameset.Web.Utils
{
    public interface ISecurityUtils
    {
        bool IsLogin();
        string? GetLoginUser();
        List<string> GetUserPermissions();
        bool IsLoginUserSysAdmin();
        bool IsLoginUserOrgAdmin();
        long? GetLoginTenant();
        bool IsLoginUserTenantAdmin(long? tenantId = null);
        bool IsLoginUserTenantOwner(long? tenantId = null);
    }
}
