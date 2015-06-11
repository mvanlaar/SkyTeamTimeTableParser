using System;
using System.Text;

using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace PDFReader
{
    public class CSVTextExtractionStrategy : ITextExtractionStrategy
    {
        private Vector lastStart;
        private Vector lastEnd;
        private StringBuilder result = new StringBuilder(); //used to store the resulting string

        public CSVTextExtractionStrategy()
        {
        }

        public void BeginTextBlock()
        {
        }

        public void EndTextBlock()
        {
        }

        public String GetResultantText()
        {
            return result.ToString();
        }

        /**
         * Captures text using a simplified algorithm for inserting hard returns and spaces
         * @param   renderInfo  render info
         */
        public void RenderText(TextRenderInfo renderInfo)
        {
            bool firstRender = result.Length == 0;
            bool hardReturn = false;

            LineSegment segment = renderInfo.GetBaseline();
            Vector start = segment.GetStartPoint();
            Vector end = segment.GetEndPoint();

            if (!firstRender)
            {
                Vector x0 = start;
                Vector x1 = lastStart;
                Vector x2 = lastEnd;

                // see http://mathworld.wolfram.com/Point-LineDistance2-Dimensional.html
                float dist = (x2.Subtract(x1)).Cross((x1.Subtract(x0))).LengthSquared / x2.Subtract(x1).LengthSquared;

                float sameLineThreshold = 1f; // we should probably base this on the current font metrics, but 1 pt seems to be sufficient for the time being
                if (dist > sameLineThreshold)
                    hardReturn = true;

                // Note:  Technically, we should check both the start and end positions, in case the angle of the text changed without any displacement
                // but this sort of thing probably doesn't happen much in reality, so we'll leave it alone for now
            }

            if (hardReturn)
            {
                //System.out.Println("<< Hard Return >>");
                result.Append(Environment.NewLine);
            }
            else if (!firstRender)
            {
                if (result[result.Length - 1] != ' ' && renderInfo.GetText().Length > 0 && renderInfo.GetText()[0] != ' ')
                { // we only insert a blank space if the trailing character of the previous string wasn't a space, and the leading character of the current string isn't a space
                    float spacing = lastEnd.Subtract(start).Length;
                    if (spacing > renderInfo.GetSingleSpaceWidth() / 2f)
                    {
                        result.Append(',');
                        //System.out.Println("Inserting implied space before '" + renderInfo.GetText() + "'");
                    }
                }
            }
            else
            {
                //System.out.Println("Displaying first string of content '" + text + "' :: x1 = " + x1);
            }

            //System.out.Println("[" + renderInfo.GetStartPoint() + "]->[" + renderInfo.GetEndPoint() + "] " + renderInfo.GetText());
            //strings can be rendered in contiguous bits, so check last character for " and remove it if we need
            //to stick two rendered strings together to form one string in the output
            if ((!firstRender) && (result[result.Length - 1] == '\"'))
            {
                result.Remove(result.Length - 1, 1);
                result.Append(renderInfo.GetText() + "\"");
            }
            else
            {
                result.Append("\"" + renderInfo.GetText() + "\"");
            }

            lastStart = start;
            lastEnd = end;
        }

        public void RenderImage(ImageRenderInfo renderInfo)
        {
        }
    }
}

