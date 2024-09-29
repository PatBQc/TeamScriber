using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamScriber.CommandLine
{
    public static class LogicConsts
    {
        public static readonly TimeSpan AudioSegmentTime = new TimeSpan(0, 5, 0);
        public static readonly string LineSeparator = new string('-', 80);

        public const int ProgressWeightAudioPerVideo = 4;
        public const int ProgressWeightWhisperConversion = 10;
        public const int ProgressWeightPromptQueries = 10;
        public const int ProgressWeightOneNoteImport = 2;

        public const string MarkdownToHtmlBaseFileSufixe = "-01-Base-MD-to-HTML";
        public const string MarkdownToHtmlEmbedFileSufixe = "-02-Embeded-HTML";
        public const string MarkdownToHtmlChromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";

        public const string MicrosoftGraphClientId = "MY-SECRET";     // Application (client) ID 
        public const string MicrosoftGraphTenantId = "MY-SECRET";     // Directory (tenant) ID 
        public const string MicrosoftGraphClientSecret = "MY-SECRET"; // Client secret 
        public const string MicrosoftGraphTenantIdCommon = "common";     // Directory (tenant) ID 
        public const string MicrosoftGraphRedirectURI = "http://localhost";
        public const string MicrosoftGraphTokenFilename = ".\\token.dat";

        //Set the scope for API call to user.read
        public static readonly string[] MicrosoftGraphScopes = new string[]
        {
            "User.Read",
            "Notes.Create",
            "Notes.Read",
            "Notes.Read.All",
            "Notes.ReadWrite",
            "Notes.ReadWrite.All"
        };


    }
}
