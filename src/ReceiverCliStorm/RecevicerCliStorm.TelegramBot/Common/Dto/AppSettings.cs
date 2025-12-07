namespace ReceiverCliStorm.TelegramBot.Common.Dto;

public class AppSettings
{
    public string Token { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string SessionsPath { get; set; } = string.Empty;
    public string DeviceInfoPath { get; set; } = string.Empty;
    public string ApiId { get; set; } = string.Empty;
    public string ApiHash { get; set; } = string.Empty;
    public string ProxyPath { get; set; } = string.Empty;
    public string FaPath { get; set; } = string.Empty;
    public string EnPath { get; set; } = string.Empty;
    public string Password2Fa { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string LogChatId { get; set; } = string.Empty;
    public string Developer { get; set; } = string.Empty;
    public bool UseProxy { get; set; }
    public bool UseLogCLI { get; set; }
    public bool UseCheckReport { get; set; }
    public bool UseChangeBio { get; set; }
    public long AskTimeOutMinute { get; set; }
    public IDictionary<string, string> CommandsSudo { get; set; } = new Dictionary<string, string>();
    public IDictionary<string, string> CommandsUser { get; set; } = new Dictionary<string, string>();
    public IDictionary<string, string> UsernamesForceJoin { get; set; } = new Dictionary<string, string>();
    public long[] Sudos { get; set; } = [];
}