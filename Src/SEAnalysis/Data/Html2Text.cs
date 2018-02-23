namespace SEA.Data
{
    using AngleSharp;
    using AngleSharp.Dom.Html;
    using AngleSharp.Extensions;
    using AngleSharp.Parser.Html;

    class Html2Text
    {
        private HtmlParser _parser;

        public Html2Text()
        {
            _parser = new HtmlParser();
        }

        public string ToText(string HtmlMarkup)
        {
            string text = "";

            using (IHtmlDocument document = _parser.Parse(HtmlMarkup))
            {
                text = document.Body.Text();
            }

            return text.Trim();
        }
    }
}