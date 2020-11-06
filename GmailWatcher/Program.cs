using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GmailWatcher
{
    public class Program
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/gmail-dotnet-quickstart.json
        static string[] Scopes = { GmailService.Scope.GmailReadonly };
        static string ApplicationName = "Gmail API .NET Quickstart";

        static void Main(string[] args)
        {
            UserCredential credential;
            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            while (true)
            {
                // Create Gmail API service.
                var service = new GmailService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });

                // Define parameters of request.
                // List labels.
                UsersResource.LabelsResource.ListRequest request = service.Users.Labels.List("me");
                var labels = request.Execute().Labels.Where(l => l.Name.ToLower() == "inbox").Select(lp => service.Users.Labels.Get("me", lp.Id).Execute());
                foreach (var label in labels.OrderByDescending(l => l.MessagesUnread))
                {
                    Console.WriteLine($"Status: {label.Name} {label.MessagesUnread}/{label.MessagesTotal}");

                    var r = service.Users.Messages.List("me");
                    r.Q = "in:inbox is:unread";
                    var mails = r.Execute().Messages?.Select(mp => service.Users.Messages.Get("me", mp.Id).Execute());
                    if (mails == null)
                        continue;

                    foreach (var mail in mails)
                    {
                        var from = mail.Payload.Headers.SingleOrDefault(mph => mph.Name == "From").Value;
                        var subject = mail.Payload.Headers.SingleOrDefault(mph => mph.Name == "Subject").Value;

                        DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(mail.InternalDate ?? 0);

                        Console.WriteLine($" - [{dateTimeOffset.LocalDateTime}] From: {from}, Subject: {subject}{(mail.Snippet.Length > 0 ? ", Snippit: " + mail.Snippet : "")}");
                    }

                    Console.Beep();
                }

                Task.Delay(1000 * 30).Wait();
            }
        }
    }
}
