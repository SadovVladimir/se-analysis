namespace SEA.Data
{
    using AngleSharp.Dom.Html;
    using AngleSharp.Extensions;
    using AngleSharp.Parser.Html;

    internal class HtmlDocGen
    {
        private HtmlParser _parser;

        public HtmlDocGen()
        {
            _parser = new HtmlParser();
        }

        public string GetFullHtmlDocAsString(string Markup)
        {
            string fullDocAsString = "";

            using (var doc = _parser.Parse(Markup))
            {
                fullDocAsString = doc.DocumentElement.OuterHtml;
            }

            return fullDocAsString;
        }
    }
}