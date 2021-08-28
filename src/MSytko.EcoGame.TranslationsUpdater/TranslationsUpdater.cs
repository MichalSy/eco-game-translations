using CsvHelper;
using CsvHelper.Configuration;
using Eco.Shared.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSytko.EcoGame.TranslationsUpdater
{
    public class TranslationsUpdater
    {
        private string _sourceFolder = string.Empty;
        private string _targetFolder = string.Empty;
        private Dictionary<SupportedLanguage, Dictionary<string, string>> _sourceTranslations = new();

        public void Run()
        {
            _sourceFolder = Directory.GetCurrentDirectory();
            _targetFolder = Path.Combine(_sourceFolder, "translations", "core-game");


            LoadSourceTranslations();

            foreach (var sourceLanguage in _sourceTranslations.Keys)
            {
                UpdateTargetTranslations(sourceLanguage);
            }
        }

        private void LoadSourceTranslations()
        {
            var allLanguages = Enum.GetValues<SupportedLanguage>().Where(e => !e.Equals(SupportedLanguage.English));
            foreach (SupportedLanguage language in allLanguages)
            {
                _sourceTranslations.Add(language, new Dictionary<string, string>());
            }

            var allFoundFiles = Directory.GetFiles(_sourceFolder, "*.csv", SearchOption.TopDirectoryOnly);
            foreach (var foundFile in allFoundFiles)
            {
                using var reader = new StreamReader(foundFile);
                using CsvReader csvParser = new(reader, CultureInfo.InvariantCulture);

                csvParser.Read();
                csvParser.ReadHeader();
                while (csvParser.Read())
                {
                    var english = csvParser.GetField((int)SupportedLanguage.English);
                    foreach (SupportedLanguage language in allLanguages)
                    {
                        var translation = csvParser.GetField((int)language);
                        _sourceTranslations[language].Add(english, translation);
                    }
                }
            }
        }

        private void UpdateTargetTranslations(SupportedLanguage language)
        {
            var targetFile = Path.Combine(_targetFolder, $"{language.ToString().ToLower()}.csv");

            Dictionary<string, string> targetTranslations = new();
            if (File.Exists(targetFile))
            {
                using var reader = new StreamReader(targetFile);
                using CsvReader csvParser = new(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {

                });


                csvParser.Read();
                csvParser.ReadHeader();
                while (csvParser.Read())
                {
                    targetTranslations.Add(csvParser.GetField(0), csvParser.GetField(1));
                }
            }

            Dictionary<string, string> missingTranslations = new();
            if (_sourceTranslations.TryGetValue(language, out var sourceTrans))
            {
                missingTranslations = sourceTrans.Keys.Except(targetTranslations.Keys).ToDictionary(k => k, k => sourceTrans[k]);
            }

            if (missingTranslations.Any())
            {
                if (!File.Exists(targetFile))
                {
                    File.WriteAllText(targetFile, "key,translation,comment\n");
                }

                bool hasNewLineAtEnd = false;
                using (FileStream fs = new(targetFile, FileMode.Open))
                {
                    fs.Position = fs.Seek(-1, SeekOrigin.End);
                    hasNewLineAtEnd = (fs.ReadByte() == '\n');
                }
                if (!hasNewLineAtEnd)
                {
                    File.AppendAllText(targetFile, "\n");
                }

                foreach (var item in missingTranslations)
                {
                    File.AppendAllText(targetFile, $"{Localizer.Convert(item.Key, true)},{Localizer.Convert(item.Value, true)}\n");
                }
            }
        }
    }
}
