using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using iTextSharp;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using PDFReader;
using System.Globalization;
using System.IO;

namespace SkyTeamTimeTableParser
{
    public class Program
    {
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
            public DateTime FlightDuration;
            public Boolean FlightCodeShare;
            public Boolean FlightNextDayArrival;
        }

        public static readonly List<string> _IATAAircraftCode = new List<string>() { "141", "142", "143", "146", "14F", "14X", "14Y", "14Z", "310", "312", "313", "318", "319", "31F", "31X", "31Y", "320", "321", "32S", "330", "332", "333", "340", "342", "343", "345", "346", "380", "38F", "703", "707", "70F", "70M", "717", "721", "722", "727", "72B", "72C", "72F", "72M", "72S", "72X", "72Y", "731", "732", "733", "734", "735", "736", "737", "738", "739", "73F", "73G", "73H", "73M", "73W", "73X", "73Y", "741", "742", "743", "744", "747", "74C", "74D", "74E", "74F", "74J", "74L", "74M", "74R", "74T", "74U", "74V", "74X", "74Y", "752", "753", "757", "75F", "75M", "762", "763", "764", "767", "76F", "76X", "76Y", "772", "773", "777", "A26", "A28", "A30", "A32", "A40", "A4F", "AB3", "AB4", "AB6", "ABB", "ABF", "ABX", "ABY", "ACD", "ACP", "ACT", "ALM", "AN4", "AN6", "AN7", "ANF", "APH", "AR1", "AR7", "AR8", "ARJ", "ARX", "AT4", "AT5", "AT7", "ATP", "ATR", "AX1", "AX8", "B11", "B12", "B13", "B14", "B15", "B72", "BE1", "BE2", "BEC", "BEH", "BEP", "BES", "BET", "BH2", "BNI", "BNT", "BUS", "CCJ", "CCX", "CD2", "CL4", "CN1", "CN2", "CNA", "CNC", "CNJ", "CNT", "CR1", "CR2", "CR7", "CR9", "CRJ", "CRV", "CS2", "CS5", "CV4", "CV5", "CVF", "CVR", "CVV", "CVX", "CVY", "CWC", "D10", "D11", "D1C", "D1F", "D1M", "D1X", "D1Y", "D28", "D38", "D3F", "D6F", "D8F", "D8L", "D8M", "D8Q", "D8T", "D8X", "D8Y", "D91", "D92", "D93", "D94", "D95", "D9C", "D9F", "D9F", "D9X", "DC3", "DC6", "DC8", "DC9", "DF2", "DF3", "DFL", "DH1", "DH2", "DH3", "DH4", "DH7", "DH8", "DHB", "DHC", "DHD", "DHH", "DHL", "DHO", "DHP", "DHR", "DHS", "DHT", "E70", "E90", "EM2", "EMB", "EMJ", "ER3", "ER4", "ERD", "ERJ", "F21", "F22", "F23", "F24", "F27", "F28", "F50", "F70", "FA7", "FK7", "FRJ", "GRG", "GRJ", "GRM", "GRS", "H25", "HEC", "HOV", "HS7", "I14", "I93", "I9F", "I9M", "I9X", "I9Y", "IL6", "IL7", "IL8", "IL9", "ILW", "J31", "J32", "J41", "JST", "JU5", "L10", "L11", "L15", "L1F", "L49", "L4T", "LCH", "LMO", "LOE", "LOF", "LOH", "LOM", "LRJ", "M11", "M1F", "M1M", "M80", "M81", "M82", "M83", "M87", "M88", "M90", "MBH", "MD9", "MIH", "MU2", "ND2", "NDC", "NDE", "NDH", "PA1", "PA2", "PAG", "PAT", "PL2", "PL6", "PN6", "RFS", "S20", "S58", "S61", "S76", "SF3", "SH3", "SH6", "SHB", "SHS", "SSC", "SWM", "T20", "TRN", "TU3", "TU5", "VCV", "WWP", "YK2", "YK4", "YN2", "YN7", "YS1" };

        static void Main(string[] args)
        {
            
            var text = new StringBuilder();
            CultureInfo ci = new CultureInfo("en-US");
            string path = AppDomain.CurrentDomain.BaseDirectory + "data\\oneworld.pdf";
            Regex rgxtime = new Regex(@"^([0-1]?[0-9]|[2][0-3]):([0-5][0-9])$");
            Regex rgxFlightNumber = new Regex(@"^([A-Z]{2}|[A-Z]\d|\d[A-Z])[0-9](\d{1,4})?(\*)?$");
            Regex rgxIATAAirport = new Regex(@"\(?[a-zA-Z]{3}\)"); // kan niet aan het begin staan kan ergens in de string staan dus geen $ op het einde.
            Regex rgxdate = new Regex(@"(([0-9])|([0-2][0-9])|([3][0-1]))(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)"); // kan meerdere keren voorkomen in string
            Regex rgxFlightDay = new Regex(@"\d$");
            List<CIFLight> CIFLights = new List<CIFLight> { };
            List<Rectangle> rectangles = new List<Rectangle>();

            //rectangles.Add(new Rectangle(x+(j*offset), (y+i*offset), offset, offset));
            float distanceInPixelsFromLeft = 0;
            float distanceInPixelsFromBottom = 0;
            float width = 253;//pdfReader.GetPageSize(page).Width / 2; // 306 deelt niet naar helft? 
            float height = 792; // pdfReader.GetPageSize(page).Height;
            // Formaat papaier 
            // Letter		 612x792
            // A4		     595x842
            var left = new Rectangle(
                        distanceInPixelsFromLeft,
                        distanceInPixelsFromBottom,
                        width,
                        height);

            var right = new Rectangle(
                       255,
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
                //float pageHeight = pdfReader.GetPageSize.Height;

                // Vaststellen valid from to date
                DateTime ValidFrom = new DateTime(2015, 5, 22);
                DateTime ValidTo = new DateTime(2015, 6, 19);
                
                
                // Loop through each page of the document
                for (var page = 7; page <= pdfReader.NumberOfPages; page++)
                //for (var page = 3; page <= pdfReader.NumberOfPages; page++)
                {

                    Console.WriteLine("Parsing page {0}...", page);
                    //float pageHeight = pdfReader.GetPageSize(page).Height;
                   
                    
                    //System.Diagnostics.Debug.WriteLine(currentText);


                    foreach (Rectangle rect in rectangles)
                    {
                        ITextExtractionStrategy its = new CSVTextExtractionStrategy();
                        var filters = new RenderFilter[1];
                        filters[0] = new RegionTextRenderFilter(rect);
                        //filters[1] = new RegionTextRenderFilter(rechts);

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
                        DateTime TEMP_DurationTime = new DateTime();
                        Boolean TEMP_FlightNextDayArrival = false;
                        foreach (string line in lines)
                        {
                            string[] values = line.SplitWithQualifier(',', '\"', true);

                            foreach (string value in values)
                            {
                                if (!String.IsNullOrEmpty(value.Trim()))
                                {
                                    // getrimde string temp value
                                    string temp_string = value.Trim();
                                    // Van en Naar
                                    if (rgxIATAAirport.Matches(temp_string).Count > 0)
                                    {
                                        if (String.IsNullOrEmpty(TEMP_FromIATA))
                                        {
                                            string tempairport = rgxIATAAirport.Match(temp_string).Groups[0].Value;
                                            tempairport = tempairport.Replace("(", "");
                                            tempairport = tempairport.Replace(")", "");
                                            TEMP_FromIATA = tempairport;
                                        }
                                        else
                                        {
                                            if (String.IsNullOrEmpty(TEMP_ToIATA) && !String.IsNullOrEmpty(TEMP_FromIATA))
                                            {

                                                string tempairport = rgxIATAAirport.Match(temp_string).Groups[0].Value;
                                                tempairport = tempairport.Replace("(", "");
                                                tempairport = tempairport.Replace(")", "");
                                                TEMP_FromIATA = tempairport;
                                                TEMP_ToIATA = tempairport;
                                            }
                                        }
                                    }
                                    // Valid from en to times
                                    if (String.Equals("-", temp_string) || temp_string.Substring(0, 1) == "-" || rgxdate.Matches(temp_string).Count > 0)
                                    {
                                        // Dit kan een validfrom or to zijn. Controle op basis van de Temp waarde. 
                                        if (TEMP_ValidFrom == DateTime.MinValue)
                                        {
                                            if (temp_string == "-" || temp_string.Substring(0, 1) == "-") { TEMP_ValidFrom = ValidFrom; }
                                            else
                                            {
                                                TEMP_ValidFrom = DateTime.ParseExact(rgxdate.Match(temp_string).Groups[0].Value, "dMMM", ci, DateTimeStyles.None);
                                            }
                                        }
                                        else
                                        {
                                            // Er is al een waarde voor from dus dit is de to.
                                            if (temp_string == "-" || temp_string.Substring(0, 1) == "-") { TEMP_ValidTo = ValidTo; }
                                            else
                                            {
                                                TEMP_ValidTo = DateTime.ParseExact(rgxdate.Match(temp_string).Groups[0].Value, "dMMM", ci, DateTimeStyles.None);
                                            }
                                        }
                                    }
                                    // dagen parsing kan een spatie bevatten tussen de nummers dit is niet bullet proof. 

                                    if (rgxFlightDay.Matches(temp_string).Count > 0 && !temp_string.Contains(":"))
                                    {
                                        // Vlucht dagen gevonden!
                                        foreach (Match ItemMatch in rgxFlightDay.Matches(temp_string))
                                        {
                                            int.TryParse(ItemMatch.Value, out TEMP_Conversie);
                                            if (TEMP_Conversie == 1) { TEMP_FlightMonday = true; }
                                            if (TEMP_Conversie == 2) { TEMP_FlightTuesday = true; }
                                            if (TEMP_Conversie == 3) { TEMP_FlightWednesday = true; }
                                            if (TEMP_Conversie == 4) { TEMP_FlightThursday = true; }
                                            if (TEMP_Conversie == 5) { TEMP_FlightFriday = true; }
                                            if (TEMP_Conversie == 6) { TEMP_FlightSaterday = true; }
                                            if (TEMP_Conversie == 7) { TEMP_FlightSunday = true; }
                                        }

                                    }
                                    // Vertrek en aankomst tijden
                                    if (temp_string.Length == 11 || temp_string.Length == 13)
                                    {
                                        // Aankomst tijd en datum? Controle basaal op :
                                        if (temp_string.Contains(":"))
                                        {
                                            // ok dit is aankomst datum en tijd.                                      

                                            string[] tijden = temp_string.Split(' ');
                                            foreach (string s in tijden)
                                            {
                                                if (TEMP_DepartTime == DateTime.MinValue)
                                                {
                                                    // tijd parsing                                                
                                                    DateTime.TryParse(s.Trim(), out TEMP_DepartTime);
                                                    //DateTime.TryParseExact(temp_string, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out TEMP_DepartTime);
                                                }
                                                else
                                                {
                                                    // Er is al een waarde voor from dus dit is de to.
                                                    string x = s;
                                                    if (s.Contains("+1"))
                                                    {
                                                        // Next day arrival
                                                        x = x.Replace("+1", "");
                                                        TEMP_FlightNextDayArrival = true;
                                                    }
                                                    DateTime.TryParse(s.Trim(), out TEMP_ArrivalTime);
                                                    //DateTime.TryParseExact(temp_string, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out TEMP_ArrivalTime);
                                                }
                                            }
                                        }
                                    }
                                    // Vluchtnumber parsing op basis van reg ex:
                                    // ^(([A-Za-z]{2,3})|([A-Za-z]\d)|(\d[A-Za-z]))(\d{1,})([A-Za-z]?)$ 
                                    // ^([A-Z]{2}|[A-Z]\d|\d[A-Z])[1-9](\d{1,3})?$ - IB0511
                                    // ^([A-Z]{2}|[A-Z]\d|\d[A-Z])[0-9](\d{1,4})?(\*)?$ - IB0511*
                                    // Nieuw rekening houdend met IB0511
                                    //Regex rgx = new Regex(@"^([A-Z]{2}|[A-Z]\d|\d[A-Z])[0-9](\d{1,4})?(\*)?$");
                                    if (rgxFlightNumber.IsMatch(temp_string) && !_IATAAircraftCode.Contains(temp_string, StringComparer.OrdinalIgnoreCase))
                                    {
                                        TEMP_FlightNumber = temp_string;
                                        if (temp_string.Contains("*"))
                                        {
                                            TEMP_FlightCodeShare = true;
                                            TEMP_FlightNumber = TEMP_FlightNumber.Replace("*", "");
                                        }
                                    }
                                    // Vliegtuig parsing
                                    if (temp_string.Length == 3)
                                    {
                                        if (_IATAAircraftCode.Contains(temp_string, StringComparer.OrdinalIgnoreCase))
                                        {
                                            TEMP_Aircraftcode = temp_string;
                                        }
                                    }
                                    if (TEMP_Aircraftcode != null && DateTime.TryParseExact(temp_string, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out TEMP_DurationTime))
                                    {
                                        // Aircraft code is gevonden, dit moet nu de vlucht tijd zijn. En dus de laatste waarde in de reeks. 
                                        TEMP_DurationTime = DateTime.ParseExact(temp_string, "HH:mm", null);
                                        CIFLights.Add(new CIFLight
                                        {
                                            FromIATA = TEMP_FromIATA,
                                            ToIATA = TEMP_ToIATA,
                                            FromDate = TEMP_ValidFrom,
                                            ToDate = TEMP_ValidTo,
                                            ArrivalTime = TEMP_ArrivalTime,
                                            DepartTime = TEMP_DepartTime,
                                            FlightAircraft = TEMP_Aircraftcode,
                                            FlightAirline = TEMP_FlightNumber.Substring(0, 2),
                                            FlightMonday = TEMP_FlightMonday,
                                            FlightTuesday = TEMP_FlightTuesday,
                                            FlightWednesday = TEMP_FlightWednesday,
                                            FlightThursday = TEMP_FlightThursday,
                                            FlightFriday = TEMP_FlightFriday,
                                            FlightSaterday = TEMP_FlightSaterday,
                                            FlightSunday = TEMP_FlightSunday,
                                            FlightNumber = TEMP_FlightNumber,
                                            FlightOperator = null,
                                            FlightDuration = TEMP_DurationTime,
                                            FlightCodeShare = TEMP_FlightCodeShare,
                                            FlightNextDayArrival = TEMP_FlightNextDayArrival
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
                                        TEMP_DurationTime = new DateTime();
                                        TEMP_FlightCodeShare = false;
                                        TEMP_FlightNextDayArrival = false;
                                    }
                                    if (temp_string.Contains("Operated by"))
                                    {
                                        // Ok dit moet worden toegevoegd aan het vorige record.
                                        CIFLights[CIFLights.Count - 1].FlightOperator = temp_string.Replace("Operated by ", "");
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
                                        TEMP_DurationTime = new DateTime();
                                        TEMP_FlightCodeShare = false;
                                        TEMP_FlightNextDayArrival = false;
                                    }
                                    //Console.WriteLine(value);
                                }
                            }
                        }

                    }

                   
                    //text.Append(currentText);                    
                }
            }

            // You'll do something else with it, here I write it to a console window
            // Console.WriteLine(text.ToString());
            
            // Write the list of objects to a file.
            System.Xml.Serialization.XmlSerializer writer =
            new System.Xml.Serialization.XmlSerializer(CIFLights.GetType());
            System.IO.StreamWriter file =
               new System.IO.StreamWriter("output\\output.xml");

            writer.Serialize(file, CIFLights);
            file.Close();

            //Console.ReadKey();



        }     


        
        
    }

        }
    }
}
