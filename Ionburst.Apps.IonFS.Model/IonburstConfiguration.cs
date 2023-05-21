namespace Ionburst.Apps.IonFS.Model
{
    public class IonburstConfiguration
    {
        public string Profile { get; set; }
        public string IonburstUri { get; set; }

        public override string ToString()
        {
            return $"{Profile}@{IonburstUri}";
        }
    }
}