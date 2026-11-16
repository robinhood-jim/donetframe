namespace Frameset.WebMvc.Dto;

public class LoginRequest
{
    public string UserAccount
    {
        get;
        set;
    } = string.Empty;

    public string Password
    {
        get;
        set;
    } = string.Empty;

    public string? VerifyId
    {
        get;
        set;
    } = string.Empty;

    public string? VerifyCode
    {
        get;
        set;
    } = string.Empty;
}