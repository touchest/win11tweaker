using Microsoft.Win32;
using Win11Tweaker.Core;

namespace Win11Tweaker.App.Catalog;

public static class ServiceTweaks
{
    const string Services = @"SYSTEM\CurrentControlSet\Services";

    public static IReadOnlyList<TweakGroupSpec> Build() =>
    [
        new TweakGroupSpec("Слежка и обновления",
        [
            new TweakDefinition
            {
                Id = "svc.telemetry",
                Title = "Мне не нужна слежка и диагностика",
                Summary = "Гасит цепочку служб сбора и передачи диагностики Microsoft: DiagTrack, dmwappushservice, WerSvc, PcaSvc.",
                Risk = RiskLevel.Safe,
                NeedsSignOut = true,
                Writes =
                [
                    Start("DiagTrack", off: 2),
                    Start("dmwappushservice", off: 3),
                    Start("WerSvc", off: 3),
                    Start("PcaSvc", off: 2)
                ]
            },
            new TweakDefinition
            {
                Id = "svc.updates",
                Title = "Я не пользуюсь автообновлениями Windows",
                Summary = "Гасит всю цепочку обновлений: службу обновления, оркестратор, службу восстановления, доставку и политику автообновления. Обновления ставятся вручную.",
                Risk = RiskLevel.Moderate,
                NeedsSignOut = true,
                Writes =
                [
                    Start("wuauserv", off: 3),
                    Start("UsoSvc", off: 2),
                    Start("WaaSMedicSvc", off: 3),
                    Start("DoSvc", off: 3),
                    Policy(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate", on: 1)
                ]
            }
        ]),

        new TweakGroupSpec("Ресурсы",
        [
            new TweakDefinition
            {
                Id = "svc.sysmain",
                Title = "Мне не нужен SuperFetch (у меня SSD)",
                Summary = "Служба SysMain отвечает за предзагрузку приложений в память. На SSD эффект минимален.",
                Risk = RiskLevel.Moderate,
                NeedsSignOut = true,
                Writes = [Start("SysMain", off: 2)]
            },
            new TweakDefinition
            {
                Id = "svc.wsearch",
                Title = "Мне не нужна индексация поиска",
                Summary = "Служба Windows Search. Без индексации поиск по содержимому файлов замедляется.",
                Risk = RiskLevel.Moderate,
                NeedsSignOut = true,
                Writes = [Start("WSearch", off: 2)]
            },
            new TweakDefinition
            {
                Id = "svc.geolocation",
                Title = "Я не использую геолокацию",
                Summary = "Служба определения местоположения (lfsvc).",
                Risk = RiskLevel.Safe,
                NeedsSignOut = true,
                Writes = [Start("lfsvc", off: 3)]
            }
        ]),

        new TweakGroupSpec("Устройства и функции",
        [
            new TweakDefinition
            {
                Id = "svc.hyperv",
                Title = "Я не использую виртуальные машины (Hyper-V)",
                Summary = "Гасит службы интеграции и хоста Hyper-V. Если не запускаешь виртуалки, они не нужны.",
                Risk = RiskLevel.Safe,
                NeedsSignOut = true,
                Writes =
                [
                    Start("HvHost", off: 3),
                    Start("vmicheartbeat", off: 3),
                    Start("vmicvss", off: 3),
                    Start("vmicshutdown", off: 3),
                    Start("vmictimesync", off: 3),
                    Start("vmicrdv", off: 3)
                ]
            },
            new TweakDefinition
            {
                Id = "svc.remote",
                Title = "Я не позволяю управлять моим ПК удалённо",
                Summary = "Гасит удалённый рабочий стол и удалённое управление: TermService, SessionEnv, UmRdpService, WinRM, RemoteRegistry, RemoteAccess. Заодно закрывает поверхность атаки.",
                Risk = RiskLevel.Moderate,
                NeedsSignOut = true,
                Writes =
                [
                    Start("TermService", off: 3),
                    Start("SessionEnv", off: 3),
                    Start("UmRdpService", off: 3),
                    Start("WinRM", off: 3),
                    Start("RemoteRegistry", off: 4),
                    Start("RemoteAccess", off: 4)
                ]
            },
            new TweakDefinition
            {
                Id = "svc.smartcard-bio",
                Title = "Я не использую смарт-карты и биометрию",
                Summary = "Гасит смарт-карты и сканер отпечатков/лица: SCardSvr, ScDeviceEnum, SCPolicySvc, CertPropSvc, WbioSrvc.",
                Risk = RiskLevel.Safe,
                NeedsSignOut = true,
                Writes =
                [
                    Start("SCardSvr", off: 3),
                    Start("ScDeviceEnum", off: 3),
                    Start("SCPolicySvc", off: 3),
                    Start("CertPropSvc", off: 3),
                    Start("WbioSrvc", off: 3)
                ]
            },
            new TweakDefinition
            {
                Id = "svc.fax-webdav-fh",
                Title = "Я не использую факс, WebDAV и историю файлов",
                Summary = "Гасит редко нужные службы: Fax (факс), WebClient (сетевые диски WebDAV), fhsvc (история файлов). Отсутствующие в системе просто игнорируются.",
                Risk = RiskLevel.Safe,
                NeedsSignOut = true,
                Writes =
                [
                    Start("Fax", off: 3),
                    Start("WebClient", off: 3),
                    Start("fhsvc", off: 3)
                ]
            }
        ])
    ];

    static RegistryWrite Start(string service, int off) => new()
    {
        Root = RegistryRoot.LocalMachine,
        Key = Services + "\\" + service,
        Name = "Start",
        Kind = RegistryValueKind.DWord,
        OnValue = 4,
        OffValue = off,
        SkipIfKeyAbsent = true
    };

    static RegistryWrite Policy(string key, string name, int on) => new()
    {
        Root = RegistryRoot.LocalMachine,
        Key = key,
        Name = name,
        Kind = RegistryValueKind.DWord,
        OnValue = on,
        OffValue = null
    };
}
