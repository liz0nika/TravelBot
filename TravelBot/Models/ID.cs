namespace TravelBot.Models
{
    public class ID
    {
            public bool status { get; set; }
            public string message { get; set; }
            public long timestamp { get; set; }
            public Datum1[] data { get; set; }
        }

        public class Datum1
        {
            public string title { get; set; }
            public string geoId { get; set; }
            public string documentId { get; set; }
            public string trackingItems { get; set; }
            public string secondaryText { get; set; }
        }

    }
