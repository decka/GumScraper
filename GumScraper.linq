<Query Kind="Program">
  <Reference>E:\Programming\LINQPad4\HtmlAgilityPack.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Globalization.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.IO.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.XML.dll</Reference>
  <Namespace>HtmlAgilityPack</Namespace>
  <Namespace>System.Globalization</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Xml.Serialization</Namespace>
  <Namespace>System.Xml.Xsl</Namespace>
</Query>

void Main()
{
		//string SourceURL = "F:\\Temporary\\GumScraper\\test.htm";
		string SourceURL = "http://www.gumtree.com.au/s-search-results.html?keywords={0}&categoryId=18560&locationStr=Australia&locationId=0&sortByName=date&searchView=LIST&topAd=false&gpTopAd=false&hpgAd=false&highlightOnly=false&pageNum=1&action=default&urgentOnly=false&pageSize=1000";
	
		string ResultsFile = "list.xml";
				
		string CountXpath = "//h1[@class='c-brdcmp-smmry']";
		string ListXpath = "//ul[@id='srchrslt-adtable']/li";
		string TitleXpath = "div[@class='rs-ad-field rs-ad-detail margin-bottom10']/h3/a";
		string Description1Xpath = "div[@class='rs-ad-field rs-ad-detail margin-bottom10']/p/text()";
		string Description2Xpath = "div[@class='rs-ad-field rs-ad-detail margin-bottom10']/p/span";
		string PriceXpath = "div[@class='rs-ad-field rs-ad-price']/div[@class='h-elips ']";
		string Location1Xpath = "div[@class='rs-ad-field rs-ad-location']/h3";
		string Location2Xpath = "div[@class='rs-ad-field rs-ad-location']/text()[2]";
		
		Uri baseUri = new Uri("http://www.gumtree.com.au");
		string SearchTerm = "specialized+tricross";
		SearchBatch newBatch = new SearchBatch { SearchTimestamp = DateTime.Now, SearchTerm = SearchTerm } ;
		SearchBatch oldBatch;
		
		CookieAwareWebClient client = new CookieAwareWebClient();
		string html = client.DownloadString(String.Format(SourceURL, newBatch.SearchTerm));
		HtmlDocument doc = new HtmlDocument();
		doc.LoadHtml(html);
		
		HtmlNode CountNode = doc.DocumentNode.SelectSingleNode(CountXpath);
		string Count = CountNode.InnerText;
		
		HtmlNodeCollection ListNodes = doc.DocumentNode.SelectNodes(ListXpath);

		Console.WriteLine("{0} items found.", ListNodes.Count);
		
		foreach (HtmlNode ListNode in ListNodes)
		{
			HtmlNode TitleNode = ListNode.SelectSingleNode(TitleXpath);
			HtmlNode Description1Node = ListNode.SelectSingleNode(Description1Xpath);
			HtmlNode Description2Node = ListNode.SelectSingleNode(Description2Xpath);
			HtmlNode PriceNode = ListNode.SelectSingleNode(PriceXpath);
			HtmlNode Location1Node = ListNode.SelectSingleNode(Location1Xpath);
			HtmlNode Location2Node = ListNode.SelectSingleNode(Location2Xpath);
			
			string Title = TitleNode.InnerText;
			Uri Link = new Uri(baseUri, TitleNode.Attributes["href"].Value);
			string Description = Description1Node.InnerText.Trim().TrimEnd('.') + (Description2Node != null ? Description2Node.InnerText.Trim() : "");
			string PriceString = PriceNode.InnerText.Trim('\r','\n');
			string Location = Location1Node.InnerText + (Location2Node != null ? ", " + Location2Node.InnerText.Trim() : "");
			decimal Price;
			decimal.TryParse(PriceString, NumberStyles.Currency, CultureInfo.CurrentCulture.NumberFormat, out Price);
			
			newBatch.SearchResults.Add(new SearchResult { Title = Title, Link = Link, Price = Price, Description = Description, Location = Location, TimestampFirstFound = DateTime.Now});
		}
	
		try
    	{	
			using (FileStream fs = new FileStream(ResultsFile, FileMode.Open))
			{
				XmlSerializer deser = new XmlSerializer(typeof(SearchBatch));
				oldBatch = (SearchBatch)deser.Deserialize(fs);
				fs.Flush();
				fs.Close();
			}
			
			foreach (SearchResult newResult in newBatch.SearchResults)
			{
				var match = oldBatch.SearchResults.Find(x => x.Link == newResult.Link);
				if (match != null)
					newResult.TimestampFirstFound = match.TimestampFirstFound;
			}
		}
		catch (FileNotFoundException ex)
		{
			Console.WriteLine("Cannot Find Input File: {0}", ex);
		}
		
		using (FileStream fs = new FileStream(ResultsFile, FileMode.Create))
		{
			XmlSerializer ser = new XmlSerializer(typeof(SearchBatch));
			ser.Serialize(fs, newBatch);
			fs.Flush();
			fs.Close();
		}
}

public class SearchResult : ISearchResult
{
	public string Title { get; set;}
	[XmlIgnore]
	public Uri Link { get; set; }
	
	[XmlElement("Link")]
	public string MyURIAsString
	{
		get { return Link != null ? Link.AbsoluteUri : null; }
		set { Link = value != null ? new Uri(value) : null; }
	}
	public decimal Price  { get; set;}
	public string Description { get; set; } 
	public string Location { get; set; }
	public DateTime TimestampFirstFound { get; set; }
}

public interface ISearchResult
{
	string Title { get; set; }
	Uri Link  { get; set; }
	decimal Price  { get; set; }
	string Description { get; set; }
	string Location { get; set; }
	DateTime TimestampFirstFound { get; set; }
}

[Serializable()]
public class SearchBatch
{
	public SearchBatch()
	{
		SearchResults = new List<SearchResult>();
	}
	public DateTime SearchTimestamp { get; set; }
	public string SearchTerm { get; set; }
	public List<SearchResult> SearchResults { get; set; }
}

public class CookieAwareWebClient : WebClient
{
	private CookieContainer m_container = new CookieContainer();
	protected override WebRequest GetWebRequest(Uri address)
	{
		WebRequest request = base.GetWebRequest(address);
		if (request is HttpWebRequest)
		{
			(request as HttpWebRequest).CookieContainer = m_container;
		}
		return request;
	}
}

