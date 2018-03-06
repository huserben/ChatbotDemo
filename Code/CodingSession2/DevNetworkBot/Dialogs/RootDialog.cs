using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevNetworkBot.Model;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Connector;

namespace DevNetworkBot.Dialogs
{
   [Serializable]
   public class RootDialog : IDialog<object>
   {
      private readonly List<DevNetwork> devNetworks = new List<DevNetwork>();
      private DevNetwork lastAddedDevNetwork;
      private Presentation lastAddedPresentation;

      public Task StartAsync(IDialogContext context)
      {
         context.Wait(MessageReceivedAsync);

         return Task.CompletedTask;
      }

      private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
      {
         var activity = await result as Activity;

         await context.PostAsync($"Welcome to the DevNetwork Bot. How can I be of Service?");

         context.Wait(ExecuteActionsAsync);
      }

      private async Task ExecuteActionsAsync(IDialogContext context, IAwaitable<object> result)
      {
         var activity = await result as Activity;

         if (activity.Text.ToUpper().Contains("DEVNETWORK"))
         {
            await AddDevNetworkSessionAsync(context);
         }
         else if (activity.Text.ToUpper().Contains("LIST"))
         {
            await ListDevNetworks(context);
            context.Wait(ExecuteActionsAsync);
         }
         else
         {
            await context.PostAsync("Sorry I did not understand. Please try again.");
            context.Wait(ExecuteActionsAsync);
         }
      }

      private async Task ListDevNetworks(IDialogContext context)
      {
         if (!devNetworks.Any())
         {
            await context.PostAsync("No DevNetworks added so far.");
         }

         foreach (var devNetwork in devNetworks)
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
      }

      private Task AddDevNetworkSessionAsync(IDialogContext context)
      {
         context.Call(new AddNewDevnetworkDialog(), AfterAddNewDevnetworkDialog);
         return Task.CompletedTask;
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
            context.Wait(ExecuteActionsAsync);
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
            context.Wait(ExecuteActionsAsync);
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

         context.Wait(ExecuteActionsAsync);
      }
   }
}