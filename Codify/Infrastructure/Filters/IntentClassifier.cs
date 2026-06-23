using Microsoft.ML;
using Microsoft.ML.Data;
using System.Collections.Generic;

namespace Codify.Infrastructure.Filters
{
    // Model input structure
    public class ModelInput
    {
        public string Text { get; set; }
        public bool IsProgramming { get; set; }
    }

    // Model output structure
    public class ModelOutput
    {
        [ColumnName("PredictedLabel")]
        public bool Prediction { get; set; }
        public float Score { get; set; }
    }

    public class IntentClassifier : IIntentClassifier
    {
        private readonly MLContext _mlContext;
        private ITransformer _model;
        private PredictionEngine<ModelInput, ModelOutput> _predictionEngine;

        public IntentClassifier()
        {
            _mlContext = new MLContext();
            TrainModel(); // Train a small model in-memory
        }

        private void TrainModel()
        {
            // Simple dataset for training (You can expand this)
            var data = new List<ModelInput>
            {
                new ModelInput { Text = "how to create a web api in core", IsProgramming = true },
                new ModelInput { Text = "ارور null reference در سی شارپ", IsProgramming = true },
                new ModelInput { Text = "git push origin main error", IsProgramming = true },
                new ModelInput { Text = "ساخت دیتابیس در sql server", IsProgramming = true },
                new ModelInput { Text = "docker compose file example", IsProgramming = true },
                new ModelInput { Text = "چجوری یک باتن رو در css وسط چین کنم", IsProgramming = true },
                new ModelInput { Text = "JSON serialization in Newtonsoft", IsProgramming = true },
                new ModelInput { Text = "الگوریتم مرتب سازی سریع", IsProgramming = true },
                new ModelInput { Text = "How to use useEffect in React", IsProgramming = true },
                new ModelInput { Text = "نحوه تعریف متغیر در پایتون", IsProgramming = true },

                new ModelInput { Text = "اخبار جنگ ایران و اسرائیل", IsProgramming = false },
                new ModelInput { Text = "قیمت دلار و سکه امروز", IsProgramming = false },
                new ModelInput { Text = "طرز تهیه قورمه سبزی خوشمزه", IsProgramming = false },
                new ModelInput { Text = "Politics in middle east", IsProgramming = false },
                new ModelInput { Text = "بهترین گوشی های سال ۲۰۲۴", IsProgramming = false },
                new ModelInput { Text = "نتایج زنده فوتبال اروپا", IsProgramming = false },
                new ModelInput { Text = "Who is the president of USA", IsProgramming = false },
                new ModelInput { Text = "وضعیت آب و هوای تهران", IsProgramming = false },
                new ModelInput { Text = "دانلود آهنگ جدید شادمهر", IsProgramming = false },
                new ModelInput { Text = "رژیم غذایی برای لاغری سریع", IsProgramming = false },
                
                new ModelInput { Text = "اخبار برنامه هسته ای ایران", IsProgramming = false }, // کلمه برنامه دارد
                new ModelInput { Text = "کد پستی منزل من چند است", IsProgramming = false }, // کلمه کد دارد
                new ModelInput { Text = "سریال گاندو قسمت آخر", IsProgramming = false }
            };

            var trainingData = _mlContext.Data.LoadFromEnumerable(data);

            // Pipeline: Text Featurization -> Logistic Regression
            var pipeline = _mlContext.Transforms.Text.FeaturizeText("Features", nameof(ModelInput.Text))
                .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: nameof(ModelInput.IsProgramming), featureColumnName: "Features"));

            _model = pipeline.Fit(trainingData);
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(_model);
        }

        public bool IsTechnical(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            var result = _predictionEngine.Predict(new ModelInput { Text = text });
            return result.Prediction;
        }
    }
}
