using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Windows.Threading;
using Newtonsoft.Json;
using static System.Net.WebRequestMethods;
using System.IO;
using System.Windows;

namespace IPray
{
    internal class GetApiPrayerTime
    {
        HttpClient aladhinClientApi;

        string response;
        int countDay = 0;
        dynamic dataApiPrayer;
        bool dataReady;
        int theme;
        string cityLoc, CountryLoc;
        BinaryReader binaryReader;

        public GetApiPrayerTime()
        {
            dataReady = false;
            countDay = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
            try
            {
                binaryReader = new BinaryReader(new FileStream("config",FileMode.Open));
            }catch(IOException e)
            {
                Console.WriteLine(e.ToString()+"can't load file, loading default instead");
                new Action(async () => await SyncPrayerTime())();
                return;
            }
            try
            {
                theme = binaryReader.ReadInt32();
                CountryLoc = binaryReader.ReadString();
                cityLoc = binaryReader.ReadString();
            }catch(IOException e)
            {
                Console.WriteLine(e.ToString() + "can't load file, loading default instead");
                new Action(async () => await SyncPrayerTime())();
                return;
            }
            new Action(async () => await SyncPrayerTime(CountryLoc, cityLoc, 5))();
            binaryReader.Close();
        }

        public bool isDataReady()
        {
            return dataReady;
        }

        public string GetTimePrayToday(string namePray)
        {
            return dataApiPrayer.data[DateTime.Today.Date.Day - 1].timings[namePray];
        }

        public string GetTimePrayFoSpesificDay(string namePray, int dayInMonth)
        {
            return dataApiPrayer.data[dayInMonth].timings[namePray];
        }

        public string GetDateInHijri()
        {
            string day,month,year;
            day = dataApiPrayer.data[DateTime.Today.Date.Day - 1].date.hijri.day;
            month = dataApiPrayer.data[DateTime.Today.Date.Day - 1].date.hijri.month.en;
            year = dataApiPrayer.data[DateTime.Today.Date.Day - 1].date.hijri.year;
            return day + " " + month + " " + year + "H";
        }

        async public Task SyncPrayerTime()
        {
            aladhinClientApi = new HttpClient();
            response = await aladhinClientApi.GetStringAsync("http://api.aladhan.com/v1/calendarByCity/2024/5?city=Tokyo&country=Japan&method=5");
            dataApiPrayer = JsonConvert.DeserializeObject<dynamic>(response);
            dataReady = true;
        }

        public void CallerSyncPrayerTime(string country,string city,int method)
        {
            new Action(async()=> await SyncPrayerTime(country,city,method))();
        }
        async private Task SyncPrayerTime(string country,string city,int method)
        {
            dataReady = false;
            string ApiEditedLink = "http://api.aladhan.com/v1/calendarByCity/2024/5?city=" + city + "&country=" + country + "&method=" + method.ToString();
            aladhinClientApi = new HttpClient();
            response = await aladhinClientApi.GetStringAsync(ApiEditedLink);
            dataApiPrayer = JsonConvert.DeserializeObject<dynamic>(response);
            dataReady = true;
        }
    }
}
