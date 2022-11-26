<Query Kind="Program">
  <Namespace>LINQPad.Controls</Namespace>
</Query>

void Main()
{
	Css.Init();
	var list = Enumerable.Range(0, 15).Select(e => $"english {e}");

	var dlg = DlgUtils.Make();
	Util.HorizontalRun(true,
		new Button("Open", _ => dlg.Open()),
		new Button("Close", _ => dlg.Close()),
		new Button("Add", _ => dlg.DC.AppendContent(list))
	).Dump();
	
	list.Dump();
	
	dlg.UI.Dump();
}

interface IDlg
{
	Control UI { get; }
	DumpContainer DC { get; }
	void Open();
	void Close();
}

static class DlgUtils
{
	private class Dlg : IDlg
	{
		public Control UI { get; }
		public DumpContainer DC { get; }
		public Dlg(Control ui, DumpContainer dc)
		{
			UI = ui;
			DC = dc;
		}
		public void Open() => UI.Styles["display"] = "flex";
		public void Close() => UI.Styles["display"] = "none";
	}
	
	public static IDlg Make()
	{
		var dcMain = new DumpContainer();
		var ctrlHeader = new Div(new Span("Folder lock")) { CssClass = "modal-header" };
		var ctrlMain = new Div(dcMain) { CssClass = "modal-main" };
		var ctrlFooter = new Div(new Span("Footer")) { CssClass = "modal-footer" };
		var divInner = new Div(ctrlHeader, ctrlMain, ctrlFooter) { CssClass = "modal-inner" };
		var dlg = new Control("dialog", divInner) { CssClass = "modal" };
		return new Dlg(dlg, dcMain);
		
		/*var dlgDiv = new Div()
		{
			CssClass = "modal",
		};
		dlgDiv.HtmlElement.ID = "mymodal";
		dlgDiv.HtmlElement.InnerHtml = """
			<div class="modal-content">
				<span class="close" onClick="document.getElementById('mymodal').style.display = 'none'">&times;</span>
				<p>Some text in the Modal..</p>
			</div>
			""";
		return new Dlg(dlgDiv);*/
	}
}

static class Css
{
	public static void Init()
	{
		Util.HtmlHead.AddStyles("""
			:root {
				--modal-backcolor: #22252A;
				--modal-backcolor-main: #353A40;
			}
			.modal {
				/* Outer */
				position: absolute; inset: 0; left: 0; top: 0; margin: auto; padding: 0; border: 0;
				width: 100%; height: 100%;
				/* Inner */
				display: flex;
				justify-content: center;
				/* Pretty */
				backdrop-filter: blur(2px);
				background-color: transparent;
			}

			.modal-inner {
				/* Outer */
				margin: auto 0; padding: 10px;
				width: 100%; height: 300px;
				max-width: 400px;
				/* Inner */
				display: grid;
				grid-template-rows: auto 1fr auto;
				gap: 10px;
				/* Pretty */
				border-radius: 20px; border-top: 1px solid #333; border-left: 1px solid #333; border-right: 1px solid #222; border-bottom: 1px solid #222;
				background-color: var(--modal-backcolor);
				box-shadow: 5px 5px 15px 5px #000000A0;
				font-size: 24px;
				color: white;
			}

			.modal-header {
				font-weight: bold;
				color: #df5252;
			}

			.modal-main {
				background-color: var(--modal-backcolor-main);
				overflow-y: auto;
			}

			.modal-footer {
			}
			"""
		);
	}
}