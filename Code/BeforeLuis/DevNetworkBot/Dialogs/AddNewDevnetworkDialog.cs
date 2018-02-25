using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevNetworkBot.Model;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace DevNetworkBot.Dialogs
{
   [Serializable]
   public class AddNewDevnetworkDialog : IDialog<object>
   {
      private readonly List<string> availableRooms = new List<string>
      {
         "Athen 5",
         "Boston",
         "Berlin"
      };

      private readonly DevNetwork createdDevNetwork = new DevNetwork();

      public async Task StartAsync(IDialogContext context)
      {
         await context.PostAsync("When should the DevNetwork be? (dd.MM.YYYY hh:mm)");
         context.Wait(AfterDateEntered);
      }

      private async Task AfterDateEntered(IDialogContext context, IAwaitable<object> result)
      {
         var activity = await result as Activity;
         var date = IsValidDate(activity.Text);

         if (date == DateTime.MinValue)
         {
            await context.PostAsync("Could not read date, plese try again");
            context.Wait(AfterDateEntered);
         }
         else
         {
            createdDevNetwork.Date = date;
            await context.PostAsync($"You entered following date: {date.ToShortDateString()} {date.ToShortTimeString()}");
            PromptDialog.Confirm(context, AfterDateValidation, "Is this date correct?");
         }
      }

      private async Task AfterDateValidation(IDialogContext context, IAwaitable<bool> result)
      {
         var keepDate = await result;

         if (keepDate)
         {
            PromptDialog.Choice(context, AfterRoomChoiceMade, availableRooms, "Choose the room you're hosting the Session");
         }
         else
         {
            await context.PostAsync("Ok, please enter a new date.");
            context.Wait(AfterDateEntered);
         }
      }

      private async Task AfterRoomChoiceMade(IDialogContext context, IAwaitable<string> result)
      {
         var selectedRoom = await result;
         createdDevNetwork.Room = selectedRoom;
         await context.PostAsync("Ok, we're done creating the dev network");

         context.Done(createdDevNetwork);
      }

      private DateTime IsValidDate(string dateAsText)
      {
         if (DateTime.TryParse(dateAsText, out var date))
         {
            return date;
         }

         return DateTime.MinValue;
      }
   }
}