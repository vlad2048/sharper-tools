<Query Kind="Program">
  <NuGetReference>PowBasics</NuGetReference>
  <Namespace>PowBasics.ColorCode</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
  <Namespace>System.Drawing</Namespace>
</Query>

void Main()
{
	const int zoom = 3;
	const int colWidth = 16 * zoom;
	const int colHeight = 16 * zoom;
	const int imgPad = 4 * zoom;
	
	var cols = ColorUtils.MakePalette(
		16,
		234,
		sat: 0.72,
		val: 0.58
	);
	cols
		.Select(c => $"\"#{c.R:X2}{c.G:X2}{c.B:X2}\"")
		.JoinText("; ")
		.Dump();
	
	var n = cols.Length;
	
	var imgWidth = colWidth * n + imgPad * (n + 1);
	var imgHeight = colHeight + imgPad * 2;
	var bmp = new Bitmap(imgWidth, imgHeight);
	using var gfx = Graphics.FromImage(bmp);
	
	var x = imgPad;
	var y = imgPad;
	
	for (var i = 0; i < n; i++)
	{
		var col = cols[i];
		var brush = new SolidBrush(col);
		gfx.FillRectangle(brush, x, y, colWidth, colHeight);
		x += colWidth + imgPad;
	}
	
	bmp.Dump();
	
}

