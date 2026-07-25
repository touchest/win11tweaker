namespace Win11Tweaker.App.Catalog;

public sealed record TaskItemSpec(string Title, string Summary, string[] Paths);

public sealed record TaskGroupSpec(string Title, IReadOnlyList<TaskItemSpec> Items);

public static class TaskCatalog
{
    public static IReadOnlyList<TaskGroupSpec> Build() =>
    [
        new TaskGroupSpec("Фоновые задания",
        [
            new TaskItemSpec("Отключить телеметрийные задания",
                "Фоновые задания сбора диагностики и обратной связи: оценщик совместимости, программа улучшения (CEIP), отчёты об ошибках, задания Feedback и Flighting.",
                [
                    @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser",
                    @"\Microsoft\Windows\Application Experience\ProgramDataUpdater",
                    @"\Microsoft\Windows\Application Experience\PcaPatchDbTask",
                    @"\Microsoft\Windows\Application Experience\StartupAppTask",
                    @"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator",
                    @"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip",
                    @"\Microsoft\Windows\Feedback\Siuf\DmClient",
                    @"\Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload",
                    @"\Microsoft\Windows\Windows Error Reporting\QueueReporting",
                    @"\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector",
                    @"\Microsoft\Windows\CloudExperienceHost\CreateObjectTask",
                    @"\Microsoft\Windows\Flighting\FeatureConfig\ReconcileFeatures",
                    @"\Microsoft\Windows\Flighting\FeatureConfig\UsageDataReporting",
                    @"\Microsoft\Windows\Flighting\FeatureConfig\UsageDataFlushing",
                    @"\Microsoft\Windows\Flighting\OneSettings\RefreshCache"
                ]),
            new TaskItemSpec("Отключить фоновые задания обновлений",
                "Фоновые проверки и повторы обновлений Windows. Ручной запуск обновлений это не блокирует.",
                [
                    @"\Microsoft\Windows\InstallService\ScanForUpdates",
                    @"\Microsoft\Windows\InstallService\ScanForUpdatesAsUser",
                    @"\Microsoft\Windows\InstallService\SmartRetry",
                    @"\Microsoft\Windows\WindowsUpdate\Scheduled Start"
                ])
        ])
    ];
}
