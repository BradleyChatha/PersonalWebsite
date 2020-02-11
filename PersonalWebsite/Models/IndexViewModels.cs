using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PersonalWebsite.Models
{
    public class IndexLanguageCard
    {
        public DateTimeOffset  StartedUsing    { get; set; }
        public string          Name            { get; set; }
        public string          Logo            { get; set; }
        public int             Comfort         { get; set; }
        public int             Knowledge       { get; set; }

        public IndexLanguageCard(string name, string logo, DateTimeOffset startedUsing, int comfort, int knowledge)
        {
            this.StartedUsing = startedUsing;
            this.Name         = name;
            this.Logo         = logo;
            this.Comfort      = comfort;
            this.Knowledge    = knowledge;
        }
    }
}
