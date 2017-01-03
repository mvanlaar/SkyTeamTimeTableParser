using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
//using iTextSharp;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using PDFReader;
using System.Globalization;
using System.IO;
using System.Data.SqlClient;
using System.Data;
using System.Net;
using CsvHelper;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
using System.Configuration;
using Newtonsoft.Json;

namespace SkyTeamTimeTableParser
{
    class Program
    {
        public static readonly List<string> _SkyTeamAircraftCode = new List<string>() { "100", "313", "319", "321", "330", "333", "343", "346", "388", "733", "735", "310", "318", "320", "32S", "332", "340", "345", "380", "717", "734", "736", "737", "739", "73G", "73J", "73W", "747", "74M", "753", "75W", "763", "767", "772", "777", "77W", "788", "AB6", "AT5", "ATR", "CR2", "CR9", "CRK", "E70", "E90", "EM2", "EQV", "ER4", "F50", "M11", "M90", "SF3", "738", "73C", "73H", "73R", "744", "74E", "752", "757", "762", "764", "76W", "773", "77L", "787", "A81", "AR8", "AT7", "BUS", "CR7", "CRJ", "DH4", "E75", "E95", "EMJ", "ER3", "ERJ", "F70", "M88", "S20", "SU9" };
        public static readonly List<string> _ColombiaAirports = new List<string>() { "APO", "AUC", "AXM", "BSC", "EJA", "BAQ", "BOG", "BGA", "BUN", "CLO", "CTG", "CRC", "CZU", "CUC", "EYP", "FLA", "GIR", "GPI", "IBE", "LET", "MZL", "MQU", "EOH", "MDE", "MVP", "MTR", "NVA", "PSO", "PEI", "PPN", "PVA", "PUU", "PCR", "UIB", "RCH", "ADZ", "SJE", "SVI", "SMR", "RVE", "TME", "TLU", "TCO", "VUP", "VVC", "ACD", "AFI", "ACR", "ARQ", "NBB", "CPB", "CCO", "CUO", "CAQ", "CPL", "IGO", "CIM", "COG", "RAV", "BHF", "EBG", "ELB", "ECR", "LGT", "HTZ", "IPI", "JUO", "LMC", "LPD", "LPE", "MGN", "MCJ", "MFS", "MMP", "MTB", "NCI", "NQU", "OCV", "ORC", "RON", "PZA", "PTX", "PLT", "PBE", "PDA", "LQM", "NAR", "OTU", "SNT", "AYG", "SSL", "SOX", "TTM", "TCD", "TIB", "TBD", "TDA", "TRB", "URI", "URR", "VGZ", "LCR", "SQE", "SRS", "ULQ", "CVE", "PAL", "PYA", "TQS", "API"};

        static void Main(string[] args)
        {

            // Downlaoding latest pdf from skyteam website
            string path = AppDomain.CurrentDomain.BaseDirectory + "data\\Skyteam_Timetable.pdf";
            string dataDir = AppDomain.CurrentDomain.BaseDirectory + "\\data";
            Directory.CreateDirectory(dataDir);

            string APIPathAirport = "airport/iata/";
            string APIPathAirline = "airline/iata/";


            Uri url = new Uri("https://services.skyteam.com/Timetable/Skyteam_Timetable.pdf");            
            const string ua = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";
            const string referer = "http://www.skyteam.com/nl/Flights-and-Destinations/Download-Timetables/";
            if (!File.Exists(path))
            {
                WebRequest.DefaultWebProxy = null;
                using (System.Net.WebClient wc = new WebClient())
                {
                    wc.Headers.Add("user-agent", ua);
                    wc.Headers.Add("Referer", referer);
                    wc.Proxy = null;
                    Console.WriteLine("Downloading latest skyteam timetable pdf file...");
                    wc.DownloadFile(url, path);
                    Console.WriteLine("Download ready...");
                }
            }
            var text = new StringBuilder();
            CultureInfo ci = new CultureInfo("en-US");

            Regex rgxtime = new Regex(@"^([0-1]?[0-9]|[2][0-3]):([0-5][0-9])(\+1)?(\+2)?(\+-1)?$");
            Regex rgxFlightNumber = new Regex(@"^([A-Z]{2}|[A-Z]\d|\d[A-Z])[0-9](\d{1,4})?(\*)?$");
            Regex rgxIATAAirport = new Regex(@"^[A-Z]{3}$");
            Regex rgxdate1 = new Regex(@"(([0-9])|([0-2][0-9])|([3][0-1])) (Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)");
            Regex rgxdate2 = new Regex(@"(([0-9])|([0-2][0-9])|([3][0-1])) (Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec) ([0-9]{4})");
            Regex rgxFlightDay = new Regex(@"^\d+$");
            Regex rgxFlightDay2 = new Regex(@"\s[1234567](\s|$)");
            Regex rgxFlightTime = new Regex(@"^([0-9]|0[0-9]|1[0-9]|2[0-3])H([0-9]|0[0-9]|1[0-9]|2[0-9]|3[0-9]|4[0-9]|5[0-9])M$");
            List<CIFLight> CIFLights = new List<CIFLight> { };
            List<Rectangle> rectangles = new List<Rectangle>();

            //rectangles.Add(new Rectangle(x+(j*offset), (y+i*offset), offset, offset));
            float distanceInPixelsFromLeft = 0;
            float distanceInPixelsFromBottom = 0;
            float width = 306;//pdfReader.GetPageSize(page).Width / 2;
            float height = 792; // pdfReader.GetPageSize(page).Height;
            // Format Paper
            // Letter		 612x792
            // A4		     595x842

            var firstpage = new Rectangle(
                        distanceInPixelsFromLeft,
                        distanceInPixelsFromBottom,
                        width,
                        height);

            var left = new Rectangle(
                        distanceInPixelsFromLeft,
                        distanceInPixelsFromBottom,
                        width,
                        height);

            var right = new Rectangle(
                       307,
                       distanceInPixelsFromBottom,
                       612,
                       height);


            rectangles.Add(left);
            rectangles.Add(right);

            // The PdfReader object implements IDisposable.Dispose, so you can
            // wrap it in the using keyword to automatically dispose of it
            Console.WriteLine("Opening PDF File...");

            //PdfReader reader = new PdfReader(path);


            using (var pdfReader = new PdfReader(path))
            {

                // Parsing first page for valid from and to dates.                

                ITextExtractionStrategy fpstrategy = new SimpleTextExtractionStrategy();

                var fpcurrentText = PdfTextExtractor.GetTextFromPage(
                    pdfReader,
                    1,
                    fpstrategy);

                fpcurrentText =
                    Encoding.UTF8.GetString(Encoding.Convert(
                        Encoding.Default,
                        Encoding.UTF8,
                        Encoding.Default.GetBytes(fpcurrentText)));

                MatchCollection matches = rgxdate2.Matches(fpcurrentText);

                string validfrom = matches[0].Value;
                string validto = matches[1].Value;

                string TEMP_PageFromIATA = null;
                string TEMP_PageToIATA = null;

                DateTime ValidFrom = DateTime.ParseExact(validfrom, "dd MMM yyyy", ci);
                DateTime ValidTo = DateTime.ParseExact(validto, "dd MMM yyyy", ci);
                // Loop through each page of the document
                for (var page = 5; page <= pdfReader.NumberOfPages; page++)
                {

                    Console.WriteLine("Parsing page {0}...", page);

                    foreach (Rectangle rect in rectangles)
                    {
                        ITextExtractionStrategy its = new CSVTextExtractionStrategy();
                        var filters = new RenderFilter[1];
                        filters[0] = new RegionTextRenderFilter(rect);

                        ITextExtractionStrategy strategy =
                            new FilteredTextRenderListener(
                                new CSVTextExtractionStrategy(), // new LocationTextExtractionStrategy()
                                filters);

                        var currentText = PdfTextExtractor.GetTextFromPage(
                            pdfReader,
                            page,
                            strategy);

                        currentText =
                            Encoding.UTF8.GetString(Encoding.Convert(
                                Encoding.Default,
                                Encoding.UTF8,
                                Encoding.Default.GetBytes(currentText)));

                        string[] lines = Regex.Split(currentText, "\r\n");
                        string TEMP_FromIATA = null;
                        string TEMP_ToIATA = null;
                        DateTime TEMP_ValidFrom = new DateTime();
                        DateTime TEMP_ValidTo = new DateTime();
                        int TEMP_Conversie = 0;
                        Boolean TEMP_FlightMonday = false;
                        Boolean TEMP_FlightTuesday = false;
                        Boolean TEMP_FlightWednesday = false;
                        Boolean TEMP_FlightThursday = false;
                        Boolean TEMP_FlightFriday = false;
                        Boolean TEMP_FlightSaterday = false;
                        Boolean TEMP_FlightSunday = false;
                        DateTime TEMP_DepartTime = new DateTime();
                        DateTime TEMP_ArrivalTime = new DateTime();
                        Boolean TEMP_FlightCodeShare = false;
                        string TEMP_FlightNumber = null;
                        string TEMP_Aircraftcode = null;
                        TimeSpan TEMP_DurationTime = TimeSpan.MinValue;
                        Boolean TEMP_FlightNextDayArrival = false;
                        int TEMP_FlightNextDays = 0;
                        foreach (string line in lines)
                        {
                            string[] values = line.SplitWithQualifier(',', '\"', true);

                            foreach (string value in values)
                            {
                                if (!String.IsNullOrEmpty(value.Trim()))
                                {
                                    // Trim the string
                                    string temp_string = value.Trim();
                                    // From and To
                                    if ((rgxIATAAirport.Matches(temp_string).Count > 0) && !(_SkyTeamAircraftCode.Contains(temp_string, StringComparer.OrdinalIgnoreCase)))
                                    {
                                        if (String.IsNullOrEmpty(TEMP_FromIATA))
                                        {
                                            TEMP_FromIATA = rgxIATAAirport.Match(temp_string).Groups[0].Value;
                                            TEMP_PageFromIATA = rgxIATAAirport.Match(temp_string).Groups[0].Value;
                                        }
                                        else
                                        {
                                            if (String.IsNullOrEmpty(TEMP_ToIATA) && !String.IsNullOrEmpty(TEMP_FromIATA))
                                            {
                                                TEMP_ToIATA = rgxIATAAirport.Match(temp_string).Groups[0].Value;
                                                TEMP_PageToIATA = rgxIATAAirport.Match(temp_string).Groups[0].Value;
                                            }
                                        }
                                    }
                                    // Valid from en to times
                                    if (String.Equals("-", temp_string) || temp_string.Substring(0, 1) == "-" || rgxdate1.Matches(temp_string).Count > 0)
                                    {
                                        // This can be a valid from or to. Check based on temp variable min value 
                                        if (TEMP_ValidFrom == DateTime.MinValue)
                                        {
                                            if (temp_string == "-" || temp_string.Substring(0, 1) == "-") { TEMP_ValidFrom = ValidFrom; }
                                            else
                                            {
                                                TEMP_ValidFrom = DateTime.ParseExact(rgxdate1.Matches(temp_string)[0].Value, "d MMM", ci, DateTimeStyles.None);
                                            }
                                        }
                                        if (TEMP_ValidTo == DateTime.MinValue)
                                        {
                                            if (temp_string == "-" || temp_string.Substring(0, 1) == "-") { TEMP_ValidTo = ValidTo; }
                                            else
                                            {
                                                string date2 = rgxdate1.Matches(temp_string)[1].Value;
                                                TEMP_ValidTo = DateTime.ParseExact(rgxdate1.Matches(temp_string)[1].Value, "d MMM", ci, DateTimeStyles.None);
                                            }
                                        }
                                    }
                                    // Parsing flightdays
                                    if (rgxFlightDay.Matches(temp_string).Count > 0 || rgxFlightDay2.Matches(temp_string).Count > 0)
                                    {
                                        // Flight days found!
                                        string y = null;
                                        if (rgxFlightDay2.Matches(temp_string).Count > 0)
                                        {
                                            foreach (Match ItemMatch in rgxFlightDay2.Matches(temp_string))
                                            {
                                                y = y + ItemMatch.Value;
                                            }
                                            y = y.Replace(" ", "");
                                        }
                                        else { y = temp_string; }
                                        y = y.Trim();

                                        if (!_SkyTeamAircraftCode.Contains(y, StringComparer.OrdinalIgnoreCase))
                                        {
                                            // Check to see it is not the airplane type.
                                            char[] arr;
                                            arr = y.ToCharArray();

                                            foreach (char c in arr)
                                            {
                                                int.TryParse(c.ToString(), out TEMP_Conversie);
                                                if (TEMP_Conversie == 1) { TEMP_FlightSunday = true; }
                                                if (TEMP_Conversie == 2) { TEMP_FlightMonday = true; }
                                                if (TEMP_Conversie == 3) { TEMP_FlightTuesday = true; }
                                                if (TEMP_Conversie == 4) { TEMP_FlightWednesday = true; }
                                                if (TEMP_Conversie == 5) { TEMP_FlightThursday = true; }
                                                if (TEMP_Conversie == 6) { TEMP_FlightFriday = true; }
                                                if (TEMP_Conversie == 7) { TEMP_FlightSaterday = true; }

                                            }
                                        }

                                    }
                                    // Depart and arrival times
                                    if (rgxtime.Matches(temp_string).Count > 0)
                                    {

                                        if (TEMP_DepartTime == DateTime.MinValue)
                                        {
                                            // Time Parsing                                             
                                            DateTime.TryParse(temp_string.Trim(), out TEMP_DepartTime);
                                        }
                                        else
                                        {
                                            // There is a from value so this is to.
                                            string x = temp_string;
                                            if (x.Contains("+1"))
                                            {
                                                // Next day arrival
                                                x = x.Replace("+1", "");
                                                TEMP_FlightNextDays = 1;
                                                TEMP_FlightNextDayArrival = true;
                                            }
                                            if (x.Contains("+2"))
                                            {
                                                // Next day arrival
                                                x = x.Replace("+2", "");
                                                TEMP_FlightNextDays = 2;
                                                TEMP_FlightNextDayArrival = true;
                                            }
                                            if (x.Contains("+-1"))
                                            {
                                                // Next day arrival
                                                x = x.Replace("+-1", "");
                                                TEMP_FlightNextDays = -1;
                                                TEMP_FlightNextDayArrival = true;
                                            }
                                            DateTime.TryParse(x.Trim(), out TEMP_ArrivalTime);
                                        }
                                    }
                                    // FlightNumber Parsing
                                    if (rgxFlightNumber.IsMatch(temp_string))
                                    {
                                        // Extra check for SU9 flight number and Aircraft Type
                                        if (TEMP_FlightNumber == null)
                                        {
                                            TEMP_FlightNumber = temp_string;
                                            if (temp_string.Contains("*"))
                                            {
                                                TEMP_FlightCodeShare = true;
                                                TEMP_FlightNumber = TEMP_FlightNumber.Replace("*", "");
                                            }
                                        }
                                    }
                                    // Aircraft parsing
                                    if (temp_string.Length == 3)
                                    {
                                        if (_SkyTeamAircraftCode.Contains(temp_string, StringComparer.OrdinalIgnoreCase))
                                        {
                                            if (TEMP_Aircraftcode == null)
                                            {
                                                TEMP_Aircraftcode = temp_string;
                                            }
                                        }
                                    }
                                    if (TEMP_Aircraftcode != null && rgxFlightTime.Matches(temp_string).Count > 0)
                                    {
                                        // Aircraft code has been found so this has to be the flighttimes. and so the last value of the string...
                                        int intFlightTimeH = 0;
                                        int intFlightTimeM = 0;
                                        var match = rgxFlightTime.Match(temp_string);
                                        intFlightTimeH = int.Parse(match.Groups[1].Value);
                                        intFlightTimeM = int.Parse(match.Groups[2].Value);
                                        TEMP_DurationTime = new TimeSpan(0, intFlightTimeH, intFlightTimeM, 0);
                                        //int rgxFlightTimeH = rgxFlightTime.Match(temp_string).Groups[0].Value
                                        //TEMP_DurationTime = DateTime.ParseExact(temp_string, "HH\H mm \M", null);
                                        string TEMP_Airline = null;
                                        if (TEMP_Aircraftcode == "BUS") { TEMP_Airline = null; }
                                        else { TEMP_Airline = TEMP_FlightNumber.Substring(0, 2); }


                                        // Bugfixing pages without flight destination or departure info. Check if we have a flightnumber equal to this flight. and use the from and to.
                                        if (TEMP_FromIATA == null)
                                        {
                                            CIFLight Flight = CIFLights.Find(item => item.FlightNumber == TEMP_FlightNumber);
                                            if (Flight != null) // check item isn't null
                                            {
                                                TEMP_FromIATA = Flight.FromIATA;
                                            }
                                            else
                                            {
                                                TEMP_FromIATA = TEMP_PageFromIATA;
                                            }
                                        }

                                        if (TEMP_ToIATA == null)
                                        {
                                            CIFLight Flight = CIFLights.Find(item => item.FlightNumber == TEMP_FlightNumber);
                                            if (Flight != null) // check item isn't null
                                            {
                                                TEMP_ToIATA = Flight.ToIATA;
                                            }
                                            else
                                            {
                                                TEMP_ToIATA = TEMP_PageToIATA;
                                            }
                                        }

                                        // If there is still no from and to information we have to parse it from the previous page or pages before it because it can stand 3 pages back?

                                        // Adding only flights to or from or in Colombia
                                        if (_ColombiaAirports.Contains(TEMP_FromIATA, StringComparer.OrdinalIgnoreCase) & !_ColombiaAirports.Contains(TEMP_ToIATA, StringComparer.OrdinalIgnoreCase) || (_ColombiaAirports.Contains(TEMP_ToIATA, StringComparer.OrdinalIgnoreCase) & !_ColombiaAirports.Contains(TEMP_FromIATA, StringComparer.OrdinalIgnoreCase)) || (_ColombiaAirports.Contains(TEMP_ToIATA, StringComparer.OrdinalIgnoreCase) & _ColombiaAirports.Contains(TEMP_FromIATA, StringComparer.OrdinalIgnoreCase)))
                                        {
                                            bool alreadyExists = CIFLights.Exists(x => x.FromIATA == TEMP_FromIATA
                                            && x.ToIATA == TEMP_ToIATA
                                            && x.FromDate == TEMP_ValidFrom
                                            && x.ToDate == TEMP_ValidTo
                                            && x.FlightNumber == TEMP_FlightNumber
                                            && x.FlightAirline == TEMP_Airline
                                            && x.FlightMonday == TEMP_FlightMonday
                                            && x.FlightTuesday == TEMP_FlightTuesday
                                            && x.FlightWednesday == TEMP_FlightWednesday
                                            && x.FlightThursday == TEMP_FlightThursday
                                            && x.FlightFriday == TEMP_FlightFriday
                                            && x.FlightSaterday == TEMP_FlightSaterday
                                            && x.FlightSunday == TEMP_FlightSunday);

                                            if (alreadyExists)
                                            {
                                                Console.WriteLine("Flight Already found...");
                                            }
                                            else
                                            {
                                                CIFLights.Add(new CIFLight
                                                {
                                                    FromIATA = TEMP_FromIATA,
                                                    ToIATA = TEMP_ToIATA,
                                                    FromDate = TEMP_ValidFrom,
                                                    ToDate = TEMP_ValidTo,
                                                    ArrivalTime = TEMP_ArrivalTime,
                                                    DepartTime = TEMP_DepartTime,
                                                    FlightAircraft = TEMP_Aircraftcode,
                                                    FlightAirline = TEMP_Airline,
                                                    FlightMonday = TEMP_FlightMonday,
                                                    FlightTuesday = TEMP_FlightTuesday,
                                                    FlightWednesday = TEMP_FlightWednesday,
                                                    FlightThursday = TEMP_FlightThursday,
                                                    FlightFriday = TEMP_FlightFriday,
                                                    FlightSaterday = TEMP_FlightSaterday,
                                                    FlightSunday = TEMP_FlightSunday,
                                                    FlightNumber = TEMP_FlightNumber,
                                                    FlightOperator = null,
                                                    FlightDuration = TEMP_DurationTime.ToString(),
                                                    FlightCodeShare = TEMP_FlightCodeShare,
                                                    FlightNextDayArrival = TEMP_FlightNextDayArrival,
                                                    FlightNextDays = TEMP_FlightNextDays
                                                });
                                            }
                                            // Cleaning All but From and To 
                                            TEMP_ValidFrom = new DateTime();
                                            TEMP_ValidTo = new DateTime();
                                            TEMP_Conversie = 0;
                                            TEMP_FlightMonday = false;
                                            TEMP_FlightTuesday = false;
                                            TEMP_FlightWednesday = false;
                                            TEMP_FlightThursday = false;
                                            TEMP_FlightFriday = false;
                                            TEMP_FlightSaterday = false;
                                            TEMP_FlightSunday = false;
                                            TEMP_DepartTime = new DateTime();
                                            TEMP_ArrivalTime = new DateTime();
                                            TEMP_FlightNumber = null;
                                            TEMP_Aircraftcode = null;
                                            TEMP_DurationTime = TimeSpan.MinValue;
                                            TEMP_FlightCodeShare = false;
                                            TEMP_FlightNextDayArrival = false;
                                            TEMP_FlightNextDays = 0;
                                        }
                                        if (temp_string.Contains("Operated by: "))
                                        {
                                            // Ok, this has to be added to the last record.
                                            CIFLights[CIFLights.Count - 1].FlightOperator = temp_string.Replace("Operated by: ", "").Trim();
                                            CIFLights[CIFLights.Count - 1].FlightCodeShare = true;
                                        }
                                        if (temp_string.Equals("Consult your travel agent for details"))
                                        {
                                            TEMP_ToIATA = null;
                                            TEMP_FromIATA = null;
                                            TEMP_ValidFrom = new DateTime();
                                            TEMP_ValidTo = new DateTime();
                                            TEMP_Conversie = 0;
                                            TEMP_FlightMonday = false;
                                            TEMP_FlightTuesday = false;
                                            TEMP_FlightWednesday = false;
                                            TEMP_FlightThursday = false;
                                            TEMP_FlightFriday = false;
                                            TEMP_FlightSaterday = false;
                                            TEMP_FlightSunday = false;
                                            TEMP_DepartTime = new DateTime();
                                            TEMP_ArrivalTime = new DateTime();
                                            TEMP_FlightNumber = null;
                                            TEMP_Aircraftcode = null;
                                            TEMP_DurationTime = TimeSpan.MinValue;
                                            TEMP_FlightCodeShare = false;
                                            TEMP_FlightNextDayArrival = false;
                                            TEMP_FlightNextDays = 0;
                                        }
                                    }



                                        
                                    
                                    //Console.WriteLine(value);
                                }
                            }
                        }

                    }
                }
            }

            // You'll do something else with it, here I write it to a console window
            // Console.WriteLine(text.ToString());

            // Write the list of objects to a file.
            System.Xml.Serialization.XmlSerializer writer =
            new System.Xml.Serialization.XmlSerializer(CIFLights.GetType());
            string myDir = AppDomain.CurrentDomain.BaseDirectory + "\\output";
            Directory.CreateDirectory(myDir);
            StreamWriter file =
               new System.IO.StreamWriter("output\\output.xml");

            writer.Serialize(file, CIFLights);
            file.Close();

            
            // GTFS Support

            string gtfsDir = AppDomain.CurrentDomain.BaseDirectory + "\\gtfs";
            System.IO.Directory.CreateDirectory(gtfsDir);

            Console.WriteLine("Creating GTFS Files...");

            Console.WriteLine("Creating GTFS File agency.txt...");
            using (var gtfsagency = new StreamWriter(@"gtfs\\agency.txt"))
            {
                var csv = new CsvWriter(gtfsagency);
                csv.Configuration.Delimiter = ",";
                csv.Configuration.Encoding = Encoding.UTF8;
                csv.Configuration.TrimFields = true;
                // header 
                csv.WriteField("agency_id");
                csv.WriteField("agency_name");
                csv.WriteField("agency_url");
                csv.WriteField("agency_timezone");
                csv.WriteField("agency_lang");
                csv.WriteField("agency_phone");
                csv.WriteField("agency_fare_url");
                csv.WriteField("agency_email");
                csv.NextRecord();

                var airlines = CIFLights.Select(m => new { m.FlightAirline }).Distinct().ToList();

                for (int i = 0; i < airlines.Count; i++) // Loop through List with for)
                {
                    using (var client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;
                        string urlapi = ConfigurationManager.AppSettings.Get("APIUrl") + APIPathAirline + airlines[i].FlightAirline;
                        var jsonapi = client.DownloadString(urlapi);
                        dynamic AirlineResponseJson = JsonConvert.DeserializeObject(jsonapi);
                        csv.WriteField(Convert.ToString(AirlineResponseJson[0].code));
                        csv.WriteField(Convert.ToString(AirlineResponseJson[0].name));
                        csv.WriteField(Convert.ToString(AirlineResponseJson[0].website));
                        csv.WriteField("America/Bogota");
                        csv.WriteField("ES");
                        csv.WriteField(Convert.ToString(AirlineResponseJson[0].phone));
                        csv.WriteField("");
                        csv.WriteField("");
                        csv.NextRecord();
                    }
                }

                //csv.WriteField("AV");
                //csv.WriteField("Avianca");
                //csv.WriteField("http://www.avianca.com");
                //csv.WriteField("America/Bogota");
                //csv.WriteField("ES");
                //csv.WriteField("");
                //csv.WriteField("");
                //csv.WriteField("");
                //csv.NextRecord();
            }

            Console.WriteLine("Creating GTFS File routes.txt ...");

            using (var gtfsroutes = new StreamWriter(@"gtfs\\routes.txt"))
            {
                // Route record


                var csvroutes = new CsvWriter(gtfsroutes);
                csvroutes.Configuration.Delimiter = ",";
                csvroutes.Configuration.Encoding = Encoding.UTF8;
                csvroutes.Configuration.TrimFields = true;
                // header 
                csvroutes.WriteField("route_id");
                csvroutes.WriteField("agency_id");
                csvroutes.WriteField("route_short_name");
                csvroutes.WriteField("route_long_name");
                csvroutes.WriteField("route_desc");
                csvroutes.WriteField("route_type");
                csvroutes.WriteField("route_url");
                csvroutes.WriteField("route_color");
                csvroutes.WriteField("route_text_color");
                csvroutes.NextRecord();

                var routes = CIFLights.Select(m => new { m.FromIATA, m.ToIATA, m.FlightAirline }).Distinct().ToList();

                for (int i = 0; i < routes.Count; i++) // Loop through List with for)
                {
                    string FromAirportName = null;
                    string ToAirportName = null;
                    using (var client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;
                        string urlapi = ConfigurationManager.AppSettings.Get("APIUrl") + APIPathAirport + routes[i].FromIATA;
                        var jsonapi = client.DownloadString(urlapi);
                        dynamic AirportResponseJson = JsonConvert.DeserializeObject(jsonapi);
                        FromAirportName = Convert.ToString(AirportResponseJson[0].name);
                    }
                    using (var client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;
                        string urlapi = ConfigurationManager.AppSettings.Get("APIUrl") + APIPathAirport + routes[i].ToIATA;
                        var jsonapi = client.DownloadString(urlapi);
                        dynamic AirportResponseJson = JsonConvert.DeserializeObject(jsonapi);
                        ToAirportName = Convert.ToString(AirportResponseJson[0].name);
                    }
                    
                    csvroutes.WriteField(routes[i].FromIATA + routes[i].ToIATA + routes[i].FlightAirline);
                    csvroutes.WriteField(routes[i].FlightAirline);
                    csvroutes.WriteField(routes[i].FromIATA + routes[i].ToIATA);
                    csvroutes.WriteField(FromAirportName + " - " + ToAirportName);
                    csvroutes.WriteField(""); // routes[i].FlightAircraft + ";" + CIFLights[i].FlightAirline + ";" + CIFLights[i].FlightOperator + ";" + CIFLights[i].FlightCodeShare
                    csvroutes.WriteField(1102);
                    csvroutes.WriteField("");
                    csvroutes.WriteField("");
                    csvroutes.WriteField("");
                    csvroutes.NextRecord();
                }
            }

            // stops.txt

            List<string> agencyairportsiata =
             CIFLights.SelectMany(m => new string[] { m.FromIATA, m.ToIATA })
                     .Distinct()
                     .ToList();

            using (var gtfsstops = new StreamWriter(@"gtfs\\stops.txt"))
            {
                // Route record
                var csvstops = new CsvWriter(gtfsstops);
                csvstops.Configuration.Delimiter = ",";
                csvstops.Configuration.Encoding = Encoding.UTF8;
                csvstops.Configuration.TrimFields = true;
                // header                                 
                csvstops.WriteField("stop_id");
                csvstops.WriteField("stop_name");
                csvstops.WriteField("stop_desc");
                csvstops.WriteField("stop_lat");
                csvstops.WriteField("stop_lon");
                csvstops.WriteField("zone_id");
                csvstops.WriteField("stop_url");
                csvstops.WriteField("stop_timezone");
                csvstops.NextRecord();

                for (int i = 0; i < agencyairportsiata.Count; i++) // Loop through List with for)
                {
                    // Using API for airport Data.
                    using (var client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;
                        string urlapi = ConfigurationManager.AppSettings.Get("APIUrl") + APIPathAirport + agencyairportsiata[i];
                        var jsonapi = client.DownloadString(urlapi);
                        dynamic AirportResponseJson = JsonConvert.DeserializeObject(jsonapi);

                        csvstops.WriteField(Convert.ToString(AirportResponseJson[0].code));
                        csvstops.WriteField(Convert.ToString(AirportResponseJson[0].name));
                        csvstops.WriteField("");
                        csvstops.WriteField(Convert.ToString(AirportResponseJson[0].lat));
                        csvstops.WriteField(Convert.ToString(AirportResponseJson[0].lng));
                        csvstops.WriteField("");
                        csvstops.WriteField(Convert.ToString(AirportResponseJson[0].website));
                        csvstops.WriteField(Convert.ToString(AirportResponseJson[0].timezone));
                        csvstops.NextRecord();
                    }                    
                }
            }


            Console.WriteLine("Creating GTFS File trips.txt, stop_times.txt, calendar.txt ...");

            using (var gtfscalendar = new StreamWriter(@"gtfs\\calendar.txt"))
            {
                using (var gtfstrips = new StreamWriter(@"gtfs\\trips.txt"))
                {
                    using (var gtfsstoptimes = new StreamWriter(@"gtfs\\stop_times.txt"))
                    {
                        // Headers 
                        var csvstoptimes = new CsvWriter(gtfsstoptimes);
                        csvstoptimes.Configuration.Delimiter = ",";
                        csvstoptimes.Configuration.Encoding = Encoding.UTF8;
                        csvstoptimes.Configuration.TrimFields = true;
                        // header 
                        csvstoptimes.WriteField("trip_id");
                        csvstoptimes.WriteField("arrival_time");
                        csvstoptimes.WriteField("departure_time");
                        csvstoptimes.WriteField("stop_id");
                        csvstoptimes.WriteField("stop_sequence");
                        csvstoptimes.WriteField("stop_headsign");
                        csvstoptimes.WriteField("pickup_type");
                        csvstoptimes.WriteField("drop_off_type");
                        csvstoptimes.WriteField("shape_dist_traveled");
                        csvstoptimes.WriteField("timepoint");
                        csvstoptimes.NextRecord();

                        var csvtrips = new CsvWriter(gtfstrips);
                        csvtrips.Configuration.Delimiter = ",";
                        csvtrips.Configuration.Encoding = Encoding.UTF8;
                        csvtrips.Configuration.TrimFields = true;
                        // header 
                        csvtrips.WriteField("route_id");
                        csvtrips.WriteField("service_id");
                        csvtrips.WriteField("trip_id");
                        csvtrips.WriteField("trip_headsign");
                        csvtrips.WriteField("trip_short_name");
                        csvtrips.WriteField("direction_id");
                        csvtrips.WriteField("block_id");
                        csvtrips.WriteField("shape_id");
                        csvtrips.WriteField("wheelchair_accessible");
                        csvtrips.WriteField("bikes_allowed ");
                        csvtrips.NextRecord();

                        var csvcalendar = new CsvWriter(gtfscalendar);
                        csvcalendar.Configuration.Delimiter = ",";
                        csvcalendar.Configuration.Encoding = Encoding.UTF8;
                        csvcalendar.Configuration.TrimFields = true;
                        // header 
                        csvcalendar.WriteField("service_id");
                        csvcalendar.WriteField("monday");
                        csvcalendar.WriteField("tuesday");
                        csvcalendar.WriteField("wednesday");
                        csvcalendar.WriteField("thursday");
                        csvcalendar.WriteField("friday");
                        csvcalendar.WriteField("saturday");
                        csvcalendar.WriteField("sunday");
                        csvcalendar.WriteField("start_date");
                        csvcalendar.WriteField("end_date");
                        csvcalendar.NextRecord();

                        //1101 International Air Service
                        //1102 Domestic Air Service
                        //1103 Intercontinental Air Service
                        //1104 Domestic Scheduled Air Service


                        for (int i = 0; i < CIFLights.Count; i++) // Loop through List with for)
                        {

                            // Calender

                            csvcalendar.WriteField(CIFLights[i].FromIATA + CIFLights[i].ToIATA + CIFLights[i].FlightNumber.Replace(" ", "") + String.Format("{0:yyyyMMdd}", CIFLights[i].FromDate) + String.Format("{0:yyyyMMdd}", CIFLights[i].ToDate));
                            csvcalendar.WriteField(Convert.ToInt32(CIFLights[i].FlightMonday));
                            csvcalendar.WriteField(Convert.ToInt32(CIFLights[i].FlightTuesday));
                            csvcalendar.WriteField(Convert.ToInt32(CIFLights[i].FlightWednesday));
                            csvcalendar.WriteField(Convert.ToInt32(CIFLights[i].FlightThursday));
                            csvcalendar.WriteField(Convert.ToInt32(CIFLights[i].FlightFriday));
                            csvcalendar.WriteField(Convert.ToInt32(CIFLights[i].FlightSaterday));
                            csvcalendar.WriteField(Convert.ToInt32(CIFLights[i].FlightSunday));
                            csvcalendar.WriteField(String.Format("{0:yyyyMMdd}", CIFLights[i].FromDate));
                            csvcalendar.WriteField(String.Format("{0:yyyyMMdd}", CIFLights[i].ToDate));
                            csvcalendar.NextRecord();

                            // Trips
                            string FromAirportName = null;
                            string ToAirportName = null;
                            using (var client = new WebClient())
                            {
                                client.Encoding = Encoding.UTF8;
                                string urlapi = ConfigurationManager.AppSettings.Get("APIUrl") + APIPathAirport + CIFLights[i].FromIATA;
                                var jsonapi = client.DownloadString(urlapi);
                                dynamic AirportResponseJson = JsonConvert.DeserializeObject(jsonapi);
                                FromAirportName = Convert.ToString(AirportResponseJson[0].name);
                            }
                            using (var client = new WebClient())
                            {
                                client.Encoding = Encoding.UTF8;
                                string urlapi = ConfigurationManager.AppSettings.Get("APIUrl") + APIPathAirport + CIFLights[i].ToIATA;
                                var jsonapi = client.DownloadString(urlapi);
                                dynamic AirportResponseJson = JsonConvert.DeserializeObject(jsonapi);
                                ToAirportName = Convert.ToString(AirportResponseJson[0].name);
                            }


                            csvtrips.WriteField(CIFLights[i].FromIATA + CIFLights[i].ToIATA + CIFLights[i].FlightAirline);
                            csvtrips.WriteField(CIFLights[i].FromIATA + CIFLights[i].ToIATA + CIFLights[i].FlightNumber.Replace(" ", "") + String.Format("{0:yyyyMMdd}", CIFLights[i].FromDate) + String.Format("{0:yyyyMMdd}", CIFLights[i].ToDate));
                            csvtrips.WriteField(CIFLights[i].FromIATA + CIFLights[i].ToIATA + CIFLights[i].FlightNumber.Replace(" ", "") + String.Format("{0:yyyyMMdd}", CIFLights[i].FromDate) + String.Format("{0:yyyyMMdd}", CIFLights[i].ToDate));
                            csvtrips.WriteField(ToAirportName);
                            csvtrips.WriteField(CIFLights[i].FlightNumber);
                            csvtrips.WriteField("");
                            csvtrips.WriteField("");
                            csvtrips.WriteField("");
                            csvtrips.WriteField("1");
                            csvtrips.WriteField("");
                            csvtrips.NextRecord();

                            // Depart Record
                            csvstoptimes.WriteField(CIFLights[i].FromIATA + CIFLights[i].ToIATA + CIFLights[i].FlightNumber.Replace(" ", "") + String.Format("{0:yyyyMMdd}", CIFLights[i].FromDate) + String.Format("{0:yyyyMMdd}", CIFLights[i].ToDate));
                            csvstoptimes.WriteField(String.Format("{0:HH:mm:ss}", CIFLights[i].DepartTime));
                            csvstoptimes.WriteField(String.Format("{0:HH:mm:ss}", CIFLights[i].DepartTime));
                            csvstoptimes.WriteField(CIFLights[i].FromIATA);
                            csvstoptimes.WriteField("0");
                            csvstoptimes.WriteField("");
                            csvstoptimes.WriteField("0");
                            csvstoptimes.WriteField("0");
                            csvstoptimes.WriteField("");
                            csvstoptimes.WriteField("");
                            csvstoptimes.NextRecord();
                            // Arrival Record
                            if (!CIFLights[i].FlightNextDayArrival)
                            {
                                csvstoptimes.WriteField(CIFLights[i].FromIATA + CIFLights[i].ToIATA + CIFLights[i].FlightNumber.Replace(" ", "") + String.Format("{0:yyyyMMdd}", CIFLights[i].FromDate) + String.Format("{0:yyyyMMdd}", CIFLights[i].ToDate));
                                csvstoptimes.WriteField(String.Format("{0:HH:mm:ss}", CIFLights[i].ArrivalTime));
                                csvstoptimes.WriteField(String.Format("{0:HH:mm:ss}", CIFLights[i].ArrivalTime));
                                csvstoptimes.WriteField(CIFLights[i].ToIATA);
                                csvstoptimes.WriteField("2");
                                csvstoptimes.WriteField("");
                                csvstoptimes.WriteField("0");
                                csvstoptimes.WriteField("0");
                                csvstoptimes.WriteField("");
                                csvstoptimes.WriteField("");
                                csvstoptimes.NextRecord();
                            }
                            else
                            {
                                //add 24 hour for the gtfs time
                                int hour = CIFLights[i].ArrivalTime.Hour;
                                hour = hour + 24;
                                int minute = CIFLights[i].ArrivalTime.Minute;
                                string strminute = minute.ToString();
                                if (strminute.Length == 1) { strminute = "0" + strminute; }
                                csvstoptimes.WriteField(CIFLights[i].FromIATA + CIFLights[i].ToIATA + CIFLights[i].FlightNumber.Replace(" ", "") + String.Format("{0:yyyyMMdd}", CIFLights[i].FromDate) + String.Format("{0:yyyyMMdd}", CIFLights[i].ToDate));
                                csvstoptimes.WriteField(hour + ":" + strminute + ":00");
                                csvstoptimes.WriteField(hour + ":" + strminute + ":00");
                                csvstoptimes.WriteField(CIFLights[i].ToIATA);
                                csvstoptimes.WriteField("2");
                                csvstoptimes.WriteField("");
                                csvstoptimes.WriteField("0");
                                csvstoptimes.WriteField("0");
                                csvstoptimes.WriteField("");
                                csvstoptimes.WriteField("");
                                csvstoptimes.NextRecord();
                            }
                        }
                    }
                }
            }

            // Create Zip File
            string startPath = gtfsDir;
            string zipPath = myDir + "\\Skyteam.zip";
            if (File.Exists(zipPath)) { File.Delete(zipPath); }
            ZipFile.CreateFromDirectory(startPath, zipPath, CompressionLevel.Fastest, false);



        }
    }

    
    public class CIFLight
    {
        // Auto-implemented properties. 
        public string FromIATA;
        public string ToIATA;
        public DateTime FromDate;
        public DateTime ToDate;
        public Boolean FlightMonday;
        public Boolean FlightTuesday;
        public Boolean FlightWednesday;
        public Boolean FlightThursday;
        public Boolean FlightFriday;
        public Boolean FlightSaterday;
        public Boolean FlightSunday;
        public DateTime DepartTime;
        public DateTime ArrivalTime;
        public String FlightNumber;
        public String FlightAirline;
        public String FlightOperator;
        public String FlightAircraft;
        public String FlightDuration;
        public Boolean FlightCodeShare;
        public Boolean FlightNextDayArrival;
        public int FlightNextDays;
    }

}
