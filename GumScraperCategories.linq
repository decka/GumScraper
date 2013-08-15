<Query Kind="Program">
  <Reference>E:\Programming\LINQPad4\HtmlAgilityPack.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Web.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.XML.dll</Reference>
  <Namespace>HtmlAgilityPack</Namespace>
  <Namespace>System.Web</Namespace>
  <Namespace>System.Xml.Serialization</Namespace>
</Query>

void Main()
{
	string SourceURL = "http://www.gumtree.com.au/";
	string CategoriesXpath = "//dl[@class='hp_cat_list']/dt";
	string SubcategoriesXpath = "following-sibling::dd[@class='hp_cat_p'][1]/dl[@class='hp_cat']/dd";
	string HrefXpath = "a";
	
	List<string> Exclusions = new List<string> { "Resumes", "Friends & Dating", "Freebies", @"Swap/Trade" };
	var Categories = new List<Category>();
	
	var webGet = new HtmlWeb();
	var document = webGet.Load(SourceURL);
	
	var CategoryNodes = document.DocumentNode.SelectNodes(CategoriesXpath);

	foreach (var CategoryNode in CategoryNodes)
	{
		var CategoryNodeHref = CategoryNode.SelectSingleNode(HrefXpath);
		string CategoryName = HttpUtility.HtmlDecode(CategoryNodeHref.InnerText);

		if (!Exclusions.Contains(CategoryName))
		{
			ushort CategoryID = CategoryLinkToID(CategoryNodeHref.Attributes["href"].Value);		
			var CurrentCategory = new Category { ParentCategory = null, Name = CategoryName, ID = CategoryID };
			Categories.Add(CurrentCategory);
			var SubcategoryNodes = CategoryNode.SelectNodes(SubcategoriesXpath);
			
			if (SubcategoryNodes != null)
			{
				foreach (var SubcategoryNode in SubcategoryNodes)
				{
					var SubcategoryNodeHref = SubcategoryNode.SelectSingleNode(HrefXpath);
					string SubcategoryName = HttpUtility.HtmlDecode(SubcategoryNodeHref.InnerText);		
					ushort SubcategoryID = CategoryLinkToID(SubcategoryNodeHref.Attributes["href"].Value);
					
					Categories.Add(new Category { ParentCategory = CurrentCategory, Name = SubcategoryName, ID = SubcategoryID });
				}
			}
		}
	}
	Categories.Select(x => new { x.ParentName, x.Name, x.ID }).OrderBy(x => x.Name).Dump();
}

ushort CategoryLinkToID(string FullLink)
{
	return Convert.ToUInt16(FullLink.Substring(FullLink.LastIndexOf("/c")+2));
}

class Category
{
	[XmlElement("ParentName")]
	public string ParentName
	{
		get { return ParentCategory != null ? ParentCategory.Name : "none"; }
	
	}
	[XmlIgnore]
	public Category ParentCategory { get; set; }
	public string Name { get; set; }
	public ushort ID { get; set; }
}