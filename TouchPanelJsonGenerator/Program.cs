using SimpleSvg2LineSegementInterpolater;
using System.Text.Json;

var svgFilePath = args.FirstOrDefault();

if (!File.Exists(svgFilePath))
{
    Console.WriteLine(".svg file not found.");
    return;
}

var opt = new InterpolaterOption()
{

};

var result = (await Interpolater.GenerateInterpolatedLineSegmentAsync(File.ReadAllText(svgFilePath), opt))
    .Where(x => x.Points.Count > 0)
    .Where(x => !string.IsNullOrWhiteSpace(x.Name))
    .OrderBy(x => x.Name)
    .ToArray();

//SimpleSvg2LineSegementInterpolater.Drawing.DrawToImage(result).Save("F:\\data.svg.png", ImageFormat.Png);
var json = JsonSerializer.Serialize(result.ToDictionary(x => x.Name, x => x.Points.Select(x => new
{
    x.X,
    x.Y
})), new JsonSerializerOptions()
{
    WriteIndented = true
});

var outputPath = Path.ChangeExtension(svgFilePath, ".TouchPanelData.json");
File.WriteAllText(outputPath, json);

Console.WriteLine($"Gooood, generate to: {outputPath}");