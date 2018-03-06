using System;
using System.Collections.Generic;

namespace DevNetworkBot.Model
{ 
   [Serializable]
   public class DevNetwork
   {
      public DevNetwork()
      {
         Presentations = new List<Presentation>();
      }

      public List<Presentation> Presentations { get; }

      public string Room { get; set; }

      public DateTime Date { get; set; }
   }
}