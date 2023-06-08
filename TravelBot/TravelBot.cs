using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bots.Types;

namespace TravelBot
{
    public class TravelBot
    {
        private static string _token { get; set; } = "6165743861:AAEn1dYdtp87vnlqGhO-ESjfVYCqkWnxnyY";

        public Dictionary<long, Client> allClients = new Dictionary<long, Client>();

        TelegramBotClient botClient = new TelegramBotClient(_token);
        CancellationToken cancellationToken = new CancellationToken();
        ReceiverOptions receiverOptions = new ReceiverOptions { AllowedUpdates = { } };
        private bool _isFeedbackText = false;
        public async Task Start()
        {
            botClient.StartReceiving(HandlerUpdateAsync, HandlerError, receiverOptions, cancellationToken);
            var botMe = await botClient.GetMeAsync();
            Console.WriteLine($"Bot {botMe.Username} starts to work");
            Console.ReadKey();

        }
        private Task HandlerError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Error in API:\n {apiRequestException.ErrorCode}" +
                $"\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
        private async Task HandlerUpdateAsync(ITelegramBotClient botClient, Telegram.Bot.Types.Update update, CancellationToken cancellationToken)
        {
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message && update?.Message?.Text != null)
            {
                await HandlerMessage(botClient, update.Message);
                return;
            }
            if (update?.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
            {
                await HandlerCallbackQuery(botClient, update.CallbackQuery);
                return;
            }
        }
        private async Task HandlerCallbackQuery(ITelegramBotClient botClient, Telegram.Bot.Types.CallbackQuery? callbackQuery)
        {
            Client client = null;
            for (int i = 0; i < allClients.Keys.Count; i++)
            {
                if (callbackQuery.Message.Chat.Id == allClients.Keys.ToList()[i])
                {
                    client = allClients[allClients.Keys.ToList()[i]];
                    break;
                }
                else continue;
            }
            if (client == null)
            {
                client = new Client();
                allClients.Add(callbackQuery.Message.Chat.Id, client);
            }

        }
        private async Task HandlerMessage(ITelegramBotClient botClient, Telegram.Bot.Types.Message message)
        {
            Client client = null;
            for (int i = 0; i < allClients.Keys.Count; i++)
            {
                if (message.Chat.Id == allClients.Keys.ToList()[i])
                {
                    client = allClients[allClients.Keys.ToList()[i]];
                    break;
                }
                else continue;
            }
            if (client == null)
            {
                client = new Client();
                allClients.Add(message.Chat.Id, client);
            }
            if (_isFeedbackText)
            {
                string Text = $"User {message.From.Username} sent a review:\n {message.Text}";
                Console.WriteLine(Text);
                _isFeedbackText = false;
                await botClient.SendTextMessageAsync(message.Chat.Id, "Thanks ✨");
                return;
            }
            if (message.Text == "/start")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Enter the city to travel");
                client._chooseCity = true;
                return;

            }
            else if (message.Type == MessageType.Text && client._chooseCity == true)
            {
                client._city = message.Text;
                client._chooseCity = false;
                await botClient.SendTextMessageAsync(message.Chat.Id, "To show the listed commands in the menu enter /keyboard:");
                return;
            }

            else if (message.Text == "/keyboard")
            {
                ReplyKeyboardMarkup keyboard = new(new[]
                {
                    new KeyboardButton[] { "Information", "Geo ID" },
                    new KeyboardButton[] { "Hotels", "Feedback" },
                })
                { ResizeKeyboard = true };
                await botClient.SendTextMessageAsync(message.Chat.Id, "Select one of the listed commands in the menu:", replyMarkup: keyboard);
                return;
            }
            else
            if (message.Text == "Feedback")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Your feedback: ");

                _isFeedbackText = true;
                return;
            }
            else if (message.Text == "Information")
            {
                string info = await Information(client._city);
                if (info == null)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Sorry, I didn't find the city you entered, press /start again to enter the city name");
                    return;
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"{info}\n");
                    return;
                }
            }
            //else if (message.Text == "Geo ID")
            //{
            //    string id = await ID(client._city);
            //    //string id = await ID(client._city);
            //    if (id == null)
            //    {
            //        await botClient.SendTextMessageAsync(message.Chat.Id, "Sorry, I didn't find the city you entered, press /start again to enter the city name");
            //        return;
            //    }
            //    else
            //    {
            //        await botClient.SendTextMessageAsync(message.Chat.Id, $"{id}\n");
            //        return;
            //    }
            //}

            else if (message.Text == "Hotels")
            {
                client.AllFalse();
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Enter the geo id:");
                client._geoidhotel = true;
                return;
            }
            else if (message.Type == MessageType.Text && client._geoidhotel == true)
            {
                client._geoidhotel = false;
                //client.geoidhotel = id;
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Enter the check-in date in this formate: xx.xx (month-day)");
                client._checkinHotel = true;
                return;
            }
            else if (message.Type == MessageType.Text && client._checkinHotel == true)
            {
                client._checkinHotel = false;

                string st = message.Text;
                Regex regex = new Regex(@"(\D)");
                client.checkinHotel = regex.Replace(st, "-");
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Enter the check-out date in this formate: xx.xx (month-day)");
                client._checkoutHotel = true;
                return;
            }
            else if (message.Type == MessageType.Text && client._checkoutHotel == true)
            {
                client._checkoutHotel = false;

                string st = message.Text;
                Regex regex = new Regex(@"(\D)");
                client.checkoutHotel = regex.Replace(st, "-");
                var hotels = await Hotels(client);
                if (hotels == null)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Sorry, nothing was found for your requst :(");
                    return;
                }
                //for (int i = 0; i < hotels.Length; i++)
                //{
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"{hotels}");
                //}
                return;
            }
        }
        public async Task<string> ID(string cityName)
        {
            HttpClient _client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://tripadvisor16.p.rapidapi.com/api/v1/hotels/searchLocation?query={cityName}"),
                Headers =
                {
                    { "X-RapidAPI-Key", "7d805f3e6amshdd2c28b9bcea802p1fe207jsnd38f60f14b10" },
                },
            };
            using (var response = await _client.SendAsync(request))
            {
                var body = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Models.ID>(body);
                string id = $"Information about {cityName}:\n ";
                if (result == null)
                {
                    return id = null;
                }

                string d_;
                for (int i = 0; i < result.data.Length; i++)
                {
                    d_ =
                        $"{result.data[i].geoId}";
                    //id = $"Information about {cityName}:\n {d_}";
                    id += d_;
                }
                return id;
            }
        }

        public async Task<string> Hotels(Client client)
        {

            HttpClient _client = new HttpClient();

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://tripadvisor16.p.rapidapi.com/api/v1/hotels/searchHotels?geoId={client.geoidhotel}&checkIn=2023-{client.checkinHotel}&checkOut=2023-{client.checkoutHotel}&pageNumber=1&adults=2&currencyCode=USD"),
                Headers =
                {
                    { "X-RapidAPI-Key", "7d805f3e6amshdd2c28b9bcea802p1fe207jsnd38f60f14b10" },
                },
            };
            using (var response = await _client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                client._hotels = JsonConvert.DeserializeObject<Models.Hotels>(body);
                string listhotels = "List of hotels:\n";
                for (int i = 0; i < client._hotels.data.data.Length; i++)
                {
                    for (int j = 0; j < client._hotels.data.data.Length; j++)
                    {
                        string hotel =
                            $"\nName: {client._hotels.data.data[i].title} \n\n" +
                            $"{client._hotels.data.data[i].primaryInfo}\n" +
                            $"{client._hotels.data.data[i].secondaryInfo}\n" +
                            $"Rating: {client._hotels.data.data[i].bubbleRating.rating}\n" +
                            $"{client._hotels.data.data[i].cardPhotos[j].sizes.urlTemplate}\n";
                        listhotels += hotel;
                    }
                }
                return listhotels;
            }

        }
        public async Task<string> Information(string cityName)
        {

            HttpClient _client = new HttpClient();

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://wiki-briefs.p.rapidapi.com/search?q={cityName}&topk=3"),
                Headers =
                {
                    { "X-RapidAPI-Key", "7d805f3e6amshdd2c28b9bcea802p1fe207jsnd38f60f14b10" },
                },
            };
            using (var response = await _client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Models.Information>(body);
                string information = $"Information about {cityName}:\n ";
                if (result == null)
                {
                    return information = null;
                }

                string day_;
                for (int i = 0; i < result.summary.Length; i++)
                {
                    day_ =
                        $"{result.title}\n" +
                        $"{result.image} \n" +
                        $"{result.summary[i]}\n";
                    information += day_;
                }
                return information;
            }
        }
    }
}
