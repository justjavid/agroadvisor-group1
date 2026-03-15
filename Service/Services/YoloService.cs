using Microsoft.Extensions.Configuration;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Service.DTOs.ImageAnalysisDTOs;
using Service.Services.Interfaces;
using System.Drawing;

namespace Service.Services;

// Sadə YOLO ONNX inteqrasiyası – model faylını və preprocess/postprocess
// hissələrini öz ehtiyacına görə tənzimləyə bilərsən.
public class YoloService : IYoloService, IDisposable
{
    private readonly InferenceSession _session;
    private readonly int _inputWidth;
    private readonly int _inputHeight;
    private readonly float _scoreThreshold;
    private readonly float _iouThreshold;

    public YoloService(IConfiguration configuration)
    {
        // appsettings.json -> "Yolo:ModelPath": "Models/yolo.onnx"
        var modelPath = configuration["Yolo:ModelPath"] ?? "Models/yolo.onnx";
        if (!File.Exists(modelPath))
        {
            // Model yoxdursa, sessiya açmırıq – sadəcə boş nəticə qaytaracağıq.
            _session = null!;
            _inputWidth = 640;
            _inputHeight = 640;
            _scoreThreshold = 0.4f;
            _iouThreshold = 0.45f;
            return;
        }

        _session = new InferenceSession(modelPath);

        // YOLOv5 tipik ölçülər (istəsən appsettings-dən də oxuya bilərsən).
        _inputWidth = 640;
        _inputHeight = 640;
        _scoreThreshold = float.TryParse(configuration["Yolo:ScoreThreshold"], out var s) ? s : 0.4f;
        _iouThreshold = float.TryParse(configuration["Yolo:IouThreshold"], out var i) ? i : 0.45f;
    }

    public Task<IReadOnlyList<DiseaseRegionDto>> DetectDiseaseRegionsAsync(ImageAnalysisRequestDto request, CancellationToken cancellationToken = default)
    {
        if (_session == null)
        {
            return Task.FromResult<IReadOnlyList<DiseaseRegionDto>>(Array.Empty<DiseaseRegionDto>());
        }

        using var ms = new MemoryStream(request.ImageBytes);
        using var originalImage = Image.FromStream(ms);

        var (input, padX, padY, scale) = Preprocess(originalImage);

        var inputName = _session.InputMetadata.Keys.First();
        var container = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(inputName, input)
        };

        using var results = _session.Run(container);
        var output = results.First().AsEnumerable<float>().ToArray();

        // YOLOv5 ONNX output: [1, N, 85] -> N x 85
        // 0..3: cx, cy, w, h (input ölçüsündə); 4: objectness; 5..: class scores
        var dimensions = _session.OutputMetadata.First().Value.Dimensions;
        var numDetections = dimensions[1];
        var infoPerDet = dimensions[2]; // 85

        var boxes = new List<(RectangleF Box, float Score)>();

        for (var i = 0; i < numDetections; i++)
        {
            var offset = i * infoPerDet;
            var cx = output[offset + 0];
            var cy = output[offset + 1];
            var w = output[offset + 2];
            var h = output[offset + 3];
            var objectness = output[offset + 4];

            // Maks class score götürürük (xəstəlik kateqoriyası fərqli olacaq, amma highlight üçün kifayət edir).
            float maxClass = 0;
            for (var c = 5; c < infoPerDet; c++)
            {
                if (output[offset + c] > maxClass)
                    maxClass = output[offset + c];
            }

            var score = objectness * maxClass;
            if (score < _scoreThreshold)
                continue;

            var x = cx - w / 2;
            var y = cy - h / 2;

            boxes.Add((new RectangleF(x, y, w, h), score));
        }

        var nmsBoxes = NonMaxSuppression(boxes, _iouThreshold);

        var regions = new List<DiseaseRegionDto>();
        foreach (var (box, score) in nmsBoxes)
        {
            // Letterbox-u geri çevirmək və orijinal şəkil ölçüsünə mapping.
            var x = (box.X - padX) / scale;
            var y = (box.Y - padY) / scale;
            var w = box.Width / scale;
            var h = box.Height / scale;

            // 0-1 aralığına normallaşdır.
            var normX = Math.Clamp(x / originalImage.Width, 0, 1);
            var normY = Math.Clamp(y / originalImage.Height, 0, 1);
            var normW = Math.Clamp(w / originalImage.Width, 0, 1);
            var normH = Math.Clamp(h / originalImage.Height, 0, 1);

            regions.Add(new DiseaseRegionDto
            {
                X = (float)normX,
                Y = (float)normY,
                Width = (float)normW,
                Height = (float)normH,
                Confidence = score
            });
        }

        return Task.FromResult<IReadOnlyList<DiseaseRegionDto>>(regions);
    }

    public void Dispose()
    {
        _session?.Dispose();
    }

    private (DenseTensor<float> Tensor, float PadX, float PadY, float Scale) Preprocess(Image image)
    {
        // Letterbox resize (YOLO üslubu).
        var scale = Math.Min(_inputWidth / (float)image.Width, _inputHeight / (float)image.Height);
        var newW = (int)(image.Width * scale);
        var newH = (int)(image.Height * scale);

        var resized = new Bitmap(_inputWidth, _inputHeight);
        using (var g = Graphics.FromImage(resized))
        {
            g.Clear(Color.Black);
            var padX = (_inputWidth - newW) / 2;
            var padY = (_inputHeight - newH) / 2;
            g.DrawImage(image, padX, padY, newW, newH);
        }

        var tensor = new DenseTensor<float>(new[] { 1, 3, _inputHeight, _inputWidth });

        for (var y = 0; y < _inputHeight; y++)
        {
            for (var x = 0; x < _inputWidth; x++)
            {
                var pixel = resized.GetPixel(x, y);
                tensor[0, 0, y, x] = pixel.R / 255f;
                tensor[0, 1, y, x] = pixel.G / 255f;
                tensor[0, 2, y, x] = pixel.B / 255f;
            }
        }

        // PadX/PadY-i (resize edərkən istifadə etdiyimiz) geri qaytarmaq üçün yenidən hesablayırıq.
        var padXRet = (_inputWidth - newW) / 2f;
        var padYRet = (_inputHeight - newH) / 2f;

        return (tensor, padXRet, padYRet, scale);
    }

    private static List<(RectangleF Box, float Score)> NonMaxSuppression(
        List<(RectangleF Box, float Score)> boxes,
        float iouThreshold)
    {
        var result = new List<(RectangleF Box, float Score)>();

        foreach (var box in boxes.OrderByDescending(b => b.Score))
        {
            var shouldAdd = true;
            foreach (var kept in result)
            {
                if (IoU(box.Box, kept.Box) > iouThreshold)
                {
                    shouldAdd = false;
                    break;
                }
            }

            if (shouldAdd)
                result.Add(box);
        }

        return result;
    }

    private static float IoU(RectangleF a, RectangleF b)
    {
        var intersection = RectangleF.Intersect(a, b);
        if (intersection.IsEmpty)
            return 0;

        var interArea = intersection.Width * intersection.Height;
        var unionArea = a.Width * a.Height + b.Width * b.Height - interArea;
        if (unionArea <= 0)
            return 0;

        return (float)(interArea / unionArea);
    }
}

