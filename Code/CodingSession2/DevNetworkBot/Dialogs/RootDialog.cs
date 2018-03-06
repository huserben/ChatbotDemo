using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevNetworkBot.Model;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;

namespace DevNetworkBot.Dialogs
{
   [Serializable]
   public class RootDialog : LuisDialog<object>
   {
      private readonly List<DevNetwork> devNetworks = new List<DevNetwork>();
      private DevNetwork lastAddedDevNetwork;
      private Presentation lastAddedPresentation;

      public RootDialog() : base(new LuisService(new LuisModelAttribute(
         ConfigurationManager.AppSettings["LuisAppId"],
         ConfigurationManager.AppSettings["LuisAPIKey"])))
      {
         LoadDevNetwork();
      }

      public override Task StartAsync(IDialogContext context)
      {
         context.Wait(ShowGreetingMessageAsync);

         return Task.CompletedTask;
      }

      private void LoadDevNetwork()
      {
         var chatbotPresentation = new Presentation { Presenter = "Benjamin", Title = "Introduction to Chatbots", Summary = "A short introduction into Chatbots created with the Microsoft Bot Framework" };
         chatbotPresentation.Tags.Add("C #");
         chatbotPresentation.Tags.Add("Bot Framework");
         chatbotPresentation.Tags.Add("Azure");
         chatbotPresentation.Tags.Add("Machine Learning");

         var devNetwork1 = new DevNetwork { Date = new DateTime(2018, 03, 9, 13, 0, 0), Room = "Athen 5" };
         devNetwork1.Presentations.Add(chatbotPresentation);

         var restIntroduction = new Presentation { Presenter = "Lorenzo", Title = "REST APIs", Summary = "Introduction into REST APIs and why you should use them" };
         restIntroduction.Tags.Add("REST");
         var restExample = new Presentation { Presenter = "Lukas", Title = "Creating REST APIs with Asp.NET", Summary = "This presentation will show hands-on how to easily create a REST API with Asp.NET" };
         restExample.Tags.Add("C #");
         restExample.Tags.Add("Asp.NET");
         restExample.Tags.Add("REST");

         var devNetwork2 = new DevNetwork { Date = new DateTime(2018, 05, 21, 14, 0, 0), Room = "Boston" };
         devNetwork2.Presentations.Add(restIntroduction);
         devNetwork2.Presentations.Add(restExample);

         var iotWithTheRaspberryPi = new Presentation { Presenter = "Milos", Title = "Bringing the Internet of Things to the Raspberry PI", Summary = "Hands-on Session that explains how to use the Azure IoT Hub together with the Raspberry PI." };
         iotWithTheRaspberryPi.Tags.Add("Raspberry PI");
         iotWithTheRaspberryPi.Tags.Add("Azure");
         iotWithTheRaspberryPi.Tags.Add("Internet of Things");

         var securityPreseentation = new Presentation { Presenter = "Richard", Title = "Obviously some Security Topic", Summary = "Why Security Feature X should be used in ALL our Applications" };
         securityPreseentation.Tags.Add("Security");

         var devNetwork3 = new DevNetwork { Date = new DateTime(2018, 07, 12, 09, 0, 0), Room = "Brisbane" };
         devNetwork3.Presentations.Add(iotWithTheRaspberryPi);
         devNetwork3.Presentations.Add(securityPreseentation);

         devNetworks.Add(devNetwork1);
         devNetworks.Add(devNetwork2);
         devNetworks.Add(devNetwork3);
      }

      private async Task ShowGreetingMessageAsync(IDialogContext context, IAwaitable<object> result)
      {
         var activity = await result as Activity;

         await context.PostAsync($"Welcome to the DevNetwork Bot. How can I be of Service?");

         context.Wait(MessageReceived);
      }

      [LuisIntent("None")]
      private async Task HandleUknownMessageAsync(IDialogContext context, LuisResult result)
      {
         await context.PostAsync("Sorry I did not understand. Please try again.");
         context.Wait(MessageReceived);
      }

      [LuisIntent("ListDevNetworks")]
      private async Task ListDevNetworks(IDialogContext context, LuisResult result)
      {
         if (!devNetworks.Any())
         {
            await context.PostAsync("No DevNetworks added so far.");
         }

         foreach (var devNetwork in devNetworks)
         {
            await ShowDevNetworkInfo(context, devNetwork);
         }

         context.Wait(MessageReceived);
      }

      private static async Task ShowDevNetworkInfo(IDialogContext context, DevNetwork devNetwork)
      {
         var stringBuilder = new StringBuilder();

         stringBuilder.Append($"DevNetwork Session sheduled for {devNetwork.Date.ToShortDateString()} {devNetwork.Date.ToShortTimeString()} in room {devNetwork.Room}");
         stringBuilder.Append($"{Environment.NewLine}{Environment.NewLine}");

         foreach (var presentation in devNetwork.Presentations)
         {
            stringBuilder.Append($"• {presentation.Title} by {presentation.Presenter}: {presentation.Summary}");
            stringBuilder.Append($"{Environment.NewLine}{Environment.NewLine}");
         }

         await context.PostAsync(stringBuilder.ToString());
      }

      [LuisIntent("AddDevNetwork")]
      private Task AddDevNetworkSessionAsync(IDialogContext context, LuisResult result)
      {
         context.Call(new AddNewDevnetworkDialog(), AfterAddNewDevnetworkDialog);
         return Task.CompletedTask;
      }

      [LuisIntent("GetNextDevNetwork")]
      private async Task GetNextDevNetworkSessionAsync(IDialogContext context, LuisResult result)
      {
         var dateToday = DateTime.Now;

         var nextDevNetwork = devNetworks.OrderBy(x => x.Date).Where(x => x.Date > dateToday).FirstOrDefault();

         if (nextDevNetwork == null)
         {
            await context.PostAsync("Sorry I think there's no Session sheduled yet.");
         }
         else
         {
            await ShowDevNetworkInfo(context, nextDevNetwork);
         }

         context.Wait(MessageReceived);
      }

      [LuisIntent("GetPresentation")]
      private async Task GetPresentationAsync(IDialogContext context, LuisResult result)
      {
         var tag = string.Empty;
         var presenter = string.Empty;

         if (result.TryFindEntity("Tag", out var tagRecommendation))
         {
            tag = tagRecommendation.Entity;
         }

         if (result.TryFindEntity("Presenter", out var presenterRecommandation))
         {
            presenter = presenterRecommandation.Entity;
         }

         var matchingPresentations = FindPresentation(tag, presenter);

         if (!matchingPresentations.Any())
         {
            await context.PostAsync("Sorry I did not find any Presentation matching your criteria :-(");
         }
         else
         {
            foreach (var presentation in matchingPresentations)
            {
               await ShowPresentationInfo(context, presentation);
            }
         }

         context.Wait(MessageReceived);
      }

      private async Task ShowPresentationInfo(IDialogContext context, Presentation presentation)
      {
         await context.PostAsync($"{presentation.Title} by {presentation.Presenter}: {presentation.Summary}");
      }

      private IEnumerable<Presentation> FindPresentation(string tag, string presenter)
      {
         if (string.IsNullOrEmpty(tag) && string.IsNullOrEmpty(presenter))
         {
            return Enumerable.Empty<Presentation>();
         }

         var filteredPresentations = devNetworks.SelectMany(x => x.Presentations);

         if (!string.IsNullOrEmpty(tag))
         {
            filteredPresentations = filteredPresentations.Where(p => p.Tags.Any(t => t.ToUpper() == tag.ToUpper()));
         }

         if (!string.IsNullOrEmpty(presenter))
         {
            filteredPresentations = filteredPresentations.Where(p => p.Presenter.ToUpper().Contains(presenter.ToUpper()));
         }

         return filteredPresentations;
      }

      private async Task AfterAddNewDevnetworkDialog(IDialogContext context, IAwaitable<object> result)
      {
         lastAddedDevNetwork = await result as DevNetwork;
         devNetworks.Add(lastAddedDevNetwork);

         PromptDialog.Confirm(context, AfterQuestionIfPresentationShallBeAdded, "Do you want to add a Presentation to the DevNetwork?");         
      }

      private async Task AfterQuestionIfPresentationShallBeAdded(IDialogContext context, IAwaitable<bool> result)
      {
         var addPresentation = await result;

         if (addPresentation)
         {
            context.Call(FormDialog.FromForm(Presentation.BuildForm, FormOptions.PromptInStart), AfterAddPresentationDialog);
         }
         else
         {
            context.Wait(MessageReceived);
         }
      }

      private async Task AfterAddPresentationDialog(IDialogContext context, IAwaitable<Presentation> result)
      {
         lastAddedPresentation = await result;

         if (lastAddedPresentation != null)
         {
            lastAddedDevNetwork.Presentations.Add(lastAddedPresentation);

            await context.PostAsync("Added Presentation, please add some tags (separated by ,)");
            context.Wait(AfterTagsAdded);
         }
         else
         {
            await context.PostAsync("Cancelled Presentation creation...");
            context.Wait(MessageReceived);
         }
      }

      private async Task AfterTagsAdded(IDialogContext context, IAwaitable<object> result)
      {
         var activity = await result as Activity;
         var tags = activity.Text.Split(',').Select(x => x.Trim());

         foreach (var tag in tags)
         {
            lastAddedPresentation.Tags.Add(tag);
         }

         context.Wait(MessageReceived);
      }
   }
}