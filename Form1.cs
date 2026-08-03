using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Win11Debloater;

public partial class Form1 : Form
{
    private sealed class OtimizacaoTask
    {
        public string Nome = "";
        public string Descricao = "";
        public string Tempo = "";
        public Func<Task> Acao = () => Task.CompletedTask;
        public Panel Card = null!;
        public Label StatusLabel = null!;
        public bool ExcludeFromQuick = false;

        public bool RequiresConfirmation = false;
        public string ConfirmationMessage = "";
    }

    private readonly List<OtimizacaoTask> _tasks = new();
    private bool _executando = false;

    private static readonly string PsHelpers = """
$ProgressPreference = 'SilentlyContinue'
$script:ProvCache = $null

function Get-ProvCache
{
    if ($null -eq $script:ProvCache)
    {
        $script:ProvCache = Get-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue
    }

    return $script:ProvCache
}

function Set-RegDword
{
    param([string]$Path, [string]$Name, [int]$Value)

    try
    {
        if (-not (Test-Path $Path))
        {
            New-Item -Path $Path -Force -ErrorAction Stop | Out-Null
        }

        Set-ItemProperty -Path $Path -Name $Name -Value $Value -Type DWord -Force -ErrorAction Stop
        Write-Output "[REG] $Path\$Name = $Value"
    }
    catch
    {
        Write-Output "[AVISO] Não foi possível definir $Path\$Name"
    }
}

function Stop-DisableService
{
    param([string]$ServiceName, [string]$Description)

    $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

    if ($svc)
    {
        Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
        Set-Service -Name $ServiceName -StartupType Disabled -ErrorAction SilentlyContinue
        Write-Output "[SVC] $Description ($ServiceName) -> parado e desabilitado"
    }
    else
    {
        Write-Output "[SVC] $Description ($ServiceName) -> não encontrado"
    }
}

function Disable-Task
{
    param([string]$TaskPath, [string]$TaskName)

    $task = Get-ScheduledTask -TaskPath $TaskPath -TaskName $TaskName -ErrorAction SilentlyContinue

    if ($task)
    {
        Disable-ScheduledTask -InputObject $task -ErrorAction SilentlyContinue | Out-Null
        Write-Output "[TASK] $TaskPath$TaskName -> desativada"
    }
    else
    {
        Write-Output "[TASK] $TaskPath$TaskName -> não encontrada"
    }
}

function Remove-AppxByName
{
    param([string]$AppName)

    Write-Output "[APP] Verificando: $AppName"

    $pkg = Get-AppxPackage -Name $AppName -AllUsers -ErrorAction SilentlyContinue

    if ($pkg)
    {
        try
        {
            Remove-AppxPackage -Package $pkg.PackageFullName -AllUsers -ErrorAction Stop
            Write-Output "[APP] -> Removido do usuário: $AppName"
        }
        catch
        {
            Write-Output "[APP] -> Não foi possível remover do usuário: $AppName"
        }
    }
    else
    {
        Write-Output "[APP] -> Já não instalado: $AppName"
    }

    $provList = @(Get-ProvCache | Where-Object { $_.DisplayName -eq $AppName })

    if ($provList.Count -gt 0)
    {
        foreach ($provItem in $provList)
        {
            try
            {
                Remove-AppxProvisionedPackage -Online -PackageName $provItem.PackageName -ErrorAction Stop | Out-Null
                Write-Output "[APP] -> Removido da imagem para novos usuários: $AppName"
            }
            catch
            {
                Write-Output "[APP] -> Imagem já não possuía pacote: $AppName"
            }
        }
    }
    else
    {
        Write-Output "[APP] -> Não provisionado para novos usuários: $AppName"
    }
}
""";

    public Form1()
    {
        InitializeComponent();

        Load += Form1_Load;
        btnRunAll.Click += BtnRunAll_Click;
        btnRunQuick.Click += BtnRunQuick_Click;
        btnRestart.Click += BtnRestart_Click;
        btnClearLog.Click += (s, e) => rtbLog.Clear();
    }

    private void Form1_Load(object? sender, EventArgs e)
    {
        MontarTasks();

        Log("Sistema pronto. Escolha uma ferramenta ou use os botões acima.", Color.FromArgb(150, 150, 150));
        Log("Dica: o modo rápido não executa DISM/SFC.", Color.FromArgb(120, 120, 120));
        Log("Após executar um pacote completo, recomendamos reiniciar o computador.", Color.FromArgb(120, 120, 120));
    }

    private void MontarTasks()
    {
        _tasks.Add(new OtimizacaoTask
        {
            Nome = "Ponto de Restauração",
            Descricao = "Backup seguro do sistema antes de aplicar mudanças.",
            Tempo = "~10s",
            Acao = CriarPontoRestauracao
        });

        _tasks.Add(new OtimizacaoTask
        {
            Nome = "Telemetria & Serviços",
            Descricao = "Desativa coleta de dados, erros e serviços de rastreio.",
            Tempo = "~10s",
            Acao = TelemetriaServicos
        });

        _tasks.Add(new OtimizacaoTask
        {
            Nome = "Privacidade do Usuário",
            Descricao = "Desliga anúncios, histórico de atividades e sugestões.",
            Tempo = "~8s",
            Acao = PrivacidadeUsuario
        });

        _tasks.Add(new OtimizacaoTask
        {
            Nome = "Apps & Bloatware",
            Descricao = "Remove apps nativos inúteis, mantendo loja e jogos.",
            Tempo = "~45s",
            Acao = RemoverBloatware,
            RequiresConfirmation = true,
            ConfirmationMessage =
                "Isto removerá apps opcionais do Windows, como:\n\n" +
                "Bing News, Bing Weather, Get Help, Tips, Messaging, Office Hub, Solitaire, Mixed Reality, Paint 3D, " +
                "Network Speed Test, Mobile Plans, People, Print 3D, Skype, Wallet, Mail/Calendar, Phone Link, " +
                "Movies & TV, Teams, Clipchamp, Power Automate, Todos, Feedback Hub, Maps e Cortana.\n\n" +
                "Não remove Microsoft Store, Segurança do Windows, Calculadora, Snipping Tool ou componentes essenciais.\n\n" +
                "Deseja realmente remover esses aplicativos?"
        });

        _tasks.Add(new OtimizacaoTask
        {
            Nome = "OneDrive & Nuvem",
            Descricao = "Remove OneDrive e integrações de nuvem forçadas.",
            Tempo = "~20s",
            Acao = OneDriveNuvem
        });

        _tasks.Add(new OtimizacaoTask
        {
            Nome = "IA, Copilot & Widgets",
            Descricao = "Desativa Copilot, Recall, widgets e IA integrada.",
            Tempo = "~8s",
            Acao = PrivacidadeIA
        });

        _tasks.Add(new OtimizacaoTask
        {
            Nome = "Interface & Busca",
            Descricao = "Menu clássico, busca local e menos sugestões.",
            Tempo = "~6s",
            Acao = InterfaceBusca
        });

        _tasks.Add(new OtimizacaoTask
        {
            Nome = "Serviços & Tarefas",
            Descricao = "Desativa tarefas agendadas de telemetria e CEIP.",
            Tempo = "~10s",
            Acao = ServicosTarefas
        });

        _tasks.Add(new OtimizacaoTask
        {
            Nome = "Desempenho & Jogos",
            Descricao = "Plano de energia, Game Mode e menos atrasos.",
            Tempo = "~5s",
            Acao = AltoDesempenho
        });

        _tasks.Add(new OtimizacaoTask
        {
            Nome = "Limpeza Profunda",
            Descricao = "Apaga temporários, caches, lixo e Lixeira.",
            Tempo = "~25s",
            Acao = LimpezaProfunda
        });

        _tasks.Add(new OtimizacaoTask
        {
            Nome = "Limpar Updates Antigos",
            Descricao = "Remove componentes substituídos do Windows Update.",
            Tempo = "5–20 min",
            Acao = LimparUpdatesAntigos,
            ExcludeFromQuick = true,
            RequiresConfirmation = true,
            ConfirmationMessage =
                "Esta etapa faz uma limpeza profunda de componentes do Windows Update.\n\n" +
                "Ela usa DISM StartComponentCleanup e ResetBase.\n" +
                "O ResetBase pode impedir a desinstalação de atualizações antigas.\n\n" +
                "Deseja realmente continuar?"
        });

        _tasks.Add(new OtimizacaoTask
        {
            Nome = "Reparo de Sistema",
            Descricao = "DISM + SFC: verifica e repara arquivos do Windows.",
            Tempo = "10–30 min",
            Acao = ReparoSistema,
            ExcludeFromQuick = true
        });

        foreach (var t in _tasks) CriarCard(t);
    }

    private void CriarCard(OtimizacaoTask t)
    {
        var card = new Panel
        {
            Width = 300,
            Height = 88,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 0, 10),
            Cursor = Cursors.Hand
        };

        var titulo = new Label
        {
            Text = t.Nome,
            AutoSize = false,
            Size = new Size(190, 18),
            Location = new Point(14, 12),
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(26, 26, 26),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        var tempo = new Label
        {
            Text = "⏱ " + t.Tempo,
            AutoSize = true,
            Location = new Point(210, 14),
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 103, 192)
        };

        var desc = new Label
        {
            Text = t.Descricao,
            AutoSize = false,
            Size = new Size(272, 30),
            Location = new Point(14, 34),
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(110, 110, 110),
            AutoEllipsis = true
        };

        var status = new Label
        {
            Text = "",
            AutoSize = true,
            Location = new Point(14, 66),
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 103, 192)
        };

        t.Card = card;
        t.StatusLabel = status;

        card.Controls.Add(titulo);
        card.Controls.Add(tempo);
        card.Controls.Add(desc);
        card.Controls.Add(status);

        void Wire(Control c)
        {
            c.MouseEnter += (s, e) =>
            {
                if (!_executando) card.BackColor = Color.FromArgb(240, 247, 253);
            };

            c.MouseLeave += (s, e) =>
            {
                if (!_executando) card.BackColor = Color.White;
            };

            c.Click += async (s, e) => await ExecutarTaskIndividual(t);
        }

        Wire(card);
        Wire(titulo);
        Wire(tempo);
        Wire(desc);
        Wire(status);

        flpTasks.Controls.Add(card);
    }

    private async Task ExecutarTaskIndividual(OtimizacaoTask t)
    {
        if (_executando) return;

        if (!ConfirmarTarefaPerigosa(t))
        {
            Log($"Operação cancelada pelo usuário: {t.Nome}.", Color.FromArgb(150, 150, 150));
            return;
        }

        _executando = true;
        SetUIBusy(true);

        try
        {
            await ExecutarTaskInterno(t);
            lblStatus.Text = $"Concluído: {t.Nome}";
        }
        finally
        {
            SetUIBusy(false);
            _executando = false;
        }
    }

    private async void BtnRunAll_Click(object? sender, EventArgs e)
    {
        await RunPackage(includeRepair: true);
    }

    private async void BtnRunQuick_Click(object? sender, EventArgs e)
    {
        await RunPackage(includeRepair: false);
    }

    private void BtnRestart_Click(object? sender, EventArgs e)
    {
        if (_executando) return;

        PerguntarReinicio();
    }

    private async Task RunPackage(bool includeRepair)
    {
        if (_executando) return;

        if (!ConfirmarPacote(includeRepair))
        {
            Log("Execução cancelada pelo usuário.", Color.FromArgb(150, 150, 150));
            return;
        }

        _executando = true;
        SetUIBusy(true);

        try
        {
            var selected = _tasks
                .Where(t => includeRepair || !t.ExcludeFromQuick)
                .ToList();

            progressBar.Value = 0;
            progressBar.Maximum = selected.Count;

            Log("========================================", Color.FromArgb(255, 210, 90));
            Log(includeRepair
                ? "PACOTE COMPLETO INICIADO"
                : "PACOTE RÁPIDO INICIADO (SEM DISM/SFC)",
                Color.FromArgb(255, 210, 90));
            Log("========================================", Color.FromArgb(255, 210, 90));

            int i = 0;

            foreach (var t in selected)
            {
                if (!ConfirmarTarefaPerigosa(t))
                {
                    i++;
                    progressBar.Value = i;
                    Log($"Etapa ignorada pelo usuário: {t.Nome}.", Color.FromArgb(255, 190, 90));
                    lblStatus.Text = $"Etapa {i} de {selected.Count} processada.";
                    continue;
                }

                await ExecutarTaskInterno(t);

                i++;
                progressBar.Value = i;
                lblStatus.Text = $"Etapa {i} de {selected.Count} concluída.";
                await Task.Delay(250);
            }

            Log("🎉 Pacote finalizado.", Color.FromArgb(110, 220, 140));
            Log("Recomenda-se reiniciar o computador.", Color.FromArgb(180, 180, 180));

            lblStatus.Text = includeRepair
                ? "Otimização completa. Reinicie o PC."
                : "Otimização rápida concluída. Reinicie o PC.";

            PerguntarReinicio();
        }
        finally
        {
            SetUIBusy(false);
            _executando = false;
        }
    }

    private bool ConfirmarTarefaPerigosa(OtimizacaoTask t)
    {
        if (!t.RequiresConfirmation || string.IsNullOrWhiteSpace(t.ConfirmationMessage))
            return true;

        var result = MessageBox.Show(
            t.ConfirmationMessage,
            $"Confirmar: {t.Nome}",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        return result == DialogResult.Yes;
    }

    private bool ConfirmarPacote(bool includeRepair)
    {
        var sb = new StringBuilder();

        sb.AppendLine("O utilitário fará as seguintes alterações:");
        sb.AppendLine();
        sb.AppendLine("1. Criará um ponto de restauração do sistema.");
        sb.AppendLine("2. Desativará telemetria, DiagTrack, WAP Push e Windows Error Reporting.");
        sb.AppendLine("3. Ajustará privacidade: ID de anúncios, histórico de atividades, sugestões e feedback.");
        sb.AppendLine("4. Removerá apps opcionais, como Bing News/Weather, Get Help, Tips, Messaging, Office Hub, Solitaire, Mixed Reality, Paint 3D, Network Speed Test, Mobile Plans, People, Print 3D, Skype, Wallet, Mail/Calendar, Phone Link, Movies & TV, Teams, Clipchamp, Power Automate, Todos, Feedback Hub, Maps e Cortana.");
        sb.AppendLine("5. Desinstalará o OneDrive e removerá inicialização/sincronização forçada.");
        sb.AppendLine("6. Desativará Copilot, análise de IA/Recall e Widgets.");
        sb.AppendLine("7. Aplicará menu de contexto clássico e busca local sem Bing.");
        sb.AppendLine("8. Desativará tarefas agendadas de telemetria, CEIP e coleta.");
        sb.AppendLine("9. Aplicará plano de alto desempenho, Game Mode e ajustes para jogos.");
        sb.AppendLine("10. Apagará temporários, Windows Temp, Prefetch, downloads antigos do Windows Update, Delivery Optimization e esvaziará a Lixeira.");

        if (includeRepair)
        {
            sb.AppendLine("11. Executará limpeza profunda de atualizações antigas (DISM StartComponentCleanup/ResetBase).");
            sb.AppendLine("12. Executará DISM + SFC. Essa etapa pode levar de 10 a 30 minutos.");
        }
        else
        {
            sb.AppendLine("11. NÃO executará limpeza profunda de atualizações, DISM/SFC. Esta é a opção rápida.");
        }

        sb.AppendLine();
        sb.AppendLine("Algumas etapas perigosas pedirão confirmação adicional antes de executar.");
        sb.AppendLine();
        sb.AppendLine("Nada essencial será removido: Microsoft Store, Segurança do Windows, Calculadora, Snipping Tool e componentes Xbox/Game Pass permanecem.");
        sb.AppendLine();
        sb.AppendLine("Atenção: a Lixeira será esvaziada e arquivos temporários serão apagados.");
        sb.AppendLine();
        sb.AppendLine("Deseja prosseguir?");

        var result = MessageBox.Show(
            sb.ToString(),
            includeRepair ? "Executar tudo" : "Executar tudo sem DISM/SFC",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        return result == DialogResult.Yes;
    }

    private void PerguntarReinicio()
    {
        var resposta = MessageBox.Show(
            "Deseja reiniciar agora?\n\nRecomendado para aplicar todas as alterações.",
            "Reiniciar computador",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button1);

        if (resposta == DialogResult.Yes)
        {
            Log("Reinicialização solicitada. O PC reiniciará em 10 segundos.", Color.FromArgb(255, 170, 90));

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "shutdown.exe",
                    Arguments = "/r /t 10 /c \"Win11 Optimizer reiniciará em 10 segundos\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch (Exception ex)
            {
                Log($"Não foi possível reiniciar: {ex.Message}", Color.FromArgb(255, 120, 120));
            }
        }
        else
        {
            Log("Reinicialização adiada.", Color.FromArgb(150, 150, 150));
        }
    }

    private async Task ExecutarTaskInterno(OtimizacaoTask t)
    {
        t.StatusLabel.Text = "● Executando...";
        t.StatusLabel.ForeColor = Color.FromArgb(0, 103, 192);
        t.Card.BackColor = Color.FromArgb(234, 241, 250);

        Log($"▶ {t.Nome}", Color.FromArgb(80, 180, 255));
        Log($"   Objetivo: {t.Descricao}", Color.FromArgb(170, 170, 170));

        var sw = Stopwatch.StartNew();

        try
        {
            await t.Acao();

            t.StatusLabel.Text = "✓ Concluído";
            t.StatusLabel.ForeColor = Color.FromArgb(20, 140, 60);

            Log($"✔ {t.Nome} concluído em {FormatTempo(sw)}.", Color.FromArgb(110, 220, 140));
        }
        catch (Exception ex)
        {
            t.StatusLabel.Text = "✗ Erro";
            t.StatusLabel.ForeColor = Color.Crimson;

            Log($"✗ Erro em {t.Nome}: {ex.Message}", Color.FromArgb(255, 120, 120));
        }
        finally
        {
            t.Card.BackColor = Color.White;
        }
    }

    private void SetUIBusy(bool busy)
    {
        btnRunAll.Enabled = !busy;
        btnRunQuick.Enabled = !busy;
        btnRestart.Enabled = !busy;

        if (busy)
            lblStatus.Text = "Executando...";
    }

    private string FormatTempo(Stopwatch sw)
    {
        if (sw.Elapsed.TotalMinutes >= 1)
            return $"{(int)sw.Elapsed.TotalMinutes}m {sw.Elapsed.Seconds}s";

        return $"{sw.Elapsed.TotalSeconds:0.#}s";
    }

    #region Ações

    private Task CriarPontoRestauracao()
    {
        string cmd = """
Set-RegDword -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore' -Name 'SystemRestorePointCreationFrequency' -Value 0
Write-Output "[SYS] Habilitando restauração do sistema na unidade C:..."
Enable-ComputerRestore -Drive 'C:\' -ErrorAction SilentlyContinue
Write-Output "[SYS] Criando ponto de restauração com descrição 'Win11Debloater'..."
Checkpoint-Computer -Description 'Win11Debloater' -RestorePointType MODIFY_SETTINGS
Write-Output "[OK] Ponto de restauração criado ou já existente."
""";

        return RunPS(cmd);
    }

    private Task TelemetriaServicos()
    {
        string cmd = """
Set-RegDword -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection' -Name 'AllowTelemetry' -Value 0
Set-RegDword -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection' -Name 'AllowTelemetry' -Value 0
Set-RegDword -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection' -Name 'AllowDeviceNameInTelemetry' -Value 0
Set-RegDword -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Privacy' -Name 'TailoredExperiencesWithDiagnosticDataEnabled' -Value 0
Set-RegDword -Path 'HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting' -Name 'Disabled' -Value 1
Stop-DisableService -ServiceName 'DiagTrack' -Description 'Connected User Experiences and Telemetry'
Stop-DisableService -ServiceName 'dmwappushservice' -Description 'WAP Push Message Routing'
Stop-DisableService -ServiceName 'WerSvc' -Description 'Windows Error Reporting'
Write-Output "[OK] Telemetria, rastreamento e relatórios de erro reduzidos."
""";

        return RunPS(cmd);
    }

    private Task PrivacidadeUsuario()
    {
        string cmd = """
Set-RegDword -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo' -Name 'Disabled' -Value 1
Set-RegDword -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\System' -Name 'EnableActivityFeed' -Value 0
Set-RegDword -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\System' -Name 'PublishUserActivities' -Value 0
Set-RegDword -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\System' -Name 'UploadUserActivities' -Value 0
Set-RegDword -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' -Name 'Start_TrackDocs' -Value 0
Set-RegDword -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Privacy' -Name 'TailoredExperiencesWithDiagnosticDataEnabled' -Value 0
Set-RegDword -Path 'HKCU:\Software\Policies\Microsoft\Windows\CloudContent' -Name 'DisableWindowsConsumerFeatures' -Value 1
Set-RegDword -Path 'HKCU:\Software\Policies\Microsoft\Windows\CloudContent' -Name 'DisableSoftLanding' -Value 1
Set-RegDword -Path 'HKCU:\Software\Policies\Microsoft\Windows\CloudContent' -Name 'DisableTailoredExperiencesWithDiagnosticData' -Value 1
Set-RegDword -Path 'HKCU:\Software\Microsoft\Siuf\Rules' -Name 'NumberOfSIUFRequests' -Value 0
Set-RegDword -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\AppCompat' -Name 'DisableInventory' -Value 1
Write-Output "[OK] Privacidade do usuário ajustada."
""";

        return RunPS(cmd);
    }

    private Task RemoverBloatware()
    {
        string cmd = """
$apps = @(
    'Microsoft.3DBuilder',
    'Microsoft.BingNews',
    'Microsoft.BingWeather',
    'Microsoft.GetHelp',
    'Microsoft.Getstarted',
    'Microsoft.Messaging',
    'Microsoft.Microsoft3DPrinting',
    'Microsoft.MicrosoftOfficeHub',
    'Microsoft.MicrosoftSolitaireCollection',
    'Microsoft.MixedReality.Portal',
    'Microsoft.MSPaint',
    'Microsoft.NetworkSpeedTest',
    'Microsoft.OneConnect',
    'Microsoft.People',
    'Microsoft.Print3D',
    'Microsoft.SkypeApp',
    'Microsoft.Wallet',
    'Microsoft.WindowsCommunicationsApps',
    'Microsoft.YourPhone',
    'Microsoft.ZuneVideo',
    'Microsoft.ZuneMusic',
    'Microsoft.MicrosoftTeams',
    'MicrosoftTeams',
    'Clipchamp.Clipchamp',
    'Microsoft.PowerAutomateDesktop',
    'Microsoft.Todos',
    'Microsoft.WindowsFeedbackHub',
    'Microsoft.WindowsMaps',
    'Microsoft.549981C3F5F10'
)

foreach ($app in $apps)
{
    Remove-AppxByName -AppName $app
}

Write-Output "[OK] Bloatware processado."
""";

        return RunPS(cmd);
    }

    private Task OneDriveNuvem()
    {
        string cmd = """
Write-Output "[ONE] Encerrando processos do OneDrive..."
Stop-Process -Name OneDrive -Force -ErrorAction SilentlyContinue

$paths = @(
    "$env:SystemRoot\SysWOW64\OneDriveSetup.exe",
    "$env:SystemRoot\System32\OneDriveSetup.exe"
)

foreach ($p in $paths)
{
    if (Test-Path $p)
    {
        Write-Output "[ONE] Desinstalando: $p"
        Start-Process $p -ArgumentList '/uninstall' -Wait -NoNewWindow -ErrorAction SilentlyContinue
    }
}

Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'OneDrive' -ErrorAction SilentlyContinue
Write-Output "[REG] HKCU Run\OneDrive -> removido da inicialização"

Set-RegDword -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\OneDrive' -Name 'DisableFileSyncNGSC' -Value 1
Write-Output "[OK] OneDrive removido e sincronização de nuvem desativada."
""";

        return RunPS(cmd);
    }

    private Task PrivacidadeIA()
    {
        string cmd = """
Set-RegDword -Path 'HKCU:\Software\Policies\Microsoft\Windows\WindowsCopilot' -Name 'TurnOffWindowsCopilot' -Value 1
Set-RegDword -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot' -Name 'TurnOffWindowsCopilot' -Value 1
Set-RegDword -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsAI' -Name 'DisableAIDataAnalysis' -Value 1

Write-Output "[IA] Verificando pacote de Widgets (Microsoft.Windows.Client.WebExperience)"
$widget = Get-AppxPackage -Name 'Microsoft.Windows.Client.WebExperience' -AllUsers -ErrorAction SilentlyContinue

if ($widget)
{
    try
    {
        Remove-AppxPackage -Package $widget.PackageFullName -AllUsers -ErrorAction Stop
        Write-Output "[APP] -> Widgets removido do usuário."
    }
    catch
    {
        Write-Output "[APP] -> Widgets não pôde ser removido."
    }
}
else
{
    Write-Output "[APP] -> Widgets já não estava instalado."
}

Set-RegDword -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Dsh' -Name 'EnableNewsAndInterests' -Value 0
Set-RegDword -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search' -Name 'AllowCortana' -Value 0

Write-Output "[OK] Copilot, Recall, widgets e assistentes desativados."
""";

        return RunPS(cmd);
    }

    private Task InterfaceBusca()
    {
        string cmd = """
Write-Output "[UI] Ativando menu de contexto clássico..."
reg add 'HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32' /f /ve | Out-Null

Set-RegDword -Path 'HKCU:\Software\Policies\Microsoft\Windows\Explorer' -Name 'DisableSearchBoxSuggestions' -Value 1
Set-RegDword -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Search' -Name 'BingSearchEnabled' -Value 0
Set-RegDword -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Search' -Name 'CortanaConsent' -Value 0
Set-RegDword -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\SearchSettings' -Name 'IsDynamicSearchBoxEnabled' -Value 0
Set-RegDword -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\SearchSettings' -Name 'IsMSSEnabled' -Value 0
Set-RegDword -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\SearchSettings' -Name 'IsDeviceSearchHistoryEnabled' -Value 0
Set-RegDword -Path 'HKCU:\Software\Policies\Microsoft\Windows\Explorer' -Name 'DisableSearchHighlights' -Value 1

Write-Output "[OK] Interface e busca ajustadas."
""";

        return RunPS(cmd);
    }

    private Task ServicosTarefas()
    {
        string cmd = """
Disable-Task -TaskPath '\Microsoft\Windows\Application Experience\' -TaskName 'Microsoft Compatibility Appraiser'
Disable-Task -TaskPath '\Microsoft\Windows\Application Experience\' -TaskName 'ProgramDataUpdater'
Disable-Task -TaskPath '\Microsoft\Windows\Application Experience\' -TaskName 'StartupAppTask'
Disable-Task -TaskPath '\Microsoft\Windows\Customer Experience Improvement Program\' -TaskName 'Consolidator'
Disable-Task -TaskPath '\Microsoft\Windows\Customer Experience Improvement Program\' -TaskName 'UsbCeip'
Disable-Task -TaskPath '\Microsoft\Windows\Customer Experience Improvement Program\' -TaskName 'BthSQM'
Disable-Task -TaskPath '\Microsoft\Windows\Windows Error Reporting\' -TaskName 'QueueReporting'
Disable-Task -TaskPath '\Microsoft\Windows\Feedback\Siuf\' -TaskName 'DmClient'
Disable-Task -TaskPath '\Microsoft\Windows\Feedback\Siuf\' -TaskName 'DmClientOnScenarioDownload'
Disable-Task -TaskPath '\Microsoft\Windows\NetTrace\' -TaskName 'GatherNetworkInfo'

Write-Output "[OK] Tarefas agendadas de telemetria e coleta desativadas."
""";

        return RunPS(cmd);
    }

    private Task AltoDesempenho()
    {
        string cmd = """
Write-Output "[ENERGIA] Criando/ativando plano Alto Desempenho..."
powercfg /duplicatescheme 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c | Out-Null
powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c

Set-RegDword -Path 'HKCU:\Software\Microsoft\GameBar' -Name 'AllowAutoGameMode' -Value 1
Set-RegDword -Path 'HKCU:\System\GameConfigStore' -Name 'GameDVR_Enabled' -Value 0
Set-RegDword -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\GameDVR' -Name 'AllowGameDVR' -Value 0
Set-RegDword -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize' -Name 'StartupDelayInMSec' -Value 0

Write-Output "[OK] Desempenho e jogos configurados."
""";

        return RunPS(cmd);
    }

    private Task LimpezaProfunda()
    {
        string cmd = """
Write-Output "[FILE] Parando serviços do Windows Update, BITS e Entrega Otimizada..."
Stop-Service -Name wuauserv, bits, DoSvc -Force -ErrorAction SilentlyContinue

Write-Output "[FILE] Limpando Temp do usuário ($env:TEMP)"
Remove-Item -Path "$env:TEMP\*" -Recurse -Force -ErrorAction SilentlyContinue

Write-Output "[FILE] Limpando Temp do sistema (C:\Windows\Temp)"
Remove-Item -Path 'C:\Windows\Temp\*' -Recurse -Force -ErrorAction SilentlyContinue

Write-Output "[FILE] Limpando Prefetch (C:\Windows\Prefetch)"
Remove-Item -Path 'C:\Windows\Prefetch\*' -Recurse -Force -ErrorAction SilentlyContinue

Write-Output "[FILE] Limpando downloads do Windows Update"
Remove-Item -Path 'C:\Windows\SoftwareDistribution\Download\*' -Recurse -Force -ErrorAction SilentlyContinue

Write-Output "[FILE] Limpando arquivos de Entrega Otimizada"
Remove-Item -Path 'C:\Windows\SoftwareDistribution\DeliveryOptimization\*' -Recurse -Force -ErrorAction SilentlyContinue

Write-Output "[FILE] Reiniciando serviços do Windows Update, BITS e Entrega Otimizada..."
Start-Service -Name wuauserv, bits, DoSvc -ErrorAction SilentlyContinue

Write-Output "[FILE] Esvaziando Lixeira"
Clear-RecycleBin -Force -Confirm:$false -ErrorAction SilentlyContinue

Write-Output "[OK] Limpeza básica concluída."
""";

        return RunPS(cmd);
    }

    private async Task LimparUpdatesAntigos()
    {
        Log("   Esta limpeza remove componentes substituídos e restos de atualizações antigas.", Color.FromArgb(150, 150, 150));
        Log("   O ResetBase pode impedir a desinstalação de atualizações antigas.", Color.FromArgb(255, 190, 90));

        await RunPS("""
Write-Output "[UPDATE] Analisando repositório de componentes (pré-limpeza)..."
Dism /Online /Cleanup-Image /AnalyzeComponentStore

Write-Output "[UPDATE] Parando serviços do Windows Update, BITS e Entrega Otimizada..."
Stop-Service -Name wuauserv, bits, DoSvc -Force -ErrorAction SilentlyContinue

Write-Output "[UPDATE] Limpando downloads do Windows Update..."
Remove-Item -Path 'C:\Windows\SoftwareDistribution\Download\*' -Recurse -Force -ErrorAction SilentlyContinue

Write-Output "[UPDATE] Reiniciando serviços do Windows Update, BITS e Entrega Otimizada..."
Start-Service -Name wuauserv, bits, DoSvc -ErrorAction SilentlyContinue

Write-Output "[UPDATE] Executando DISM StartComponentCleanup..."
Dism /Online /Cleanup-Image /StartComponentCleanup /Quiet

if ($LASTEXITCODE -eq 0 -or $LASTEXITCODE -eq 3010)
{
    Write-Output "[OK] StartComponentCleanup concluído."
}
else
{
    Write-Output "[AVISO] StartComponentCleanup terminou com código $LASTEXITCODE."
}

Write-Output "[UPDATE] Executando DISM ResetBase (limpeza profunda)..."
Dism /Online /Cleanup-Image /StartComponentCleanup /ResetBase /Quiet

if ($LASTEXITCODE -eq 0 -or $LASTEXITCODE -eq 3010)
{
    Write-Output "[OK] ResetBase concluído."
}
else
{
    Write-Output "[AVISO] ResetBase terminou com código $LASTEXITCODE."
}

Write-Output "[CLEAN] Configurando Disk Cleanup para categorias de atualização..."

$categories = @(
    'Update Cleanup',
    'Limpeza do Windows Update',
    'Previous Windows Installation(s)',
    'Instalações anteriores do Windows',
    'Temporary Setup Files',
    'Arquivos de instalação temporários',
    'Windows Upgrade Log Files',
    'Arquivos de log de atualização do Windows',
    'Delivery Optimization Files',
    'Arquivos de Otimização de Entrega',
    'Temporary Files',
    'Arquivos temporários'
)

foreach ($cat in $categories)
{
    $path = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\$cat"

    if (Test-Path $path)
    {
        Set-ItemProperty -Path $path -Name 'StateFlags0001' -Value 2 -Type DWord -Force -ErrorAction SilentlyContinue
        Write-Output "[CLEAN] Categoria habilitada: $cat"
    }
}

Write-Output "[CLEAN] Executando Disk Cleanup (pode abrir uma janela de progresso)..."
Start-Process cleanmgr.exe -ArgumentList '/d C /sagerun:1' -Wait -NoNewWindow -ErrorAction SilentlyContinue

Write-Output "[UPDATE] Analisando repositório de componentes (pós-limpeza)..."
Dism /Online /Cleanup-Image /AnalyzeComponentStore

Write-Output "[OK] Limpeza de atualizações concluída."
""");
    }

    private async Task ReparoSistema()
    {
        Log("   Fase 1/2 — DISM RestoreHealth...", Color.FromArgb(120, 200, 255));
        Log("   Isso pode levar de 10 a 30 minutos.", Color.FromArgb(150, 150, 150));

        await RunPS("""
Write-Output "[DISM] Restaurando imagem do Windows..."
Dism /Online /Cleanup-Image /RestoreHealth
Write-Output "[OK] DISM RestoreHealth concluído."
""");

        Log("   Fase 2/2 — SFC Scannow...", Color.FromArgb(120, 200, 255));

        await RunPS("""
Write-Output "[SFC] Verificando arquivos do sistema..."
sfc /scannow
Write-Output "[OK] SFC concluído."
""");
    }

    #endregion

    #region Executor PowerShell + Log

    private async Task RunPS(string script)
    {
        var tempFile = Path.Combine(
            Path.GetTempPath(),
            $"Win11Debloat_{Guid.NewGuid():N}.ps1");

        var fullScript =
            "[Console]::OutputEncoding=[System.Text.Encoding]::UTF8\r\n" +
            "$OutputEncoding=[System.Text.Encoding]::UTF8\r\n" +
            "$ErrorActionPreference='Continue'\r\n" +
            PsHelpers + "\r\n" + script;

        File.WriteAllText(tempFile, fullScript, new UTF8Encoding(true));

        var outputCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var errorCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempFile}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var proc = new Process { StartInfo = psi };

        proc.OutputDataReceived += (s, e) =>
        {
            if (e.Data is null)
                outputCompleted.TrySetResult(true);
            else
                TrataLinha(e.Data, false);
        };

        proc.ErrorDataReceived += (s, e) =>
        {
            if (e.Data is null)
                errorCompleted.TrySetResult(true);
            else
                TrataLinha(e.Data, true);
        };

        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        await proc.WaitForExitAsync().ConfigureAwait(false);
        await Task.WhenAll(outputCompleted.Task, errorCompleted.Task).ConfigureAwait(false);

        try { File.Delete(tempFile); } catch { }
    }

    private void TrataLinha(string? linha, bool erro)
    {
        if (string.IsNullOrWhiteSpace(linha)) return;

        // Remove barras de progresso do DISM: [==== 63.4% ====]
        if (Regex.IsMatch(linha, @"^\s*\[[=\s\d\.\%]+\]\s*$")) return;

        string txt = "   " + linha.Trim();
        Color cor;

        if (linha.Contains("[AVISO]"))
            cor = Color.FromArgb(255, 190, 90);
        else if (erro)
            cor = Color.FromArgb(255, 140, 140);
        else if (linha.Contains("[OK]") || linha.Contains("êxito") || linha.Contains("100.0%"))
            cor = Color.FromArgb(110, 220, 140);
        else if (linha.Contains("[REG]"))
            cor = Color.FromArgb(150, 190, 255);
        else if (linha.Contains("[SVC]"))
            cor = Color.FromArgb(180, 150, 255);
        else if (linha.Contains("[APP]"))
            cor = Color.FromArgb(180, 210, 240);
        else if (linha.Contains("[TASK]"))
            cor = Color.FromArgb(240, 190, 120);
        else if (linha.Contains("[FILE]"))
            cor = Color.FromArgb(150, 220, 180);
        else if (linha.Contains("[IA]") || linha.Contains("[WIDGET]"))
            cor = Color.FromArgb(220, 170, 220);
        else if (linha.Contains("[ONE]"))
            cor = Color.FromArgb(140, 200, 220);
        else if (linha.Contains("[UI]") || linha.Contains("[BUSCA]"))
            cor = Color.FromArgb(210, 210, 150);
        else if (linha.Contains("[ENERGIA]") || linha.Contains("[GAME]"))
            cor = Color.FromArgb(240, 200, 120);
        else if (linha.Contains("[UPDATE]") || linha.Contains("[CLEAN]"))
            cor = Color.FromArgb(120, 200, 255);
        else if (linha.Contains("[DISM]") || linha.Contains("[SFC]"))
            cor = Color.FromArgb(120, 200, 255);
        else
            cor = Color.FromArgb(210, 210, 210);

        Log(txt, cor);
    }

    private void Log(string texto, Color cor)
    {
        if (rtbLog.IsDisposed) return;

        if (rtbLog.InvokeRequired)
        {
            rtbLog.Invoke(() => Log(texto, cor));
            return;
        }

        rtbLog.SelectionStart = rtbLog.TextLength;
        rtbLog.SelectionLength = 0;
        rtbLog.SelectionColor = cor;
        rtbLog.AppendText($"{DateTime.Now:HH:mm:ss} │ {texto}\n");
        rtbLog.SelectionColor = rtbLog.ForeColor;
        rtbLog.ScrollToCaret();
    }

    #endregion
}