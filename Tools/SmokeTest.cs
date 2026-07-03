using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AvaloniaApp.Models;
using AvaloniaApp.Services;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

namespace AvaloniaApp.Tools;

/// <summary>
/// Дымовой тест Phase 3: справочники, шаблоны, рендеринг и генерация DOCX.
/// </summary>
public static class SmokeTest
{
    public static void Run()
    {
        var outputDir = AppDomain.CurrentDomain.BaseDirectory;

        Console.WriteLine("Seeding reference data...");
        ReferenceDataService.StaticSeedIfEmptyAsync().GetAwaiter().GetResult();
        Console.WriteLine($"SeedStatusMessage: {ReferenceDataService.StaticSeedStatusMessage}");

        var docTypes = ReferenceDataService.StaticGetDocTypesAsync().GetAwaiter().GetResult();
        Console.WriteLine($"doc_type_registry rows: {docTypes.Count}");
        foreach (var dt in docTypes.Take(3))
        {
            Console.WriteLine($"  {dt.Id}: {dt.Code} - {dt.LabelRu}");
        }

        var departments = ReferenceDataService.StaticGetDepartmentsAsync().GetAwaiter().GetResult();
        Console.WriteLine($"org_structure rows: {departments.Count}");
        foreach (var dep in departments.Take(3))
        {
            Console.WriteLine($"  {dep.Code}: {dep.NameRu} (root={dep.IsRoot}, parent={dep.ParentCode ?? "null"})");
        }

        var templates = TemplateLoader.LoadFromDirectory(AppSettings.FactoryRoot);

        var summaryLines = new List<string>
        {
            $"SeedStatusMessage: {ReferenceDataService.StaticSeedStatusMessage}",
            $"doc_type_registry rows: {docTypes.Count}",
            $"org_structure rows: {departments.Count}",
            $"Loaded {templates.Count} templates.",
            $"{"DocType",-8} {"Level",-5} {"Shield",-6} {"Sections",-8}",
        };

        foreach (var t in templates)
        {
            var line = $"{t.DocTypeCode,-8} {t.HmsLevel,-5} {t.HasShield,-6} {t.Sections.Count,-8}";
            summaryLines.Add(line);
            Console.WriteLine(line);
        }

        var pm = templates.FirstOrDefault(t => t.DocTypeCode == "PM");
        string preview = "(PM template not found)";
        if (pm != null)
        {
            var meta = CreateSampleMetadata(pm);
            var rendered = TemplateEngine.Render(meta, pm);
            preview = rendered.Length > 500 ? rendered.Substring(0, 500) : rendered;

            Console.WriteLine();
            Console.WriteLine("--- Rendered PM preview (first 500 chars) ---");
            Console.WriteLine(preview);

            File.WriteAllText(Path.Combine(outputDir, "smoke_preview.md"), rendered);
        }

        // --- Phase 3: DOCX generation for every template ---
        Console.WriteLine();
        Console.WriteLine("--- DOCX smoke generation ---");
        var docxOutputDir = @"D:\Avalonia";
        Directory.CreateDirectory(docxOutputDir);

        var docxResults = new List<(string Code, long Size, int Errors)>();

        foreach (var def in templates)
        {
            var meta = CreateSampleMetadata(def);
            var rendered = TemplateEngine.Render(meta, def);
            var path = Path.Combine(docxOutputDir, $"smoke_{def.DocTypeCode}.docx");

            try
            {
                DocxGenerator.Generate(rendered, path);
                var errors = ValidateDocx(path);
                var size = new FileInfo(path).Length;
                docxResults.Add((def.DocTypeCode, size, errors));
                Console.WriteLine($"{def.DocTypeCode,-8} {size,10} bytes  {errors,5} errors  -> {path}");
            }
            catch (Exception ex)
            {
                docxResults.Add((def.DocTypeCode, 0, -1));
                Console.WriteLine($"{def.DocTypeCode,-8} FAILED: {ex.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"{"DocType",-8} | {"size_bytes",12} | {"openxml_errors",14}");
        Console.WriteLine(new string('-', 40));
        foreach (var r in docxResults)
        {
            Console.WriteLine($"{r.Code,-8} | {r.Size,12} | {r.Errors,14}");
        }

        summaryLines.Add(string.Empty);
        summaryLines.Add("DOCX smoke results:");
        summaryLines.Add($"{"DocType",-8} {"size_bytes",12} {"openxml_errors",14}");
        foreach (var r in docxResults)
        {
            summaryLines.Add($"{r.Code,-8} {r.Size,12} {r.Errors,14}");
        }

        File.WriteAllLines(Path.Combine(outputDir, "smoke_summary.txt"), summaryLines);
        File.WriteAllText(Path.Combine(outputDir, "smoke_preview.txt"), preview);

        VerifyDocumentRepositoryAsync().GetAwaiter().GetResult();

        VerifyDocumentBodyMarkdownRoundTrip();

        RunE2EAsync().GetAwaiter().GetResult();
    }

    private static void VerifyDocumentBodyMarkdownRoundTrip()
    {
        Console.WriteLine();
        Console.WriteLine("--- Phase 4 Step 3 verification ---");

        var sampleBody = new DocumentBody
        {
            Purpose = "Цель: обеспечить безопасность работ.",
            Scope = "Применяется ко всем подразделениям.",
            Terms = new System.Collections.ObjectModel.ObservableCollection<TermPair>
            {
                new() { Position = 0, Term = "SOP", Definition = "Standard Operating Procedure" },
                new() { Position = 1, Term = "JSA", Definition = "Job Safety Analysis" },
            },
            Sections = new System.Collections.ObjectModel.ObservableCollection<BodySection>
            {
                new() { Position = 0, Title = "Входы и выходы", Body = "Вход: заявка.\nВыход: акт." },
                new() { Position = 1, Title = "Визуальный алгоритм", Body = "```mermaid\ngraph TD\n    A --> B\n```" },
            },
            PrimaryTraining = "Вводный инструктаж, 5 дней.",
            RecurrentTraining = "Ежегодная проверка знаний.",
            ParentDocument = "Кодекс",
            RelatedDocuments = new System.Collections.ObjectModel.ObservableCollection<StringItem>
            {
                new() { Value = "Связанный документ 1" },
                new() { Value = "Связанный документ 2" },
            },
            ChangeLog = new System.Collections.ObjectModel.ObservableCollection<ChangeLogRow>
            {
                new() { Position = 0, Date = "25.06.2026", Revision = "1", Author = "Иванов", Reviewer = "Петров", Description = "Первая редакция" },
            },
        };

        var sampleMeta = new DocumentMetadata { Author = "Иванов" };
        var md = DocumentBodyToMarkdown.Convert(sampleMeta, sampleBody);
        Console.WriteLine($"Generated markdown ({md.Length} chars):");
        Console.WriteLine(md);
        Console.WriteLine();

        // Round-trip test
        var parsed = DocumentBodyParser.Parse(md);
        Console.WriteLine($"Parsed back: Purpose='{parsed.Purpose.Substring(0, Math.Min(30, parsed.Purpose.Length))}...'");
        Console.WriteLine($"  Terms count: {parsed.Terms.Count}");
        Console.WriteLine($"  Sections count: {parsed.Sections.Count}");
        Console.WriteLine($"  Primary training length: {parsed.PrimaryTraining.Length}");
        Console.WriteLine($"  Related count: {parsed.RelatedDocuments.Count}");
        Console.WriteLine($"  ChangeLog count: {parsed.ChangeLog.Count}");
    }

    private static async Task VerifyDocumentRepositoryAsync()
    {
        Console.WriteLine();
        Console.WriteLine("--- Phase 4 Step 1 verification ---");
        DocumentSchema.EnsureCreated();

        var testMeta = new DocumentMetadata
        {
            DocId = "TEST-P4S1-001",
            DocType = "PM",
            Title = "Test Phase 4 Step 1",
            DepartmentName = "QHSE",
            Author = "Test Author",
            Owner = "Test Owner",
            Approver = "Test Approver",
            ApproverRole = "Руководитель подразделения",
            Revision = "1",
            Status = "проект",
            EffectiveDate = "2026-06-25",
        };

        var testBody = new DocumentBody
        {
            Purpose = "Test purpose",
            Scope = "Test scope",
            Terms = new System.Collections.ObjectModel.ObservableCollection<TermPair>
            {
                new() { Position = 0, Term = "SOP", Definition = "Standard Operating Procedure" },
                new() { Position = 1, Term = "JSA", Definition = "Job Safety Analysis" },
            },
            Sections = new System.Collections.ObjectModel.ObservableCollection<BodySection>
            {
                new() { Position = 0, Title = "Входы", Body = "Input content" },
                new() { Position = 1, Title = "Выходы", Body = "Output content" },
            },
            PrimaryTraining = "Initial training",
            RecurrentTraining = "Yearly training",
            ParentDocument = "Parent ref",
            RelatedDocuments = new System.Collections.ObjectModel.ObservableCollection<StringItem>
            {
                new() { Value = "Related 1" },
                new() { Value = "Related 2" },
            },
            ChangeLog = new System.Collections.ObjectModel.ObservableCollection<ChangeLogRow>
            {
                new() { Position = 0, Date = "25.06.2026", Revision = "1", Author = "A", Reviewer = "B", Description = "Initial" },
            },
        };

        var id = await DocumentRepository.SaveAsync(testMeta, testBody).ConfigureAwait(false);
        Console.WriteLine($"Saved with id={id}");

        var loaded = await DocumentRepository.LoadByDocIdAsync("TEST-P4S1-001").ConfigureAwait(false);
        if (loaded.HasValue)
        {
            Console.WriteLine($"Loaded: {loaded.Value.Metadata.DocId} '{loaded.Value.Metadata.Title}'");
            Console.WriteLine($"  Terms: {loaded.Value.Body.Terms.Count}");
            Console.WriteLine($"  Sections: {loaded.Value.Body.Sections.Count}");
            Console.WriteLine($"  Related: {loaded.Value.Body.RelatedDocuments.Count}");
            Console.WriteLine($"  ChangeLog: {loaded.Value.Body.ChangeLog.Count}");
        }

        await DocumentRepository.DeleteAsync(id).ConfigureAwait(false);
        Console.WriteLine($"Deleted id={id}");

        var gone = await DocumentRepository.LoadByDocIdAsync("TEST-P4S1-001").ConfigureAwait(false);
        Console.WriteLine($"After delete: {(gone.HasValue ? "STILL EXISTS (bug)" : "gone (OK)")}");
    }

    private static async Task RunE2EAsync()
    {
        var outputLines = new List<string>();
        var pass = 0;
        var fail = 0;

        void Log(string message)
        {
            Console.WriteLine(message);
            outputLines.Add(message);
        }

        void MarkPass()
        {
            pass++;
        }

        void MarkFail()
        {
            fail++;
        }

        try
        {
            Log(string.Empty);
            Log("=== Phase 4 E2E Test ===");

            // 1. Schema
            DocumentSchema.EnsureCreated();

            // 2. Create test document (PM) with all fields populated
            var metadata = new DocumentMetadata
            {
                DocId = "E2E-PM-001",
                DocType = "PM",
                Title = "Карта процесса E2E тест",
                DepartmentName = "QHSE",
                Author = "Иванов И.И.",
                Owner = "QHSE",
                Approver = "Петров П.П.",
                ApproverRole = "Руководитель подразделения",
                Revision = "1",
                Status = "проект",
                EffectiveDate = "2026-06-25",
            };

            var body = new DocumentBody
            {
                Purpose = "Обеспечить безопасность при проведении работ",
                Scope = "Все подразделения ООО «БурСервис»",
                Terms = new System.Collections.ObjectModel.ObservableCollection<TermPair>
                {
                    new() { Position = 0, Term = "SOP", Definition = "Стандартная операционная процедура" },
                    new() { Position = 1, Term = "JSA", Definition = "Анализ безопасности работ" },
                    new() { Position = 2, Term = "MOC", Definition = "Управление изменениями" },
                },
                Sections = new System.Collections.ObjectModel.ObservableCollection<BodySection>
                {
                    new() { Position = 0, Title = "Входы и выходы", Body = "**Владелец процесса:** Иванов И.И.\n\n**Вход:** Заявка от заказчика.\n\n**Выход:** Акт выполненных работ." },
                    new() { Position = 1, Title = "Визуальный алгоритм", Body = "```mermaid\ngraph TD\n    Start([Старт]) --> Check{Данные полные?}\n    Check -->|Нет| Reject([Отказ])\n    Check -->|Да| Work[Выполнить работу]\n    Work --> QC{QC пройден?}\n    QC -->|Нет| Work\n    QC -->|Да| End([Финиш])\n```" },
                    new() { Position = 2, Title = "Матрица ответственности (RACI)", Body = "| Шаг | Описание | Инженер | Менеджер | Заказчик |\n|---|---|---|---|---|\n| 1 | Получение заявки | R | A | I |\n| 2 | Выполнение | R | A | I |\n| 3 | Контроль качества | A | C | I |" },
                    new() { Position = 3, Title = "KPI процесса", Body = "| Метрика | Цель | Период |\n|---|---|---|\n| Срок выполнения | ≤ 5 дней | Месяц |\n| Дефекты | ≤ 1% | Месяц |" },
                },
                PrimaryTraining = "Вводный инструктаж, 3 дня",
                RecurrentTraining = "Ежегодная проверка знаний",
                ParentDocument = "Кодекс деловой этики",
                RelatedDocuments = new System.Collections.ObjectModel.ObservableCollection<StringItem>
                {
                    new() { Value = "BP-QHSE-001" },
                    new() { Value = "ST-QHSE-005" },
                },
                ChangeLog = new System.Collections.ObjectModel.ObservableCollection<ChangeLogRow>
                {
                    new() { Position = 0, Date = "25.06.2026", Revision = "1", Author = "Иванов", Reviewer = "Петров", Description = "Первая редакция" },
                },
            };

            // 3. Save to DB
            var id = await DocumentRepository.SaveAsync(metadata, body).ConfigureAwait(false);
            Log($"[OK] Saved document id={id}");
            MarkPass();

            // 4. Load back
            var loaded = await DocumentRepository.LoadByDocIdAsync("E2E-PM-001").ConfigureAwait(false);
            if (!loaded.HasValue)
            {
                Log("[FAIL] LoadByDocIdAsync returned null");
                MarkFail();
                return;
            }

            var (loadedMeta, loadedBody) = loaded.Value;
            Log($"[OK] Loaded: doc_id={loadedMeta.DocId}, sections={loadedBody.Sections.Count}, terms={loadedBody.Terms.Count}");
            MarkPass();

            // 5. Convert to markdown
            var mdBody = DocumentBodyToMarkdown.Convert(loadedBody);
            var fullMd = MarkdownService.Compose(loadedMeta, mdBody);
            Log($"[OK] Markdown generated ({fullMd.Length} chars)");
            MarkPass();

            // 6. Generate DOCX
            var docxPath = @"D:\Avalonia\smoke_E2E_PM.docx";
            DocxGenerator.Generate(fullMd, docxPath);
            var size = new FileInfo(docxPath).Length;
            Log($"[OK] DOCX generated ({size} bytes): {docxPath}");
            MarkPass();

            // 7. Validate DOCX
            using (var wordDoc = WordprocessingDocument.Open(docxPath, false))
            {
                var validator = new OpenXmlValidator();
                var errors = validator.Validate(wordDoc).ToList();
                if (errors.Count == 0)
                {
                    Log("[OK] OpenXML validation: 0 errors");
                    MarkPass();
                }
                else
                {
                    Log($"[FAIL] OpenXML validation: {errors.Count} errors");
                    foreach (var e in errors.Take(5))
                    {
                        Log($"  - {e.Description}");
                    }

                    MarkFail();
                }
            }

            // 8. Update + re-save
            loadedBody.Sections.Add(new BodySection { Position = loadedBody.Sections.Count, Title = "Дополнительная секция", Body = "Содержимое" });
            loadedMeta.Revision = "2";
            await DocumentRepository.SaveAsync(loadedMeta, loadedBody).ConfigureAwait(false);
            Log("[OK] Updated document");
            MarkPass();

            // 9. Re-load
            var reloaded = await DocumentRepository.LoadByDocIdAsync("E2E-PM-001").ConfigureAwait(false);
            if (!reloaded.HasValue)
            {
                Log("[FAIL] Re-load returned null");
                MarkFail();
            }
            else
            {
                Log($"[OK] Re-loaded: revision={reloaded.Value.Metadata.Revision}, sections={reloaded.Value.Body.Sections.Count}");
                MarkPass();
            }

            // 10. Delete
            await DocumentRepository.DeleteAsync(id).ConfigureAwait(false);
            Log("[OK] Deleted document");
            MarkPass();

            // 11. Verify deletion
            var gone = await DocumentRepository.LoadByDocIdAsync("E2E-PM-001").ConfigureAwait(false);
            if (gone.HasValue)
            {
                Log($"[FAIL] After delete: still exists");
                MarkFail();
            }
            else
            {
                Log("[OK] After delete: gone");
                MarkPass();
            }

            // 12. ChangeLogSheet v2: 5 columns + placeholder hint
            VerifyChangeLogSheetV2(Log, MarkPass, MarkFail);
        }
        catch (Exception ex)
        {
            Log($"[FAIL] Unexpected exception: {ex}");
            MarkFail();
        }
        finally
        {
            Log($"SUMMARY: pass={pass} fail={fail}");
            Directory.CreateDirectory(@"D:\Avalonia");

            // Если stdout перенаправлен (Tee-Object, > file и т.п.), файл уже заполняется потоком вывода.
            // Иначе пишем результат E2E-теста явно в файл.
            if (!Console.IsOutputRedirected)
            {
                try
                {
                    File.WriteAllLines(@"D:\Avalonia\smoke_results.txt", outputLines);
                }
                catch (IOException ex)
                {
                    Log($"[INFO] Could not write smoke_results.txt directly: {ex.Message}");
                }
            }
        }
    }

    private static DocumentMetadata CreateSampleMetadata(TemplateDefinition def)
    {
        var meta = new DocumentMetadata
        {
            DocId = $"DOC-{def.DocTypeCode}-001",
            DocType = def.DocTypeCode,
            Title = $"Тестовый {def.DocTypeCode}-документ",
            DepartmentName = "QHSE",
            Author = "Иванов И.И.",
            Owner = "Иванов И.И.",
            Approver = "Петров П.П.",
            ApproverRole = def.DefaultApproverRole,
            Revision = "1",
            Status = "проект",
            EffectiveDate = DateTime.Today.ToString("yyyy-MM-dd"),
        };

        return meta;
    }

    private static int ValidateDocx(string path)
    {
        using var doc = WordprocessingDocument.Open(path, false);
        var validator = new OpenXmlValidator();
        return validator.Validate(doc).Count();
    }

    private static void VerifyChangeLogSheetV2(Action<string> log, Action markPass, Action markFail)
    {
        log(string.Empty);
        log("--- Phase 5 ChangeLogSheet v2 verification ---");

        // 1) Серый шаблон-подсказка определён и не содержит markdown-таблицы.
        var hint = Views.Fields.ChangeLogResources.DescriptionHint;
        if (hint.Contains("[Раздел/пункт]:") && !hint.Contains("|"))
        {
            log($"[OK] Description hint defined ({hint.Length} chars)");
            markPass();
        }
        else
        {
            log($"[FAIL] Description hint malformed: {hint}");
            markFail();
            return;
        }

        // 2) Создаём новую строку с авто-предзаполнением Date/Revision/Author.
        var rows = new ObservableCollection<ChangeLogRow>();
        var newRow = DocumentBodyFactory.CreatePrefilledChangeLogRow(null, rows);
        if (newRow.Date == DateTime.Today.ToString("dd.MM.yyyy")
            && newRow.Revision == "1"
            && string.IsNullOrEmpty(newRow.Description))
        {
            log($"[OK] New row prefilled: Date='{newRow.Date}' Rev='{newRow.Revision}' Desc=empty");
            markPass();
        }
        else
        {
            log($"[FAIL] New row prefilled incorrectly: Date='{newRow.Date}' Rev='{newRow.Revision}' Desc='{newRow.Description}'");
            markFail();
            return;
        }

        // 3) Документ с 5-колоночной строкой (автор — из метаданных, не из строки).
        var body = new DocumentBody
        {
            Purpose = "Test ChangeLog",
            Scope = "Test",
            ChangeLog = new System.Collections.ObjectModel.ObservableCollection<ChangeLogRow>
            {
                new()
                {
                    Position = 0,
                    Date = "25.06.2026",
                    Revision = "1",
                    Reviewer = "Петров П.П.",
                    Description = "Уточнена процедура контроля качества",
                },
            },
        };

        var md = DocumentBodyToMarkdown.Convert(body);
        if (md.Contains("| Дата ред. |")
            && md.Contains("| Номер ред. |")
            && md.Contains("| Разработал |")
            && md.Contains("| Проверил |")
            && md.Contains("| Краткое описание изменений |")
            && md.Contains("Уточнена процедура контроля качества"))
        {
            log("[OK] 5-column changelog table rendered in markdown");
            markPass();
        }
        else
        {
            log("[FAIL] 5-column changelog table NOT in markdown");
            markFail();
        }
    }
}
