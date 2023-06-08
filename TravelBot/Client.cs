using TravelBot.Models;

namespace TravelBot
{
    public class Client
    {
        public Hotels _hotels { get; set; }
        public ID _id { get; set; }

        public string _city { get; set; }
        public bool _chooseCity { get; set; } = false;
        public int geoidhotel { get; set; }
        public bool _geoidhotel { get; set; }
        public string checkinHotel { get; set; }
        public bool _checkinHotel { get; set; } = false;

        public string checkoutHotel { get; set; }
        public bool _checkoutHotel { get; set; } = false;
        public void AllFalse()
        {
            _checkinHotel = false;
            _checkoutHotel = false;
            _chooseCity = false;
            _geoidhotel = false;
        }
    }
}
