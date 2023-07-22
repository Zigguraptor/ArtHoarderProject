using ArtHoarderClient.Infrastructure;

namespace ArtHoarderClient.Models
{
    internal class NavigationTree : IPanelData
    {
        public string PanelTitle { get; set; } = "Navigation Tree";
    }
}