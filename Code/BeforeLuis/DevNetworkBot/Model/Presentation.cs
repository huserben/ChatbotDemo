using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder.FormFlow;

namespace DevNetworkBot.Model
{
   [Serializable]
   public class Presentation
   {
      public Presentation()
      {
         Tags = new List<string>();
      }

      public string Presenter { get; set; }

      public string Title { get; set; }

      public string Summary { get; set; }

      public List<string> Tags { get; }

      public static IForm<Presentation> BuildForm()
      {
         return new FormBuilder<Presentation>()
            .Field(nameof(Presenter))
            .Field(nameof(Title))
            .Field(nameof(Summary))
            .Build();
      }
   }
}