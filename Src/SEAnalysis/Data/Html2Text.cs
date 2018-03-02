namespace SEA.Data
{
    using AngleSharp.Dom.Html;
    using AngleSharp.Extensions;
    using AngleSharp.Parser.Html;

    internal class Html2Text
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