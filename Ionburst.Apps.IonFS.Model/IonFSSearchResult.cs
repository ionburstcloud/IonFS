using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ionburst.Apps.IonFS.Model
{
    public class IonFSSearchResult
    {
        public string Name { get; set; }
        public string Tag { get; set; }
        public string Value { get; set; }

        public IonFSObject Object { get; set; }
    }
}