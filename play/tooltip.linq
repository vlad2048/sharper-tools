<Query Kind="Program">
  <Namespace>LINQPad.Controls</Namespace>
</Query>

void Main()
{
	Init();
	
	"_\n_\n_\n_\n_\n_".Dump();
	
	var spanTxt = new Span("Hover over me");
	var spanTooltip = new Span("TooltiptextTooltiptextTooltiptext\ndslkjhfsdf\nkdhjfs\nkdh");
	var div = new Div(spanTxt, spanTooltip);
	div.Children.Add(spanTxt);
	
	div.CssClass = "tooltip";
	spanTooltip.CssClass = "tooltiptext";
	
	div.Dump();
}



void Init()
{
	Util.HtmlHead.AddStyles("""
		.tooltip {
		  position: relative;
		  display: inline-block;
		  border-bottom: 1px dotted black;
		  flex-direction: column;
		}

		.tooltip .tooltiptext {
		  //visibility: hidden;
		  width: 120px;
		  background-color: #555;
		  color: #fff;
		  //text-align: center;
		  border-radius: 6px;
		  padding: 5px 0;
		  position: absolute;
		  z-index: 1;
		  bottom: 125%;
		  left: 50%;
		  margin-left: -60px;
		  opacity: 1;
		  transition: opacity 0.3s;
		}

		.tooltip .tooltiptext::after {
		  content: "";
		  position: absolute;
		  top: 100%;
		  left: 50%;
		  margin-left: -5px;
		  border-width: 5px;
		  border-style: solid;
		  border-color: #555 transparent transparent transparent;
		}

		.tooltip:hover .tooltiptext {
		  visibility: visible;
		  opacity: 1;
		}
	""");
}