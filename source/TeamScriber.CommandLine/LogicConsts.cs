using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamScriber.CommandLine
{
    public class LogicConsts
    {
        public static readonly TimeSpan AudioSegmentTime = new TimeSpan(0, 5, 0);

        public const int ProgressWeightAudioPerVideo = 4;
        public const int ProgressWeightWhisperConversion = 10;
        public const int ProgressWeightPromptQueries = 10;
        public const int ProgressWeightOneNoteImport = 2;

        public const string BaseFileSufixe = "-01-Base-MD-to-HTML";
        public const string EmbedFileSufixe = "-02-Embeded-HTML";
        public const string ChromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";

        public const string clientId = "MY-SECRET";     // Application (client) ID 
        public const string tenantId = "MY-SECRET";     // Directory (tenant) ID 
        public const string clientSecret = "MY-SECRET"; // Client secret 
        public const string tenantIdCommon = "common";     // Directory (tenant) ID 
        public const string redirectURI = "http://localhost";

        //Set the scope for API call to user.read
        public static readonly string[] scopes = new string[]
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
