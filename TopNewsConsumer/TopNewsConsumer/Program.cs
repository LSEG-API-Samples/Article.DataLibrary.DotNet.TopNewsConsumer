using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Refinitiv.DataPlatform.Core;
using Refinitiv.DataPlatform.Content.News;
using Refinitiv.DataPlatform.Delivery.Request;

namespace TopNewsConsole
{
    class Program
    {
        
        #region RDPCredential
        // Please set RDP Username, Password and AppKey here.
        static string UserName = "<RDP Username>";
        static string Password = "<RDP Password>";
        static string AppKey = "<APP Key>";
        #endregion
        
        private static string TopNewsEndpoint = "https://api.refinitiv.com/data/news/v1/top-news";

        // b_ShowNewStory set to true to get story associated with top news headline and print to console output
        private static bool b_ShowNewsStory =false;

        // b_SaveImagesToFile set to true to get photo associated with top news headline and save photo to images_save_path
        private static bool b_SaveImagesToFile = false;
        // Change path to a valid path on your pc
        private static string images_save_path = @"c:\tmp\images\";

        static void Main(string[] args)
        {
            #region SessionManagement

            var session = CoreFactory.CreateSession(new PlatformSession.Params()
                .WithOAuthGrantType(new GrantPassword().UserName(UserName)
                    .Password(Password))
                .AppKey(AppKey)
                .WithTakeSignonControl(true)
                .OnState((s, state, msg) => Console.WriteLine($"{DateTime.Now}:{msg}. (State: {state})"))
                .OnEvent((s, eventCode, msg) => Console.WriteLine($"{DateTime.Now}:{msg}. (Event: {eventCode})")));
            session.Open();

            if (session.OpenState == Session.State.Opened) 
                Console.WriteLine("Session is now open");
            else if (session.OpenState == Session.State.Pending)
            {
                Console.WriteLine("Session state is pending");
            }
            if(session.OpenState == Session.State.Closed)
            {
                Console.WriteLine("Session is now closed");
                return;
            }
            
            #endregion


            #region GetTopNewsPackage
            // Call Endpoint.SendRequestAsync to get TopNews from TopNewsEndpoint
            var topNewsPkgResp = Endpoint.SendRequestAsync(session, new Uri(TopNewsEndpoint)).GetAwaiter().GetResult();

            // Parse TopNewsPackage from data element
            var data = topNewsPkgResp.Data.Raw["data"]?.ToObject<IList<TopNewsPackage>>();
            if (data != null)
            {
                foreach (var package in data)
                {
                    Console.WriteLine($"Package Name:{package.Name} Number of Pages: {package.Pages?.Count}");
                    if (!package.Pages!.Any()) continue;
                    foreach (var subPackage in package.Pages)
                    {
                        Console.WriteLine("\n");
                        // Retrieve Top News Headlines for each underlying Page using specified TopNewsId
                        #region GetTopNewsHedlines

                        Console.WriteLine($"\t\t{subPackage.Name} [{subPackage.TopNewsId}]");
                        var topNewsHeadlinesResp = Endpoint.SendRequestAsync(session,
                                new Uri($"{TopNewsEndpoint}/{subPackage.TopNewsId}"))
                            .GetAwaiter().GetResult();

                        var headlinesList = topNewsHeadlinesResp.Data.Raw["data"]?.ToObject<IList<TopNewsData>>();
                        if (!headlinesList.Any()) continue;

                        foreach (var topHeadline in headlinesList)
                        {
                            Console.WriteLine(
                                $"\t\t\t> {topHeadline.text} storyId:[{topHeadline.storyId}] imageId:[{topHeadline.image?.id}]");

                            #region GetImages

                            if (b_SaveImagesToFile)
                            {
                                if (topHeadline.image != null && !string.IsNullOrEmpty(topHeadline.image.id.Trim()))
                                {

                                    // Retrieve Headlines Images and save it to images_save_path
                                    var image = Image.Definition(topHeadline.image?.id).Rendition("thumbnail")
                                        .GetData();
                                    if (image.IsSuccess)
                                    {
                                        Console.WriteLine(
                                            $"\t\t\tGet Image Id: [{topHeadline.image?.id}] Image represented as a: {image.Data.Image} of length: {image.Data.Image.Length} bytes.\n");

                                        using var ms = new MemoryStream(image.Data.Image);
                                        using var fs = new FileStream($"{images_save_path}{topHeadline.image.id}.jpg",
                                            FileMode.Create);
                                        ms.WriteTo(fs);
                                    }
                                    else
                                    {
                                        Console.WriteLine(
                                            $"Failed to retrieve image ID: {topHeadline.image?.id}\n{image.Status}");
                                    }
                                }
                            }
                            #endregion
                            #region  GetStory

                            if (b_ShowNewsStory)
                            {
                                // Retrieve News Story using Story class. It required storyId from the Top News headline.
                                if (!string.IsNullOrEmpty(topHeadline.storyId))
                                {
                                    var story = Story.Definition(topHeadline.storyId).GetData();
                                    Console.WriteLine(story.IsSuccess
                                        ? $"\n\t\t\t Retrieving Story Id:{topHeadline.storyId}\n{story.Data.NewsStory}\n"
                                        : $"\n\t\t\tProblem retrieving the story: {story.Status}\n");
                                }
                                else
                                {
                                    Console.WriteLine("\n\t\t\tStory Id is empty skipped\n");
                                }
                            }

                            #endregion

                           
                        }
                        #endregion
                    }
                    Console.WriteLine("\n");
                }
            }

            #endregion

            Console.ReadKey();
        }
    }

    internal class TopNewsData
    {
        public string text { get; set; }
        public string dateLine { get; set; }
        public string snippet { get; set; }
        public TopNewsImages image { get; set; }
        public string versionCreated { get; set; }
        public string storyId { get; set; }
        public IList<HeadlineInfo> relatedHeadlines { get; set; }
    }
    internal class TopNewsImages
    {
        public string byLine { get; set; }
        public string text { get; set; }
        public string smallId { get; set; }
        public string id { get; set; }
    }

    internal class HeadlineInfo
    {
        public string text { get; set; }
        public string versionCreated { get; set; }
        public string dateLine { get; set; }
        public string snippet { get; set; }
        public string webUrl { get; set; }
        public string documentType { get; set; }
        public string storyId { get; set; }
    }
    internal class TopNewsPackage
    {
        public string Name { get; set; }
        public IList<TopNewsPackageData> Pages { get; set; }
    }
    internal class TopNewsPackageData
    {
        public string Name { get; set; }
        public string RevisionId { get; set; }
        public string RevisionDate { get; set; }
        public string TopNewsId { get; set; }
    }
}
