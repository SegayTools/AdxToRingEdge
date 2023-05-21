using SimpleSvg2LineSegementInterpolater;
using System.Drawing.Imaging;
using System.Text.Json;
using TouchPanelJsonGenerator;

var svgFilePath = args.FirstOrDefault();

if (!File.Exists(svgFilePath))
{
    Console.WriteLine(".svg file not found.");
    return;
}

var opt = new InterpolaterOption()
{

};

var results = (await Interpolater.GenerateInterpolatedLineSegmentAsync(File.ReadAllText(svgFilePath), opt))
    .Select(x =>
    {
        x.Points = x.Points.DistinctContinuousBy(x => x).ToList();
        return x;
    })
    .Where(x => x.Points.Count > 0)
    .Where(x => !string.IsNullOrWhiteSpace(x.Name))
    .OrderBy(x => x.Name)
    .ToArray();

foreach (var result in results)
    LineSegmentSimplifier.SimplifySameGradientPoints(result);

var previewOutputPath = Path.ChangeExtension(svgFilePath, ".TouchPanelData.Preview.png");
Drawing.DrawToImage(results).Save(previewOutputPath, ImageFormat.Png);

var json = JsonSerializer.Serialize(results.ToDictionary(x => x.Name, x => x.Points.Select(x => new
{
    x.X,
    x.Y
})), new JsonSerializerOptions()
{
    WriteIndented = true
});

var outputPath = Path.ChangeExtension(svgFilePath, ".TouchPanelData.json");
File.WriteAllText(outputPath, json);

Console.WriteLine($"Gooood, json generated to: {outputPath} , preview generated to {previewOutputPath}");