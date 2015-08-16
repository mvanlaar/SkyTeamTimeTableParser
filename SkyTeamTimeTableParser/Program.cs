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

namespace SkyTeamTimeTableParser
{
    class Program
    {
        public static readonly List<string> _SkyTeamAircraftCode = new List<string>() { "100","313","319","321","330","333","343","346","388","733","735","310","318","320","32S","332","340","345","380","717","734","736","737", "739", "73G", "73J", "73W", "747", "74M", "753", "75W", "763", "767", "772", "777", "77W", "788", "AB6", "AT5", "ATR", "CR2", "CR9", "CRK", "E70", "E90", "EM2", "EQV", "ER4", "F50", "M11", "M90", "SF3", "738", "73C", "73H", "73R", "744", "74E", "752", "757", "762", "764", "76W", "773", "77L", "787", "A81", "AR8", "AT7", "BUS", "CR7", "CRJ", "DH4", "E75", "E95", "EMJ", "ER3", "ERJ", "F70", "M88", "S20", "SU9" };

        static void Main(string[] args)
        {

            // Downlaoding latest pdf from skyteam website
            string path = AppDomain.CurrentDomain.BaseDirectory + "data\\Skyteam_Timetable.pdf";
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
                                    if (rgxIATAAirport.Matches(temp_string).Count > 0)
                                    {
                                        if (String.IsNullOrEmpty(TEMP_FromIATA))
                                        {                                            
                                            TEMP_FromIATA = rgxIATAAirport.Match(temp_string).Groups[0].Value;
                                        }
                                        else
                                        {
                                            if (String.IsNullOrEmpty(TEMP_ToIATA) && !String.IsNullOrEmpty(TEMP_FromIATA))
                                            {                                                
                                                TEMP_ToIATA = rgxIATAAirport.Match(temp_string).Groups[0].Value;
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
                                    if (rgxFlightDay.Matches(temp_string).Count > 0 || rgxFlightDay2.Matches(temp_string).Count > 0)                                    {
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

                                        if (!_SkyTeamAircraftCode.Contains(y, StringComparer.OrdinalIgnoreCase)) { 
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
                                        if (TEMP_FlightNumber == null) { 
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
                                            if (TEMP_Aircraftcode == null) { 
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
                                        if (TEMP_Aircraftcode == "BUS") { TEMP_Airline = null;}
                                        else { TEMP_Airline = TEMP_FlightNumber.Substring(0,2);}

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

            //Console.ReadKey();
            //Console.WriteLine("Insert into Database...");
            //for (int i = 0; i < CIFLights.Count; i++) // Loop through List with for)
            //{
            //    using (SqlConnection connection = new SqlConnection("Server=(local);Database=CI-Import;Trusted_Connection=True;"))
            //    {
            //        using (SqlCommand command = new SqlCommand())
            //        {
            //            command.Connection = connection;            // <== lacking
            //            command.CommandType = CommandType.StoredProcedure;
            //            command.CommandText = "InsertFlight";
            //            command.Parameters.Add(new SqlParameter("@FlightSource", "SkyTeam"));
            //            command.Parameters.Add(new SqlParameter("@FromIATA", CIFLights[i].FromIATA));
            //            command.Parameters.Add(new SqlParameter("@ToIATA", CIFLights[i].ToIATA));
            //            command.Parameters.Add(new SqlParameter("@FromDate", CIFLights[i].FromDate));
            //            command.Parameters.Add(new SqlParameter("@ToDate", CIFLights[i].ToDate));
            //            command.Parameters.Add(new SqlParameter("@FlightMonday", CIFLights[i].FlightMonday));
            //            command.Parameters.Add(new SqlParameter("@FlightTuesday", CIFLights[i].FlightTuesday));
            //            command.Parameters.Add(new SqlParameter("@FlightWednesday", CIFLights[i].FlightWednesday));
            //            command.Parameters.Add(new SqlParameter("@FlightThursday", CIFLights[i].FlightThursday));
            //            command.Parameters.Add(new SqlParameter("@FlightFriday", CIFLights[i].FlightFriday));
            //            command.Parameters.Add(new SqlParameter("@FlightSaterday", CIFLights[i].FlightSaterday));
            //            command.Parameters.Add(new SqlParameter("@FlightSunday", CIFLights[i].FlightSunday));
            //            command.Parameters.Add(new SqlParameter("@DepartTime", CIFLights[i].DepartTime));
            //            command.Parameters.Add(new SqlParameter("@ArrivalTime", CIFLights[i].ArrivalTime));
            //            command.Parameters.Add(new SqlParameter("@FlightNumber", CIFLights[i].FlightNumber));
            //            command.Parameters.Add(new SqlParameter("@FlightAirline", CIFLights[i].FlightAirline));
            //            command.Parameters.Add(new SqlParameter("@FlightOperator", CIFLights[i].FlightOperator));
            //            command.Parameters.Add(new SqlParameter("@FlightAircraft", CIFLights[i].FlightAircraft));
            //            command.Parameters.Add(new SqlParameter("@FlightCodeShare", CIFLights[i].FlightCodeShare));
            //            command.Parameters.Add(new SqlParameter("@FlightNextDayArrival", CIFLights[i].FlightNextDayArrival));
            //            command.Parameters.Add(new SqlParameter("@FlightDuration", CIFLights[i].FlightDuration));
            //            command.Parameters.Add(new SqlParameter("@FlightNextDays", CIFLights[i].FlightNextDays));
            //            foreach (SqlParameter parameter in command.Parameters)
            //            {
            //                if (parameter.Value == null)
            //                {
            //                    parameter.Value = DBNull.Value;
            //                }
            //            }


            //            try
            //            {
            //                connection.Open();
            //                int recordsAffected = command.ExecuteNonQuery();
            //            }

            //            finally
            //            {
            //                connection.Close();
            //            }
            //        }
            //    }
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
