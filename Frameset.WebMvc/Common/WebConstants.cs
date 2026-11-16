namespace Frameset.WebMvc.Common
{
    public static class WebConstants
    {
        public enum ACCOUNT_TYPE
        {
            SYSADMIN = 1,  //System User,can operator system menu
            ORGADMIN = 2,  // Org User,can access Org right menu and menu Assign by system user
            EMPLOYER = 3,
            EMPLOYEE = 4,
            ORDINARY = 5
        }
        public enum TENANT_TYPE
        {
            OWNER = 1,
            ORGADMIN = 2,
            EMPLOYEE,
            EMPLOYER,
            TEMPORARY,
            NORIGHT
        }
        public enum SYS_RESPONSIBLITIY
        {
            SYS_RESP = 1,
            ORG_RESP,
            FREE_RESP
        }
        public enum AUDIT_TYPE
        {
            DRAFT = 0,
            INIT = 1,
            VERIFYIED = 2,
            DENIED = -1,
        }
        public static readonly string SYSPARAMS_PERMISSIONS_PERFIX = "PERMISSIONS_";
        public static readonly string SYSPARAMS_PERMISSIONS_ORG_PERFIX = "PERMISSIONS_ORG";
        public static readonly string SYSPARAMS_PERMISSIONS_ORDINARY_PERFIX = "PERMISSIONS_ORDINARY";
        public static readonly string SYSPARAMS_ROLES_PERFIX = "ROLES_";
        public static readonly string SYSPARAMS_DEFAULT_PERFIX = "DEFAULT";
        public static readonly string TOKEN = "tokenMap";
        public static readonly string LOCALE_KEY = "application.locale";
        public static readonly string RESOURCE_DEFAULT = "0";
        public static readonly string RESOURCE_ASSIGN = "1";
        public static readonly string RESOURCE_DENIED = "2";
        public enum ASSIGN_TYPE
        {
            DEFAULT = 0,
            ASSING = 1,
            DENIED = 2
        }
    }
}
