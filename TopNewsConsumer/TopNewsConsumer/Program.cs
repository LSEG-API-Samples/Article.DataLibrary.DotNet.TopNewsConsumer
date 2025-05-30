using System;
using System.IO;
using System.Linq;
using LSEG.Data.Core;
using LSEG.Data.Content.News;



namespace TopNewsConsole
{
    class Program
    {
        
        #region Credential
        // Please set Username, Password and AppKey here.
        
        static string UserName = Environment.GetEnvironmentVariable("RDP_USERNAME");
        static string Password = Environment.GetEnvironmentVariable("RDP_PASSWORD");
        static string AppKey = Environment.GetEnvironmentVariable("APP_KEY");
        #endregion
        
        

        // b_ShowNewStory set to true to get story associated with top news headline and print to console output
        private static bool b_ShowNewsStory =false;

        // b_SaveImagesToFile set to true to get photo associated with top news headline and save photo to images_save_path
        private static bool b_SaveImagesToFile = false;
        // Change path to a valid path on your pc
        private static string images_save_path = @"c:\tmp\images\";

        static void Main(string[] args)
        {
            #region SessionManagement

            var session = PlatformSession.Definition().AppKey(AppKey)
                .OAuthGrantType(new GrantPassword().UserName(UserName).Password(Password))
                .TakeSignonControl(true)
                .GetSession()
                .OnState((s, state, msg) => Console.WriteLine($"{DateTime.Now}:{msg}. (State: {state})"))
                .OnEvent((s, eventCode, msg) => Console.WriteLine($"{DateTime.Now}:{msg}. (Event: {eventCode})"));

           
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

            var topNewsPkgResp = TopNews.Definition().GetData();

           
          
            
            if (topNewsPkgResp != null)
            {
                foreach (var package in topNewsPkgResp.Data.Categories)
                {
                    Console.WriteLine($"Package Name:{package.Key} Number of Pages: {package.Value.Count}");
                    if (!package.Value.Any()) continue;
                    foreach (var subPackage in package.Value)
                    {
                        Console.WriteLine("\n");
                        // Retrieve Top News Headlines for each underlying Page using specified TopNewsId
                        #region GetTopNewsHeadlines

                        Console.WriteLine($"\t\t{subPackage.Page} [{subPackage.TopNewsID}]");

                        var topNewsHeadlinesResp = TopNewsHeadlines.Definition(subPackage.TopNewsID).GetData();
                        


                        var headlinesList = topNewsHeadlinesResp.Data.Headlines;
                        if (!headlinesList.Any()) continue;

                        foreach (var topHeadline in headlinesList)
                        {
                            Console.WriteLine(
                                $"\t\t\t> {topHeadline.Text} storyId:[{topHeadline.StoryId}] imageId:[{topHeadline.ImageId}]");

                            #region GetImages

                            if (b_SaveImagesToFile)
                            {
                                if (topHeadline.ImageId != null && !string.IsNullOrEmpty(topHeadline.ImageId.Trim()))
                                {

                                    // Retrieve Headlines Images and save it to images_save_path
                                    var image = Image.Definition(topHeadline.ImageId).Rendition("thumbnail")
                                        .GetData();
                                    if (image.IsSuccess)
                                    {
                                        Console.WriteLine(
                                            $"\t\t\tGet Image Id: [{topHeadline.ImageId}] Image represented as a: {image.Data.Image} of length: {image.Data.Image.Length} bytes.\n");

                                        using var ms = new MemoryStream(image.Data.Image);
                                        using var fs = new FileStream($"{images_save_path}{topHeadline.ImageId}.jpg",
                                            FileMode.Create);
                                        ms.WriteTo(fs);
                                    }
                                    else
                                    {
                                        Console.WriteLine(
                                            $"Failed to retrieve image ID: {topHeadline.ImageId}\n{image.HttpStatus}");
                                    }
                                }
                            }
                            #endregion
                            #region  GetStory

                            if (b_ShowNewsStory)
                            {
                                // Retrieve News Story using Story class. It required storyId from the Top News headline.
                                if (!string.IsNullOrEmpty(topHeadline.StoryId))
                                {
                                    var story = Story.Definition(topHeadline.StoryId).GetData();
                                    Console.WriteLine(story.IsSuccess
                                        ? $"\n\t\t\t Retrieving Story Id:{topHeadline.StoryId}\n{story.Data.NewsStory}\n"
                                        : $"\n\t\t\tProblem retrieving the story: {story.HttpStatus}\n");
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
  
}
